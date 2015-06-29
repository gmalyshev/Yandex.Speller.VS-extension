using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Tagging;

namespace Yandex.Speller.VS.Tags
{
	internal interface IMisspellingTag : ITag
	{
		IEnumerable<string> Suggestions
		{
			get;
		}
	}
}