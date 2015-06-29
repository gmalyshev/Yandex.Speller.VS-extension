using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Yandex.Speller.VS.Tags;

namespace Yandex.Speller.VS.Spelling
{
	internal class MisspellingTag : IMisspellingTag
	{
		public MisspellingTag(SnapshotSpan span, IEnumerable<string> suggestions)
		{
			Span = span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);
			Suggestions = suggestions;
		}

		public ITrackingSpan Span { get; private set; }

		public IEnumerable<string> Suggestions { get; private set; }

		public ITagSpan<IMisspellingTag> ToTagSpan(ITextSnapshot snapshot)
		{
			return new TagSpan<IMisspellingTag>(Span.GetSpan(snapshot), this);
		}
	}
}