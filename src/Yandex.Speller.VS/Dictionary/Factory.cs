using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Yandex.Speller.Api;
using Yandex.Speller.Api.DataContract;

namespace Yandex.Speller.VS.Dictionary
{
	public class Factory
	{
		private static readonly ExternalDictionary ExternalDictionary;

		static Factory()
		{
			string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			Dictionary<Lang, string> dictionary = Enum.GetValues(typeof (Lang))
				.Cast<Lang>()
				.ToDictionary(x => x, y => Path.Combine(folder, "dic" + y.ToString() + ".dic"));
			ExternalDictionary = new ExternalDictionary(dictionary);
		}

		public IYandexDictionary CreateYandexDictionary()
		{
			return new YandexDictionary(new YandexCacheProxy(new YandexSpeller()), ExternalDictionary);
		}
	}
}