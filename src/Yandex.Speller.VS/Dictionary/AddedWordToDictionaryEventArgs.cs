using System;

namespace Yandex.Speller.VS.Dictionary
{
	/// <summary />
	public class AddedWordToDictionaryEventArgs : EventArgs
	{
		/// <summary />
		public AddedWordToDictionaryEventArgs(string word)
		{
			Word = word;
		}

		/// <summary>
		/// Word placed in the Dictionary.
		/// </summary>
		public string Word
		{
			get;
			private set;
		}
	}
}