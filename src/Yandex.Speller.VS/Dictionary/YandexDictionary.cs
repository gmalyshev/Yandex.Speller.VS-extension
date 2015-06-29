using System;
using Yandex.Speller.Api;
using Yandex.Speller.Api.DataContract;

namespace Yandex.Speller.VS.Dictionary
{
	internal class YandexDictionary : IYandexDictionary
	{
		private readonly IExternalDictionary _externalDictionary;
		private readonly object _sync = new object();
		private readonly IYandexSpeller _yandexSpeller;
		private bool _innerUpdate;

		public YandexDictionary(IYandexSpeller yandexSpeller, IExternalDictionary externalDictionary)
		{
			_yandexSpeller = yandexSpeller;
			_externalDictionary = externalDictionary;
			_externalDictionary.DictionaryUpdated += _externalDictionary_DictionaryUpdated;
		}

		/// <summary>
		/// Dictionary contains the word.
		/// </summary>
		public bool ContainsWord(string word, Lang lang, out Error error)
		{
			lock (_sync)
			{
				error = null;
				if (_externalDictionary.ContainWord(word, lang))
					return true;

				SpellResult result = _yandexSpeller.CheckText(word, lang, Options.ByWords, TextFormat.Plain);
				if (result.Errors.Count > 0)
				{
					error = result.Errors[0];
					return false;
				}

				_innerUpdate = true;

				try
				{
					_externalDictionary.AddWord(word, lang);
				}
				finally
				{
					_innerUpdate = false;
				}


				return true;
			}
		}

		/// <summary>
		/// Add a word to the dictionary
		/// </summary>
		public void AddWord(string word, Lang lang)
		{
			lock (_externalDictionary)
			{
				_externalDictionary.AddWord(word, lang);
			}
		}

		/// <summary>
		/// Does dictionary contain the word?
		/// </summary>
		/// <param name="word"></param>
		/// <param name="lang"></param>
		/// <returns></returns>
		public bool ContainWord(string word, Lang lang)
		{
			Error error;
			return ContainsWord(word, lang, out error);
		}

		public event EventHandler<AddedWordToDictionaryEventArgs> DictionaryUpdated;

		private void _externalDictionary_DictionaryUpdated(object sender, AddedWordToDictionaryEventArgs e)
		{
			if (_innerUpdate) return;
			EventHandler<AddedWordToDictionaryEventArgs> handle = DictionaryUpdated;
			if (!ReferenceEquals(handle, null)) handle(sender, e);
		}
	}
}