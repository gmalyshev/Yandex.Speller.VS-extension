using System.Collections.Generic;
using Yandex.Speller.Api;
using Yandex.Speller.Api.DataContract;

namespace Yandex.Speller.VS
{
	internal class YandexCacheProxy : IYandexSpeller
	{
		private readonly IYandexSpeller _speller;

		private readonly Cache _cache = new Cache();

		public YandexCacheProxy(IYandexSpeller speller)
		{
			_speller = speller;
		}

		/// <summary>
		/// Checks spelling in this passage of text.
		/// </summary>
		SpellResult IYandexSpeller.CheckText(string text, Lang lang, Options options, TextFormat format)
		{
			CacheKey key = new CacheKey(text, lang, options, format);
			SpellResult result;
			if (!_cache.TryGetValue(key, out result))
			{
				result = _speller.CheckText(text, lang, options, format);
				_cache.Add(key, result);
			}
			return result;
		}

		/// <summary>
		/// Checks spelling in specified text fragments.
		/// </summary>
		/// <returns>For each fragment returns a separate array error prompts.</returns>
		SpellResult[] IYandexSpeller.CheckTexts(string[] text, Lang lang, Options options, TextFormat format)
		{
			List<SpellResult> result = new List<SpellResult>();
			foreach (var s in text)
			{
				CacheKey key = new CacheKey(s, lang, options, format);
				SpellResult cacheResult;
				if (!_cache.TryGetValue(key, out cacheResult))
				{
					cacheResult = _speller.CheckText(s, lang, options, format);
					_cache.Add(key, cacheResult);
				}
				result.Add(cacheResult);
			}

			return result.ToArray();
		}

		private class Cache : Dictionary<CacheKey, SpellResult> {}

		private struct CacheKey
		{
			private readonly TextFormat _format;
			private readonly Lang _lang;
			private readonly Options _options;
			private readonly string _text;

			public CacheKey(string text, Lang lang, Options options, TextFormat format)
			{
				_text = text;
				_lang = lang;
				_options = options;
				_format = format;
			}

			/// <summary />
			public bool Equals(CacheKey other)
			{
				return string.Equals(_text, other._text) && _lang == other._lang && _options == other._options &&
				       _format == other._format;
			}

			/// <summary>
			/// Returns the hash code for this instance.
			/// </summary>
			/// <returns>
			/// A 32-bit signed integer that is the hash code for this instance.
			/// </returns>
			public override int GetHashCode()
			{
				unchecked
				{
					int hashCode = (_text != null ? _text.GetHashCode() : 0);
					hashCode = (hashCode*397) ^ (int) _lang;
					hashCode = (hashCode*397) ^ (int) _options;
					hashCode = (hashCode*397) ^ (int) _format;
					return hashCode;
				}
			}

			/// <summary>
			/// Indicates whether this instance and a specified object are equal.
			/// </summary>
			/// <returns>
			/// true if <paramref name="obj" /> and this instance are the same type and represent the same value; otherwise, false.
			/// </returns>
			/// <param name="obj">Another object to compare to. </param>
			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				return obj is CacheKey && Equals((CacheKey) obj);
			}
		}
	}
}