using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Yandex.Speller.VS.Dictionary;
using Yandex.Speller.VS.Tags;

namespace Yandex.Speller.VS.Spelling
{
	[Export(typeof (ITaggerProvider))]
	[ContentType("text")]
	[TagType(typeof (IMisspellingTag))]
	internal sealed class SpellingNaturalTextTaggerProvider : ITaggerProvider
	{
		private readonly IYandexDictionary SpellingDictionary = new Factory().CreateYandexDictionary();
#pragma warning disable
		[Import]
		private IBufferTagAggregatorFactoryService AggregatorFactory;
#pragma warning restore
		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
		{
			return buffer.Properties.GetOrCreateSingletonProperty(
				() =>
					new SpellingNaturalTextTagger(buffer, AggregatorFactory.CreateTagAggregator<NaturalTextTag>(buffer))) as ITagger<T>;
		}
	}
}