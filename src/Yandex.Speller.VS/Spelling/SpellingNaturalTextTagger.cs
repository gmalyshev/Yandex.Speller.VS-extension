using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Yandex.Speller.Api.DataContract;
using Yandex.Speller.VS.Tags;

namespace Yandex.Speller.VS.Spelling
{
	internal sealed class SpellingNaturalTextTagger : SpellingBase<NaturalTextTag>
	{
		public SpellingNaturalTextTagger(ITextBuffer buffer, ITagAggregator<NaturalTextTag> naturalTextTagger
		                                 )
			: base(buffer, naturalTextTagger) {}


		protected override IEnumerable<MisspellingTag> GetMisspellingsInSpan(SnapshotSpan span)
		{
			string text = span.GetText();
			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == ' ' || text[i] == '\t' || text[i] == '\r' || text[i] == '\n')
					continue;
			    bool isFoundUppercase = false;

				int end = i;
				try
				{
					for (; end < text.Length; end++)
					{
						char c = text[end];
						isFoundUppercase = isFoundUppercase || (char.IsUpper(c)&&(end>i));
						if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
							break;
					}

					if (isFoundUppercase) continue;
					string textToParse = text.Substring(i, end - i);


					Error result;
					if (!Dictionary.ContainsWord(textToParse, Lang.Ru | Lang.En, out result))
					{
						var errorSpan = new SnapshotSpan(span.Snapshot, span.Start + i + result.Pos, result.Len);
						yield return new MisspellingTag(errorSpan, result.Steer.ToArray());
					}
				}
				finally
				{
					// Move past this word
					i = end - 1;
				}
			}
		}
	}
}