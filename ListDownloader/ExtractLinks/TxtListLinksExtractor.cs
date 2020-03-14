using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ListDownloader
{
	/// <summary>
	/// Извлекатель ссылок и названий из текстового файла,
	/// так, что названием считается каждая предыдущая непустая строка
	/// предшевствующая линку
	/// </summary>
	class TxtListLinksExtractor : ILinksExtractor
	{
		public string mFilePath { get; private set; }
		public Encoding mEncoding { get; private set; }

		public TxtListLinksExtractor( string file_path, Encoding encoding )
		{
			mFilePath = file_path;
			mEncoding = encoding;
		}

		public List<LinkInfo> ExtractLinks()
		{
			List<LinkInfo> result = new List<LinkInfo>();
			string prevCaption = "";
			using( StreamReader reader = new StreamReader( mFilePath, mEncoding ) )
			{
				while( !reader.EndOfStream )
				{
					string line = reader.ReadLine().Trim();
					if( string.IsNullOrEmpty( line ) )
						continue;

					if( Helpers.IsURLValid( line ) )
					{
						result.Add( new LinkInfo() { mUrl = line, mCaption = prevCaption } );
						prevCaption = "";
					}
					else
					{
						prevCaption = line.Trim();
					}
				}
			}

			return result;
		}
	}
}
