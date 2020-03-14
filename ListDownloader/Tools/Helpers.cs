using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace ListDownloader
{
	static class Helpers
	{
		/// <summary>
		/// Форматированный вывод размера информации
		/// </summary>
		/// <param name="bytes">Байт</param>
		public static string FormatBytes( long bytes )
		{
			string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
			int i;
			double d_bytes = bytes;
			for( i = 0; i < suffixes.Length && bytes >= 1024; i++, bytes /= 1024 )
				d_bytes = bytes / 1024.0;
			return string.Format( "{0:0.##} {1}", d_bytes, suffixes[i] );
		}

		/// <summary>
		/// Найти расширение для файла в реестре по его mimeType
		/// </summary>
		public static string GetDefaultExtension( string mimeType )
		{
			if(string.IsNullOrEmpty( mimeType ) )
				return "";
			RegistryKey key = Registry.ClassesRoot.OpenSubKey( @"MIME\Database\Content Type\" + mimeType, false );
			if( key == null )
				return "";
			object value = key.GetValue( "Extension", null );
			if( value == null )
				return "";
			return value.ToString();
		}

		/// <summary>
		/// Вытащить расширение файла из ссылки на его скачку
		/// </summary>
		public static string GetExtFromURL( string s_url )
		{
			char[] pathSplit = new char[] { '/', '\\' };
			Uri url = new Uri( s_url );
			string[] pathParts = url.LocalPath.Split( pathSplit, StringSplitOptions.RemoveEmptyEntries );
			if( pathParts.Count() == 0 )
				return "";
			string lastPath = pathParts.Last();
			int dot = lastPath.LastIndexOf( '.' );
			if( dot < 0 || dot == lastPath.Count() - 1 )
				return "";
			return lastPath.Substring( dot );
		}

		/// <summary>
		/// Получить путь к файлу с подменённым расширением
		/// </summary>
		/// <param name="filePath">Путь к файлу</param>
		/// <param name="ext">Новое расширение</param>
		public static string ExtReplace( string filePath, string ext )
		{
			string folderPath = Path.GetDirectoryName( filePath );
			string fileName = Path.GetFileNameWithoutExtension( filePath );
			if( ext != "" && ext[0] != '.' )
				ext = "." + ext;
			fileName = fileName + ext;
			return Path.Combine( folderPath, fileName );
		}

		/// <summary>
		/// Собрать путь к файлу.
		/// Из fileCaption неприемлемые символы будут заменены на подчёркивания
		/// fileCaption обрежется, если оно слишком большое
		/// </summary>
		/// <param name="folderPath">Папка, в которой лежит файл</param>
		/// <param name="fileCaption">Некое наименование файла, претендующее на его имя</param>
		/// <param name="ext">Расширение файла</param>
		public static string GetFilePath( string folderPath, string fileCaption, string ext )
		{
			char[] invalidChars = Path.GetInvalidFileNameChars();
			char replacer = '_';

			string fileName = fileCaption;
			foreach( var ch in invalidChars )
				fileName = fileName.Replace( ch, replacer );

			int MAX_FILE_CAPTION = 100; // Это просто фиксированный предел
			// Вводим его, потому что в файлах могут быть какие угодно строки,
			// да и мы можем что-то недопарсить.
			if( fileName.Count() > MAX_FILE_CAPTION )
				fileName = fileName.Substring( 0, MAX_FILE_CAPTION );

			// Добавляем точку к ext, если надо.
			if( ext != "" && ext[0] != '.' )
				ext = "." + ext;

			string path = Path.Combine( folderPath, fileName + ext );

			int MAX_PATH = 250; // Наш предел, с небольшим запасом от виндового
			if( path.Count() > MAX_PATH )
			{
				int cut = path.Count() - MAX_PATH;
				fileName = fileName.Substring( 0, cut );
				path = Path.Combine( folderPath, fileName + ext );
			}

			return path;
		}

		/// <summary>
		/// По пути к файлу получить путь к файлу с префиксом в имени
		/// </summary>
		/// <returns></returns>
		public static string ApplyFilePrefix( string file_path, string prefix )
		{
			string folder = Path.GetDirectoryName( file_path );
			string filename = Path.GetFileName( file_path );
			filename = prefix + filename;
			return Path.Combine( folder, filename );
		}

		/// <summary>
		/// Проверка, является ли строка урлом.
		/// </summary>
		public static bool IsURLValid( string url )
		{
			Uri uriResult;
			return Uri.TryCreate( url, UriKind.Absolute, out uriResult );
		}

		/// <summary>
		/// Вычислить MD5 по строке
		/// </summary>
		public static string CalcMD5( string str )
		{
			using( var md5 = System.Security.Cryptography.MD5.Create() )
			{
				byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes( str );
				byte[] hashBytes = md5.ComputeHash( inputBytes );

				// Convert the byte array to hexadecimal string
				StringBuilder sb = new StringBuilder();
				for( int i = 0; i < hashBytes.Length; i++ )
					sb.Append( hashBytes[i].ToString( "X2" ) );
				return sb.ToString();
			}
		}
	}
}
