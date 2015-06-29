using Microsoft.VisualStudio.Text.Tagging;

namespace Yandex.Speller.VS.Tags
{
	/// <summary>
	/// Squiggle tag for misspelled words.
	/// </summary>
	internal class SpellSquiggleTag : ErrorTag
	{
		public SpellSquiggleTag(string squiggleType, object toolTipContent) : base(squiggleType, toolTipContent) {}
	}
}