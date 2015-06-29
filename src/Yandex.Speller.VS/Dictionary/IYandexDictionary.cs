using Yandex.Speller.Api.DataContract;

namespace Yandex.Speller.VS.Dictionary
{
	/// <summary>
	/// Dictionary representation by Yandex.
	/// </summary>
	public interface IYandexDictionary : IExternalDictionary
	{
		/// <summary>
		/// Dictionaty contains a word.
		/// </summary>
		bool ContainsWord(string word, Lang lang, out Error error);
	}
}