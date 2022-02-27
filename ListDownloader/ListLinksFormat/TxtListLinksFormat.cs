using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ListDownloader
{
	/// <summary>
	/// Формат списка ссылок: текстовый файл.
	/// Названием файла считается каждая предыдущая непустая строка,
	/// предшевствующая линку
	/// </summary>
	class TxtListLinksFormat : IListLinksFormat
	{
		public string mFilePath { get; private set; }
		public Encoding mEncoding { get; private set; }

		public TxtListLinksFormat( string file_path, Encoding encoding )
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

		public void DeleteLink( LinkInfo link_info )
		{
			int prev_block_line = -1;
			int this_link_line = -1;
			using( StreamReader reader = new StreamReader( mFilePath, mEncoding ) )
			{
				for( int line_num = 0; !reader.EndOfStream; ++line_num )
				{
					string line = reader.ReadLine().Trim();
					if( string.IsNullOrEmpty( line ) )
						continue;

					if( Helpers.IsURLValid( line ) )
					{
						if( line == link_info.mUrl )
						{
							this_link_line = line_num;
							break;
						}
						else
							prev_block_line = line_num;
					}
				}
			}

			if( this_link_line == -1 )
				throw new Exception( $"DeleteLink: link '{link_info.mUrl}' is not found." );

			Helpers.RemoveLinesFromFile( mFilePath, mEncoding, prev_block_line + 1, this_link_line );
		}
	}
}
