using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Yandex.Speller.VS.SmartTags
{
	internal class SpellSmartTag : Microsoft.VisualStudio.Language.Intellisense.SmartTag
	{
		public SpellSmartTag(ReadOnlyCollection<SmartTagActionSet> actionSets) :
			base(SmartTagType.Factoid, actionSets) {}
	}
}