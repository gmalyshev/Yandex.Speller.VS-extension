using System;
using System.Collections.Generic;
using System.Linq;
using Yandex.Speller.Api.DataContract;

namespace Yandex.Speller.VS.Dictionary
{
	/// <summary>
	/// Implementation <see cref="IExternalDictionary" />.
	/// </summary>
	public class ExternalDictionary : IExternalDictionary
	{
		private readonly IDictionary<Lang, ThreadSafetyDictionaryFile> _dictionary =
			new Dictionary<Lang, ThreadSafetyDictionaryFile>();

		private readonly Lang[] _supportLang = Enum.GetValues(typeof (Lang)).Cast<Lang>().ToArray();

		/// <summary>
		/// Constructor.
		/// </summary>
		public ExternalDictionary(IEnumerable<KeyValuePair<Lang, string>> dictionaryFileName)
		{
			foreach (var dicFile in dictionaryFileName)
			{
				_dictionary.Add(dicFile.Key, new ThreadSafetyDictionaryFile(dicFile.Value));
			}
		}


		/// <summary>
		/// Add a word to the dictionary
		/// </summary>
		/// <param name="word"></param>
		/// <param name="lang"></param>
		public void AddWord(string word, Lang lang)
		{
			bool wordAdd = false;
			foreach (Lang  langVariant in _supportLang)
			{
				if (!lang.HasFlag(langVariant)) continue;
				_dictionary[langVariant].AddWord(word);
				wordAdd = true;
			}

			if (wordAdd) RaiseAddWordEvent(word);
		}

		/// <summary>
		/// Does dictionary contain the word?
		/// </summary>
		/// <param name="word"></param>
		/// <param name="lang"></param>
		/// <returns></returns>
		public bool ContainWord(string word, Lang lang)
		{
			bool result = false;
			foreach (Lang langVariant in _supportLang)
			{
				if (lang.HasFlag(langVariant))
					result = result || (_dictionary.ContainsKey(langVariant) && _dictionary[langVariant].ContainsWord(word));
			}
			return result;
		}

		/// <summary>
		/// Raised when a new word is added to the dictionary, with the word
		/// that was added.
		/// </summary>
		public event EventHandler<AddedWordToDictionaryEventArgs> DictionaryUpdated;


		private void RaiseAddWordEvent(string word)
		{
			EventHandler<AddedWordToDictionaryEventArgs> handler = DictionaryUpdated;
			if (handler != null)
				DictionaryUpdated(this, new AddedWordToDictionaryEventArgs(word));
		}
	}
}