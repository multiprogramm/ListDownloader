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
	/// Интерфейс формата списка ссылок
	/// </summary>
	interface IListLinksFormat
	{
		/// <summary>
		/// Достать линки и инфу о них
		/// </summary>
		List<LinkInfo> ExtractLinks();

		/// <summary>
		/// Удалить информацию об этой ссылке
		/// </summary>
		/// <param name="link_info">Информация о ссылке, которую нужно удалить</param>
		void DeleteLink( LinkInfo link_info );
	}
	
	/// <summary>
	/// Разные вещи по извлечению ссылок
	/// </summary>
	static class LinksTools
	{
		/// <summary>
		/// Фабричный метод для создания извлекателя под файл.
		/// </summary>
		static public IListLinksFormat CreateListLinksFormat( string filePath, Encoding encoding )
		{
			string ext = Path.GetExtension( filePath ).ToLowerInvariant();
			IListLinksFormat result;
			if( ext == ".m3u" || ext == ".m3u8" )
				result = new M3UListLinksFormat( filePath, encoding );
			else
				result = new TxtListLinksFormat( filePath, encoding );

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
							info.mCaption = lastPath.Substring( 0, dot );
					}
					
					if( string.IsNullOrEmpty( info.mCaption ) )
						info.mCaption = Helpers.CalcMD5( info.mUrl );
				}
			}
		}
	}
}
