﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Yandex.Speller.Api;
using Yandex.Speller.Api.DataContract;
using Yandex.Speller.VS.Dictionary;
using Yandex.Speller.VS.SmartTags;
using Yandex.Speller.VS.Spelling;
using Yandex.Speller.VS.Tags;

namespace Yandex.Speller.VS
{
	/// <summary>
	/// Tagger for Spelling smart tags.
	/// </summary>
	internal class SpellSmartTagger : ITagger<SpellSmartTag>, IDisposable
	{
		#region Private Fields

		internal const string SpellingErrorType = "Spelling Error Smart Tag";
		private ITextBuffer _buffer;
		private IYandexDictionary _dictionary;
		private ITagAggregator<IMisspellingTag> _misspellingAggregator;
		private bool disposed;

		#endregion

		#region MEF Imports / Exports

		[Export(typeof (IViewTaggerProvider))]
		[ContentType("text")]
		[TagType(typeof (SpellSmartTag))]
		[Order(After = Priority.High)]
		internal class SpellSmartTaggerProvider : IViewTaggerProvider
		{
			#region MEF Imports

			private IYandexDictionary Dictionary = new Factory().CreateYandexDictionary();

			[Import(typeof (IBufferTagAggregatorFactoryService))]
			internal IBufferTagAggregatorFactoryService TagAggregatorFactory = null;

			#endregion

			#region ITaggerProvider

			public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
			{
				if (buffer == null)
					throw new ArgumentNullException("buffer");
				if (textView == null)
					throw new ArgumentNullException("textView");

				// Make sure we only tagging top buffer
				if (buffer != textView.TextBuffer)
					return null;

				return
					textView.Properties.GetOrCreateSingletonProperty(
						() =>
							new SpellSmartTagger(buffer, Dictionary, TagAggregatorFactory.CreateTagAggregator<IMisspellingTag>(buffer)) as
								ITagger<T>);
			}

			#endregion
		}

		#endregion

		public SpellSmartTagger(ITextBuffer buffer, IYandexDictionary dictionary,
		                        ITagAggregator<IMisspellingTag> misspellingAggregator)
		{
			_buffer = buffer;
			_dictionary = dictionary;
			_misspellingAggregator = misspellingAggregator;

			_misspellingAggregator.TagsChanged += (sender, args) =>
			{
				foreach (var span in args.Span.GetSpans(_buffer))
					RaiseTagsChangedEvent(span);
			};
		}

		#region ITagger<SpellSmartTag> Members

		/// <summary>
		/// Returns tags on demand.
		/// </summary>
		/// <param name="spans">Spans collection to get tags for.</param>
		/// <returns>Squiggle tags in provided spans.</returns>
		public IEnumerable<ITagSpan<SpellSmartTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			if (spans.Count == 0)
				yield break;

			ITextSnapshot snapshot = spans[0].Snapshot;

			foreach (var misspelling in _misspellingAggregator.GetTags(spans))
			{
				var misspellingSpans = misspelling.Span.GetSpans(snapshot);
				if (misspellingSpans.Count != 1)
					continue;

				SnapshotSpan errorSpan = misspellingSpans[0];

				yield return new TagSpan<SpellSmartTag>(errorSpan,
					new SpellSmartTag(GetSmartTagActions(errorSpan, misspelling.Tag.Suggestions)));
			}
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		#endregion

		#region IDisposable

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					_misspellingAggregator.Dispose();
					_misspellingAggregator = null;
				}

				disposed = true;
			}
		}

		#endregion

		#region Helpers

		private ReadOnlyCollection<SmartTagActionSet> GetSmartTagActions(SnapshotSpan errorSpan,
		                                                                 IEnumerable<string> suggestions)
		{
			List<SmartTagActionSet> smartTagSets = new List<SmartTagActionSet>();

			ITrackingSpan trackingSpan = errorSpan.Snapshot.CreateTrackingSpan(errorSpan, SpanTrackingMode.EdgeExclusive);

			// Add spelling suggestions (if there are any)
			List<ISmartTagAction> actions = new List<ISmartTagAction>();

			foreach (var suggestion in suggestions)
				actions.Add(new SpellSmartTagAction(trackingSpan, suggestion, true));

			if (actions.Count > 0)
				smartTagSets.Add(new SmartTagActionSet(actions.AsReadOnly()));

			// Add Dictionary operations (ignore all)
			List<ISmartTagAction> dictionaryActions = new List<ISmartTagAction>();
			dictionaryActions.Add(new SpellDictionarySmartTagAction(trackingSpan, _dictionary, "Ignore", Lang.Ru | Lang.En));
			smartTagSets.Add(new SmartTagActionSet(dictionaryActions.AsReadOnly()));

			return smartTagSets.AsReadOnly();
			
		}

		#endregion

		#region Event handlers

		private void RaiseTagsChangedEvent(SnapshotSpan subjectSpan)
		{
			EventHandler<SnapshotSpanEventArgs> handler = this.TagsChanged;
			if (handler != null)
			{
				handler(this, new SnapshotSpanEventArgs(subjectSpan));
			}
		}

		#endregion
	}
}