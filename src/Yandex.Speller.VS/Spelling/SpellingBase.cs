using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Yandex.Speller.VS.Dictionary;
using Yandex.Speller.VS.Tags;

namespace Yandex.Speller.VS.Spelling
{
	internal abstract class SpellingBase<T> : IDisposable, ITagger<IMisspellingTag> where T : ITag
	{
		private readonly ITextBuffer _buffer;		
		private readonly object _dirtySpanLock = new object();
		private readonly Dispatcher _dispatcher;
		private readonly ITagAggregator<T> _textTagger;
		private List<SnapshotSpan> _dirtySpans;
		private volatile List<MisspellingTag> _misspellings;
		private DispatcherTimer _timer;
		private Thread _updateThread;
		private readonly IYandexDictionary _dictionary = new Factory().CreateYandexDictionary();
		protected SpellingBase(ITextBuffer buffer, ITagAggregator<T> textTagger)
		{
			_buffer = buffer;
			_textTagger = textTagger;
			_dispatcher = Dispatcher.CurrentDispatcher;			

			_dirtySpans = new List<SnapshotSpan>();
			_misspellings = new List<MisspellingTag>();

			_buffer.Changed += BufferChanged;
			_textTagger.TagsChanged += NaturalTagsChanged;
			_dictionary.DictionaryUpdated += DictionaryUpdated;

			// To start with, the entire buffer is dirty
			// Split this into chunks, so we update pieces at a time
			ITextSnapshot snapshot = _buffer.CurrentSnapshot;

			foreach (ITextSnapshotLine line in snapshot.Lines)
				AddDirtySpan(line.Extent);
		}

		/// <summary />
		protected IYandexDictionary Dictionary => _dictionary;

	    public IEnumerable<ITagSpan<IMisspellingTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			if (spans.Count == 0)
				yield break;

			List<MisspellingTag> currentMisspellings = _misspellings;

			if (currentMisspellings.Count == 0)
				yield break;

			ITextSnapshot snapshot = spans[0].Snapshot;

			foreach (MisspellingTag misspelling in currentMisspellings)
			{
				ITagSpan<IMisspellingTag> tagSpan = misspelling.ToTagSpan(snapshot);
				if (tagSpan.Span.Length == 0)
					continue;

				if (spans.IntersectsWith(new NormalizedSnapshotSpanCollection(tagSpan.Span)))
					yield return tagSpan;
			}
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		private void NaturalTagsChanged(object sender, TagsChangedEventArgs e)
		{
			NormalizedSnapshotSpanCollection dirtySpans = e.Span.GetSpans(_buffer.CurrentSnapshot);

			if (dirtySpans.Count == 0)
				return;

			var dirtySpan = new SnapshotSpan(_buffer.CurrentSnapshot, dirtySpans[0].Start, dirtySpans[dirtySpans.Count - 1].End);
			AddDirtySpan(dirtySpan);
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(dirtySpan));
		}

		private void DictionaryUpdated(object sender, AddedWordToDictionaryEventArgs e)
		{
			lock (_dirtySpanLock)
			{
				List<MisspellingTag> currentMisspellings = _misspellings;
				ITextSnapshot snapshot = _buffer.CurrentSnapshot;

				foreach (MisspellingTag misspelling in currentMisspellings)
				{
					SnapshotSpan span = misspelling.Span.GetSpan(snapshot);

					if (span.GetText() == e.Word)
						AddDirtySpan(span);
				}
			}
		}

		private void BufferChanged(object sender, TextContentChangedEventArgs e)
		{
			ITextSnapshot snapshot = e.After;

			foreach (ITextChange change in e.Changes)
			{
				var changedSpan = new SnapshotSpan(snapshot, change.NewSpan);

				ITextSnapshotLine startLine = changedSpan.Start.GetContainingLine();
				ITextSnapshotLine endLine = (startLine.EndIncludingLineBreak < changedSpan.End)
					? changedSpan.End.GetContainingLine()
					: startLine;

				AddDirtySpan(new SnapshotSpan(startLine.Start, endLine.End));
			}
		}

		private NormalizedSnapshotSpanCollection GetNaturalLanguageSpansForDirtySpan(SnapshotSpan dirtySpan)
		{
			if (dirtySpan.IsEmpty)
				return new NormalizedSnapshotSpanCollection();

			ITextSnapshot snapshot = dirtySpan.Snapshot;
			return new NormalizedSnapshotSpanCollection(_textTagger.GetTags(dirtySpan)
				.SelectMany(tag => tag.Span.GetSpans(snapshot))
				.Select(s => s.Intersection(dirtySpan))
				.Where(s => s.HasValue && !s.Value.IsEmpty)
				.Select(s => s.Value));
		}

		private void AddDirtySpan(SnapshotSpan span)
		{
			NormalizedSnapshotSpanCollection naturalLanguageSpans = GetNaturalLanguageSpansForDirtySpan(span);

			if (naturalLanguageSpans.Count == 0)
				return;

			lock (_dirtySpanLock)
			{
				_dirtySpans.AddRange(naturalLanguageSpans);
				ScheduleUpdate();
			}
		}

		private void ScheduleUpdate()
		{
			if (_timer == null)
			{
				_timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle, _dispatcher)
				{
					Interval = TimeSpan.FromMilliseconds(2000)
				};

				_timer.Tick += (sender, args) =>
				{
					// If an update is currently running, wait until the next timer tick
					if (_updateThread != null && _updateThread.IsAlive)
						return;

					_timer.Stop();

					_updateThread = new Thread(CheckSpellings)
					{
						Name = "Spell Check",
						Priority = ThreadPriority.Normal
					};

					if (!_updateThread.TrySetApartmentState(ApartmentState.STA))
						Debug.Fail("Unable to set thread apartment state to STA, things *will* break.");

					_updateThread.Start();
				};
			}

			_timer.Stop();
			_timer.Start();
		}

		private void CheckSpellings(object obj)
		{
			IList<SnapshotSpan> dirtySpans;

			lock (_dirtySpanLock)
			{
				dirtySpans = _dirtySpans;
				if (dirtySpans.Count == 0)
					return;

				_dirtySpans = new List<SnapshotSpan>();
			}

			ITextSnapshot snapshot = _buffer.CurrentSnapshot;

			var dirty = new NormalizedSnapshotSpanCollection(
				dirtySpans.Select(span => span.TranslateTo(snapshot, SpanTrackingMode.EdgeInclusive)));

			if (dirty.Count == 0)
			{
				Debug.Fail("The list of dirty spans is empty when normalized, which shouldn't be possible.");
				return;
			}

			// Break up dirty into component pieces, so we produce incremental updates
			foreach (SnapshotSpan dirtySpan in dirty)
			{
				List<MisspellingTag> currentMisspellings = _misspellings;
				var newMisspellings = new List<MisspellingTag>();

				int removed = currentMisspellings.RemoveAll(tag => tag.ToTagSpan(snapshot).Span.OverlapsWith(dirtySpan));
				newMisspellings.AddRange(GetMisspellingsInSpan(dirtySpan));

				// Also remove empties
				removed += currentMisspellings.RemoveAll(tag => tag.ToTagSpan(snapshot).Span.IsEmpty);

				// If anything has been updated, we need to send out a change event
				if (newMisspellings.Count != 0 || removed != 0)
				{
					currentMisspellings.AddRange(newMisspellings);

					_dispatcher.Invoke(() =>
					{
					    _misspellings = currentMisspellings;
                        TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(dirtySpan));
					});
				}
			}

			lock (_dirtySpanLock)
			{
				if (_dirtySpans.Count != 0)
					_dispatcher.Invoke(ScheduleUpdate);
			}
		}

		protected abstract IEnumerable<MisspellingTag> GetMisspellingsInSpan(SnapshotSpan span);

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		void IDisposable.Dispose()
		{
			_dictionary.DictionaryUpdated -= DictionaryUpdated;
		}
	}
}