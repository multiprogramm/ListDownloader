using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ListDownloader
{
	/// <summary>
	/// URL и инфа о нём
	/// </summary>
	class LinkInfo
	{
		public string mUrl;
		public string mCaption;
	}

	/// <summary>
	/// Интерфейс доставателя инфы о линках
	/// </summary>
	interface ILinksExtractor
	{
		/// <summary>
		/// Достать линки и инфу о них
		/// </summary>
		List<LinkInfo> ExtractLinks();
	}
	
	/// <summary>
	/// Разные вещи по извлечению ссылок
	/// </summary>
	static class LinksTools
	{
		/// <summary>
		/// Фабричный метод для создания извлекателя под файл.
		/// </summary>
		static public ILinksExtractor CreateExtractor( string filePath, Encoding encoding )
		{
			string ext = Path.GetExtension( filePath ).ToLowerInvariant();
			ILinksExtractor result;
			if( ext == ".m3u" || ext == ".m3u8" )
				result = new M3UListExtractor( filePath, encoding );
			else
				result = new TxtListLinksExtractor( filePath, encoding );

			return result;
		}

		/// <summary>
		/// Удаляет все пустые линки
		/// </summary>
		static public void DeleteEmptyLinks( List<LinkInfo> links )
		{
			links.RemoveAll( link_info => {
				return string.IsNullOrEmpty( link_info.mUrl );
			} );
		}

		/// <summary>
		/// Заполнение пустых mCaption у линков
		/// </summary>
		static public void FillEmptyCaptions( List<LinkInfo> links )
		{
			char[] pathSplit = new char[] { '/', '\\' };
			foreach(var info in links )
			{
				if( string.IsNullOrEmpty( info.mCaption ) )
				{
					Uri url = new Uri( info.mUrl );
					string[] pathParts = url.LocalPath.Split( pathSplit, StringSplitOptions.RemoveEmptyEntries );
					if( pathParts.Count() != 0 )
					{
						string lastPath = pathParts.Last();
						int dot = lastPath.LastIndexOf( '.' );
						if( dot >= 0 && dot < lastPath.Count() - 1 )
							info.mCaption = lastPath.Substring( dot );
					}
					
					if( string.IsNullOrEmpty( info.mCaption ) )
						info.mCaption = Helpers.CalcMD5( info.mUrl );
				}
			}
		}
	}
}
