using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Yandex.Speller.Api.DataContract;
using Yandex.Speller.VS.Dictionary;

namespace Yandex.Speller.VS.SmartTags
{
	/// <summary>
	/// Smart tag action for adding new words to the dictionary.
	/// </summary>
	internal class SpellDictionarySmartTagAction : ISmartTagAction
	{
		#region Private data

		private readonly Lang _lang;
		private IYandexDictionary _dictionary;
		private ITrackingSpan _span;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor for SpellDictionarySmartTagAction.
		/// </summary>
		/// <param name="span">Word to add to dictionary.</param>
		/// <param name="dictionary">The dictionary (used to ignore the word).</param>
		/// <param name="displayText">Text to show in the context menu for this action.</param>
		public SpellDictionarySmartTagAction(ITrackingSpan span, IYandexDictionary dictionary, string displayText, Lang lang)
		{
			_span = span;
			_dictionary = dictionary;
			_lang = lang;
			DisplayText = displayText;
		}

		# endregion

		#region ISmartTagAction implementation

		/// <summary>
		/// Text to display in the context menu.
		/// </summary>
		public string DisplayText
		{
			get;
			private set;
		}

		/// <summary>
		/// Icon to place next to the display text.
		/// </summary>
		public System.Windows.Media.ImageSource Icon
		{
			get
			{
				return null;
			}
		}

		/// <summary>
		/// This method is executed when action is selected in the context menu.
		/// </summary>
		public void Invoke()
		{
			_dictionary.AddWord(_span.GetText(_span.TextBuffer.CurrentSnapshot), _lang);
		}

		/// <summary>
		/// Enable/disable this action.
		/// </summary>
		public bool IsEnabled
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Action set to make sub menus.
		/// </summary>
		public ReadOnlyCollection<SmartTagActionSet> ActionSets
		{
			get
			{
				return null;
			}
		}

		#endregion
	}
}