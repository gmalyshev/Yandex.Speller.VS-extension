using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Yandex.Speller.VS.Tags;

namespace Yandex.Speller.VS.Taggers
{
	/// <summary>
	/// Provides tags for UserType and variable names
	/// </summary>
	internal class CodeElementTextTagger : ITagger<CodeElementTextTag>
	{
		#region Private Fields

		private ITextBuffer _buffer;
		private IClassifier _classifier;

		#endregion

		#region MEF Imports / Exports

		/// <summary>
		/// MEF connector for the Natural Text Tagger.
		/// </summary>
		[Export(typeof (ITaggerProvider))]
		[ContentType("code")]
		[TagType(typeof (CodeElementTextTag))]
		internal class CodeElementTextTaggerProvider : ITaggerProvider
		{
			#region MEF Imports

			[Import]
			internal IClassifierAggregatorService ClassifierAggregatorService
			{
				get;
				set;
			}

			#endregion

			#region ITaggerProvider

			public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
			{
				if (buffer == null)
				{
					throw new ArgumentNullException("buffer");
				}

				return new CodeElementTextTagger(buffer, ClassifierAggregatorService.GetClassifier(buffer)) as ITagger<T>;
			}

			#endregion
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor for Natural Text Tagger.
		/// </summary>
		/// <param name="buffer">Relevant buffer.</param>
		/// <param name="classifier">List of all available classifiers.</param>
		public CodeElementTextTagger(ITextBuffer buffer, IClassifier classifier)
		{
			_buffer = buffer;
			_classifier = classifier;
		}

		#endregion

		#region ITagger<CodeElementText> Members

		/// <summary>
		/// Returns tags on demand.
		/// </summary>
		/// <param name="spans">Spans collection to get tags for.</param>
		/// <returns>Tags in provided spans.</returns>
		public IEnumerable<ITagSpan<CodeElementTextTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			if (_classifier != null)
			{
				foreach (var snapshotSpan in spans)
				{
					Debug.Assert(snapshotSpan.Snapshot.TextBuffer == _buffer);
					foreach (ClassificationSpan classificationSpan in _classifier.GetClassificationSpans(snapshotSpan))
					{
						if (
							classificationSpan.ClassificationType.ToString().ToLower(CultureInfo.InvariantCulture).Contains("user types") ||
							classificationSpan.ClassificationType.ToString().ToLower(CultureInfo.InvariantCulture).Contains("identifier"))
						{
							yield return new TagSpan<CodeElementTextTag>(
								classificationSpan.Span,
								new CodeElementTextTag()
								);
						}
					}
				}
			}
		}

#pragma warning disable 67
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
#pragma warning restore 67

		#endregion
	}
}