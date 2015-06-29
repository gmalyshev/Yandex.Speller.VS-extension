using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Yandex.Speller.Api.DataContract;
using Yandex.Speller.VS.Dictionary;

namespace Yandex.Speller.VS.Tests
{
	public class ExternalDictionaryTest
	{
		[Fact]
		public void ThreadSafetyDictionaryFile_DictionaryFile_OneThread()
		{
			var dic = new ThreadSafetyDictionaryFile(Path.GetTempFileName());
			dic.AddWord("test");
			Assert.True(dic.ContainsWord("test"));
			Assert.False(dic.ContainsWord("no_test_word"));
		}

		[Fact]
		public void ThreadSafetyDictionaryFile_DictionaryFile_MultiThread()
		{
			var dic = new ThreadSafetyDictionaryFile(Path.GetTempFileName());


			Task ta = Task.Run(() =>
			{
				for (int i = 0; i < 100; i++)
				{
					dic.AddWord("a" + i);
				}
			});

			Task tb = Task.Run(() =>
			{
				for (int i = 0; i < 100; i++)
				{
					dic.AddWord("b" + i);
				}
			});

			Task tc = Task.Run(() =>
			{
				for (int i = 0; i < 100; i++)
				{
					dic.AddWord("c" + i);
				}
			});

			Task td = Task.Run(() =>
			{
				for (int i = 0; i < 100; i++)
				{
					dic.AddWord("d" + i);
				}
			});


			Task.WaitAll(ta, tb, tc, td);
			for (int i = 0; i < 100; i++)
			{
				Assert.True(dic.ContainsWord("a" + i));
				Assert.True(dic.ContainsWord("b" + i));
				Assert.True(dic.ContainsWord("c" + i));
				Assert.True(dic.ContainsWord("d" + i));
			}
		}

		[Fact]
		public void ThreadSafetyDictionaryFile_DictionaryFile_MultiThreadAndInstance()
		{
			string fileName = Path.GetTempFileName();
			Task ta = Task.Run(() =>
			{
				var dic1 = new ThreadSafetyDictionaryFile(fileName);

				for (int i = 0; i < 100; i++)
				{
					dic1.AddWord("a" + i);
				}
			});

			Task tb = Task.Run(() =>
			{
				var dic2 = new ThreadSafetyDictionaryFile(fileName);

				for (int i = 0; i < 100; i++)
				{
					dic2.AddWord("b" + i);
				}
			});

			Task tc = Task.Run(() =>
			{
				var dic3 = new ThreadSafetyDictionaryFile(fileName);

				for (int i = 0; i < 100; i++)
				{
					dic3.AddWord("c" + i);
				}
			});

			Task td = Task.Run(() =>
			{
				var dic4 = new ThreadSafetyDictionaryFile(fileName);

				for (int i = 0; i < 100; i++)
				{
					dic4.AddWord("d" + i);
				}
			});

			var dic = new ThreadSafetyDictionaryFile(fileName);
			Task.WaitAll(ta, tb, tc, td);


			dic.AddWord("test");
			for (int i = 0; i < 100; i++)
			{
				Assert.True(dic.ContainsWord("a" + i));
				Assert.True(dic.ContainsWord("b" + i));
				Assert.True(dic.ContainsWord("c" + i));
				Assert.True(dic.ContainsWord("d" + i));
			}
		}

		[Fact]
		public void ExternalDictionary_Create()
		{
			string fileName = Path.GetTempFileName();
			var dictionary = new ExternalDictionary(new[] {new KeyValuePair<Lang, string>(Lang.En, fileName)});
			dictionary.AddWord("test", Lang.En);
			Assert.True(dictionary.ContainWord("test", Lang.En));
		}

		[Fact]
		public void ExternalDictionary_LangSelect()
		{
			string fileName = Path.GetTempFileName();
			var dictionary = new ExternalDictionary(new[] {new KeyValuePair<Lang, string>(Lang.En, fileName)});
			dictionary.AddWord("test", Lang.En);
			Assert.False(dictionary.ContainWord("test", Lang.Ru));
			Assert.False(dictionary.ContainWord("test", Lang.Uk));
			Assert.True(dictionary.ContainWord("test", Lang.En));
		}

		[Fact]
		public void ExternalDictionary_LangSelect2()
		{
			string fileName1 = Path.GetTempFileName();
			string fileName2 = Path.GetTempFileName();
			var dictionary =
				new ExternalDictionary(new[]
				{new KeyValuePair<Lang, string>(Lang.En, fileName1), new KeyValuePair<Lang, string>(Lang.Ru, fileName2)});
			dictionary.AddWord("test_en", Lang.En);
			dictionary.AddWord("test_ru", Lang.Ru);
			dictionary.AddWord("test_ru_en", Lang.Ru | Lang.En);
			Assert.False(dictionary.ContainWord("test_en", Lang.Ru));
			Assert.False(dictionary.ContainWord("test_ru", Lang.En));
			Assert.True(dictionary.ContainWord("test_en", Lang.En));
			Assert.True(dictionary.ContainWord("test_ru", Lang.Ru));
			Assert.True(dictionary.ContainWord("test_ru_en", Lang.Ru));
			Assert.True(dictionary.ContainWord("test_ru_en", Lang.En));
			Assert.True(dictionary.ContainWord("test_ru_en", Lang.En | Lang.Ru));
		}
	}
}