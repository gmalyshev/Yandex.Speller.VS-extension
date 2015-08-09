using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Yandex.Speller.VS.Tags;

namespace Yandex.Speller.VS.Taggers
{
	/// <summary>
	/// Provides tags for text files.
	/// </summary>
	internal class NaturalTextTagger : ITagger<NaturalTextTag>
	{
		#region Private Fields

		private ITextBuffer _buffer;

		#endregion

		#region MEF Imports / Exports

		/// <summary>
		/// MEF connector for the Natural Text Tagger.
		/// </summary>
		[Export(typeof (ITaggerProvider))]
		[ContentType("plaintext")]
		[TagType(typeof (NaturalTextTag))]
		internal class NaturalTextTaggerProvider : ITaggerProvider
		{
			public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
			{
				if (buffer == null)
				{
					throw new ArgumentNullException("buffer");
				}

				return new NaturalTextTagger(buffer) as ITagger<T>;
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor for Natural Text Tagger.
		/// </summary>
		/// <param name="buffer">Relevant buffer.</param>
		public NaturalTextTagger(ITextBuffer buffer)
		{
			_buffer = buffer;
		}

		#endregion

		#region ITagger<NaturalTextTag> Members

		/// <summary>
		/// Returns tags on demand.
		/// </summary>
		/// <param name="spans">Spans collection to get tags for.</param>
		/// <returns>Tags in provided spans.</returns>
		public IEnumerable<ITagSpan<NaturalTextTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			foreach (var snapshotSpan in spans)
			{
				Debug.Assert(snapshotSpan.Snapshot.TextBuffer == _buffer);
				yield return new TagSpan<NaturalTextTag>(
					snapshotSpan,
					new NaturalTextTag()
					);
			}
		}

#pragma warning disable 67
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
#pragma warning restore 67

		#endregion
	}
}