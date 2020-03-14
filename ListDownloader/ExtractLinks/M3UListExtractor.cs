using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ListDownloader
{
	/// <summary>
	/// Извлекатель ссылок и названий треков из M3U
	/// </summary>
	class M3UListExtractor : ILinksExtractor
	{
		public string mFilePath { get; private set; }
		public Encoding mEncoding { get; private set; }

		// Казалось бы, у этих форматов по определению
		// фиксирована кодировка, однако, на это правило
		// большинство составителей таких файлов кладут болт.
		public M3UListExtractor( string file_path, Encoding encoding )
		{
			mFilePath = file_path;
			mEncoding = encoding;
		}

		public List<LinkInfo> ExtractLinks()
		{
			List<LinkInfo> result = new List<LinkInfo>();
			using( StreamReader reader = new StreamReader( mFilePath, mEncoding ) )
			{
				while( !reader.EndOfStream )
					LineProcess( result, reader.ReadLine().Trim() );
			}

			return result;
		}

		// Private:

		void LineProcess( List<LinkInfo> list_links, string line )
		{
			if( string.IsNullOrEmpty( line ) || line.StartsWith( "#EXTM3U" ) )
				return;

			if( line.StartsWith( "#EXTINF:" ) )
			{
				string caption = ExtractExtinfCaption( line );
				list_links.Add( new LinkInfo() { mCaption = caption } );
			}
			else if( Helpers.IsURLValid( line ) )
			{
				list_links.Last().mUrl = line;
			}
		}

		string ExtractExtinfCaption( string line )
		{
			int idx = line.IndexOf( ',' );
			if( idx < 0 )
				return "";
			string caption = line.Substring( idx + 1 );
			caption = caption.Trim();
			return caption;
		}
	}
}
