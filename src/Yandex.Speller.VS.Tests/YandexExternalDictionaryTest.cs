using System;
using System.Collections.Generic;
using Moq;
using Ploeh.AutoFixture;
using Xunit;
using Yandex.Speller.Api;
using Yandex.Speller.Api.DataContract;
using Yandex.Speller.VS.Dictionary;

namespace Yandex.Speller.VS.Tests
{
	public class YandexExternalDictionaryTest
	{
		private readonly Fixture _fixture;

		public YandexExternalDictionaryTest()
		{
			_fixture = new Fixture();
		}

		[Theory, AutoMoqData]
		public void Test(string s)
		{
			Assert.NotNull(s);
		}

		[Theory, AutoMoqData]
		public void YandexDictionary_Create(IYandexSpeller yandexSpeller, MockExternalDictionary mockExternalDictionary)
		{
			IYandexDictionary yandex = new YandexDictionary(yandexSpeller, mockExternalDictionary);
			var word = _fixture.Create<string>();
			Error error;
			Assert.False(yandex.ContainsWord(word, Lang.En, out error));
			yandex.AddWord(word, Lang.En);
			Assert.True(yandex.ContainsWord(word, Lang.En, out error));
		}

		[Theory, AutoMoqData]
		public void YandexDictionary_YandexAPIRequest(IYandexSpeller yandexSpeller,
			MockExternalDictionary mockExternalDictionary)
		{
			IYandexDictionary yandex = new YandexDictionary(yandexSpeller, mockExternalDictionary);
			var word = _fixture.Create<string>();
			Error error;
			Mock.Get(yandexSpeller)
				.Setup(x => x.CheckText(word, Lang.En, It.IsAny<Options>(), It.IsAny<TextFormat>()))
				.Returns(new SpellResult {Errors = new List<Error>()});
			Assert.True(yandex.ContainsWord(word, Lang.En, out error));
		}

		public class MockExternalDictionary : IExternalDictionary
		{
			private readonly IDictionary<Lang, IList<string>> _dictionary = new Dictionary<Lang, IList<string>>();

			public virtual void AddWord(string word, Lang lang)
			{
				if (_dictionary.ContainsKey(lang))
				{
					_dictionary[lang].Add(word);
				}
				else
				{
					_dictionary.Add(lang, new List<string> {word});
				}

				OnDictionaryUpdated(new AddedWordToDictionaryEventArgs(word));
			}

			public virtual bool ContainWord(string word, Lang lang)
			{
				if (_dictionary.ContainsKey(lang))
				{
					return _dictionary[lang].Contains(word);
				}

				return false;
			}

			public event EventHandler<AddedWordToDictionaryEventArgs> DictionaryUpdated;

			protected virtual void OnDictionaryUpdated(AddedWordToDictionaryEventArgs e)
			{
				EventHandler<AddedWordToDictionaryEventArgs> handler = DictionaryUpdated;
				if (handler != null) handler(this, e);
			}
		}
	}
}