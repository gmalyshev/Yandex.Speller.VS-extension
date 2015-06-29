using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Yandex.Speller.VS.Dictionary
{
	public class ThreadSafetyDictionaryFile
	{
		private readonly string _fileName;
		private readonly HashSet<string> _hashSet = new HashSet<string>();
		private readonly Mutex _mutex;
		private DateTime _fileWriteTime = DateTime.MinValue;

		public ThreadSafetyDictionaryFile(string fileName)
		{
			_fileName = fileName;
			_mutex = new Mutex(false, "Global\\" + fileName.Replace('/', '_').Replace('\\', '_'));
			ReadFile();
		}

		private void ReadFile()
		{
			try
			{
				_mutex.WaitOne();
				if (File.Exists(_fileName))
				{
					_hashSet.Clear();
					using (TextReader reader = new StreamReader(_fileName, Encoding.UTF8))
					{
						string word;
						while ((word = reader.ReadLine()) != null)
						{
							_hashSet.Add(word);
						}

						_fileWriteTime = GetFileWriteDateTime();
					}
				}
			}
			finally
			{
				_mutex.ReleaseMutex();
			}
		}

		private DateTime GetFileWriteDateTime()
		{
			if (File.Exists(_fileName))
			{
				return File.GetLastWriteTime(_fileName);
			}

			return DateTime.MinValue;
		}

		public void AddWord(string word)
		{
			try
			{
				_mutex.WaitOne();
				if (_fileWriteTime < GetFileWriteDateTime())
				{
					ReadFile();
				}
				using (var writer = new StreamWriter(_fileName, true))
				{
					writer.WriteLine(word);
					writer.Flush();
					_hashSet.Add(word);
					_fileWriteTime = GetFileWriteDateTime();
				}
			}
			finally
			{
				_mutex.ReleaseMutex();
			}
		}

		public bool ContainsWord(string word)
		{
			return _hashSet.Contains(word);
		}
	}
}