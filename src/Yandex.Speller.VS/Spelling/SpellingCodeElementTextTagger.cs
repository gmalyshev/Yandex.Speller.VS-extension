using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Yandex.Speller.Api.DataContract;
using Yandex.Speller.VS.Tags;

namespace Yandex.Speller.VS.Spelling
{
    internal sealed class SpellingCodeElementTextTagger : SpellingBase<CodeElementTextTag>
    {
        const string FrameSymbols=" \t\r\n_";
        public SpellingCodeElementTextTagger(ITextBuffer buffer, ITagAggregator<CodeElementTextTag> naturalTextTagger
            ) : base(buffer, naturalTextTagger)
        {
        }

        #region Helpers

        protected override IEnumerable<MisspellingTag> GetMisspellingsInSpan(SnapshotSpan span)
        {
            var text = span.GetText();

            for (var i = 0; i < text.Length; i++)
            {
                if (FrameSymbols.Contains(text[i].ToString()) || char.IsDigit( text[i]))
                {
                    continue;
                }

                var end = i+1;
                for (; end < text.Length; end++)
                {
                    if (FrameSymbols.Contains(text[end].ToString()) || char.IsDigit(text[end])|| char.IsUpper(text[end]))
                    {
                        break;
                    }
                }

                if (end - i > 1)
                {
                    var textToParse = text.Substring(i, end - i);
                    Error result;
                    if (!Dictionary.ContainsWord(textToParse, Lang.En, out result))
                    {
                        var errorSpan = new SnapshotSpan(span.Snapshot, span.Start + i + result.Pos, result.Len);
                        yield return new MisspellingTag(errorSpan, result.Steer.ToArray());
                    }
                }

                // Move past this word
                i = end - 1;
            }
        }

        #endregion
    }
}