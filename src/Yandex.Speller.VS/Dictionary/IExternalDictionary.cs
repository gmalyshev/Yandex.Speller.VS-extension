using System;
using Yandex.Speller.Api.DataContract;

namespace Yandex.Speller.VS.Dictionary
{
	/// <summary>
	/// The interface defines the external dictionary.
	/// </summary>
	public interface IExternalDictionary
	{
		/// <summary>
		/// Add a word to the dictionary.
		/// </summary>
		/// <param name="word"></param>
		/// <param name="lang"></param>
		void AddWord(string word, Lang lang);

		/// <summary>
		/// Does dictionary contain the word?
		/// </summary>
		/// <param name="word"></param>
		/// <param name="lang"></param>
		/// <returns></returns>
		bool ContainWord(string word, Lang lang);

		/// <summary>
		/// Raised when a new word is added to the dictionary, with the word.
		/// that was added.
		/// </summary>
		event EventHandler<AddedWordToDictionaryEventArgs> DictionaryUpdated;
	}
}