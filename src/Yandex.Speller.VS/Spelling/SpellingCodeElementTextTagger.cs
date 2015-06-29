using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Yandex.Speller.Api.DataContract;
using Yandex.Speller.VS.Tags;

namespace Yandex.Speller.VS.Spelling
{
	internal sealed class SpellingCodeElementTextTagger : SpellingBase<CodeElementTextTag>
	{
		public SpellingCodeElementTextTagger(ITextBuffer buffer, ITagAggregator<CodeElementTextTag> naturalTextTagger
		                                    ) : base(buffer, naturalTextTagger) {}

		#region Helpers

		protected override IEnumerable<MisspellingTag> GetMisspellingsInSpan(SnapshotSpan span)
		{
			string text = span.GetText();

			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == ' ' || text[i] == '\t' || text[i] == '\r' || text[i] == '\n'|| text[i]=='_')
					continue;

				int end = i;

				for (; end < text.Length; end++)
				{
					char c = text[end];

					if (c == ' ' || c == '\t' || c == '\r' || c == '\n' || c=='_' ||(char.IsUpper(c) && (end > i)))
						break;
				}


				string textToParse = text.Substring(i, end - i);


				Error result;
				if (!Dictionary.ContainsWord(textToParse, Lang.En, out result))
				{
					SnapshotSpan errorSpan = new SnapshotSpan(span.Snapshot, span.Start + i + result.Pos, result.Len);
					yield return new MisspellingTag(errorSpan, result.Steer.ToArray());
				}

				// Move past this word
				i = end - 1;
			}
		}

		#endregion
	}
}