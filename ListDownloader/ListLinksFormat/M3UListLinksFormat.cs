using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ListDownloader
{
	/// <summary>
	/// Формат списка ссылок: файл M3U
	/// Извлекает ссылки и названия треков из M3U
	/// </summary>
	class M3UListLinksFormat : IListLinksFormat
	{
		public string mFilePath { get; private set; }
		public Encoding mEncoding { get; private set; }

		// Казалось бы, у этих форматов по определению
		// фиксирована кодировка, однако, на это правило
		// большинство составителей таких файлов кладут болт.
		public M3UListLinksFormat( string file_path, Encoding encoding )
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
				{
					string data;
					switch( LineProcess( out data, reader.ReadLine().Trim() ) )
					{
						case LineProcessResult.Caption:
							result.Add( new LinkInfo() { mCaption = data } );
							break;

						case LineProcessResult.Link:
							if( result.Count > 0 )
								result.Last().mUrl = data;
							else
								result.Add( new LinkInfo() { mUrl = data } );
							break;

						case LineProcessResult.None:
						case LineProcessResult.ExtM3U:
							break;
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
					string data;
					switch( LineProcess( out data, reader.ReadLine().Trim() ) )
					{
						case LineProcessResult.Link:
							if( data == link_info.mUrl )
								this_link_line = line_num;
							else
								prev_block_line = line_num;
							break;

						case LineProcessResult.ExtM3U:
							prev_block_line = line_num;
							break;

						case LineProcessResult.Caption:
						case LineProcessResult.None:
							break;
					}

					if( this_link_line != -1 )
						break;
				}
			}

			if( this_link_line == -1 )
				throw new Exception( $"DeleteLink: link '{link_info.mUrl}' is not found." );
			if( prev_block_line == -1 )
				throw new Exception( $"DeleteLink: previous block for link '{link_info.mUrl}' is not found." );

			Helpers.RemoveLinesFromFile( mFilePath, mEncoding, prev_block_line + 1, this_link_line );
		}

		// Private:

		enum LineProcessResult
		{
			None,
			ExtM3U,
			Caption,
			Link
		}

		LineProcessResult LineProcess( out string result, string line )
		{
			result = "";
			if( string.IsNullOrEmpty( line ) )
				return LineProcessResult.None;

			if( line.StartsWith( "#EXTM3U" ) )
				return LineProcessResult.ExtM3U;

			if( line.StartsWith( "#EXTINF:" ) )
			{
				result = ExtractExtinfCaption( line );
				return LineProcessResult.Caption;
			}

			if( Helpers.IsURLValid( line ) )
			{
				result = line;
				return LineProcessResult.Link;
			}

			return LineProcessResult.None;
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
