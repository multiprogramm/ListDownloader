﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ListDownloader
{
	/// <summary>
	/// Ошибка, связанная с чтением опций
	/// </summary>
	class OptionsError : LogicError
	{
		public OptionsError( string error ) : base( error ) { }
	}

	/// <summary>
	/// Фиктивная ошибка, которую мы бросаем,
	/// если нас открыли в режиме /help
	/// </summary>
	class PrintHelpQueryException : Exception { }

	/// <summary>
	/// Параметры работы
	/// </summary>
	class Options
	{
		// Путь к файлу со ссылками
		public string ListFilePath { get; private set; } = "";

		// Кодировка в файле ListFilePath
		public Encoding Encoding { get; private set; } = Encoding.GetEncoding( DEFAULT_ENCODING );
		static readonly private string KEY_ENCODING = "-encoding";
		static readonly private string DEFAULT_ENCODING = "utf-8";

		// Путь к папке, в которую будут качаться файлы
		public string FolderPath { get; private set; } = "";
		static readonly private string KEY_FOLDER_PATH = "-dir";

		// Сколько файлов грузим параллельно
		public int MaxParallel { get; private set; } = DEFAULT_MAX_PARALLEL;
		static readonly private string KEY_MAX_PARALLEL = "-threads";
		static readonly private int DEFAULT_MAX_PARALLEL = 2;

		// Задержка в миллисекундах между обновлением статистики скачивания в консоли
		public int UpdateInfoMsec { get; private set; } = DEFAULT_UPDATE_INFO_MSEC;
		static readonly private string KEY_UPDATE_INFO_MSEC = "-timeupd";
		static readonly private int DEFAULT_UPDATE_INFO_MSEC = 200;

		// Ставить ли номер префиксом у имени файла
		public bool IsNumerateFiles { get; private set; } = false;
		static readonly private string KEY_NUMERATE_FILES = "-num";

		// Удалять ли ссылки из файла-списка после скачки
		public bool IsDeleteDownloadedLinks { get; private set; } = false;
		static readonly private string KEY_DELETE_DOWNLOADED_LINKS = "-deletelinks";

		// Не ждать в конце ввод символа
		public bool IsReadKey { get; private set; } = true;
		static readonly private string KEY_NO_READ_KEY = "-noreadkey";

		// Перемещать URL-аутентификацию в http-хедер 
		// Authorization как Basic-аутентификацию
		// Т.е. был у нас URL:
		// https://username:password@example.com/arch.zip
		// А станет URL:
		// https://example.com/arch.zip
		// С хедером
		// Authorization=Basic dXNlcm5hbWU6cGFzc3dvcmQ=
		public bool IsMoveUrlAuthToBasicHttpAuth { get; private set; } = false;
		static readonly private string KEY_MOVE_URL_AUTH_TO_BASIC_HTTP_AUTH = "-MoveUrlAuthToBasicHttpAuth";

		// Копировать URL-аутентификацию в http-хедер
		// Authorization как Basic-аутентификацию
		// Т.е. был у нас URL:
		// https://username:password@example.com/arch.zip
		// И он останется тем же самым:
		// https://username:password@example.com/arch.zip
		// Но дополнительно к запросу добавится хедер
		// Authorization=Basic dXNlcm5hbWU6cGFzc3dvcmQ=
		public bool IsCopyUrlAuthToBasicHttpAuth { get; private set; } = false;
		static readonly private string KEY_COPY_URL_AUTH_TO_BASIC_HTTP_AUTH = "-CopyUrlAuthToBasicHttpAuth";

		// Минимальная пауза после скачивания
		public int? MinPauseMsec { get; private set; } = DEFAULT_MIN_PAUSE_MSEC;
		static readonly private string KEY_MIN_PAUSE_MSEC = "-minPause";
		static readonly private int? DEFAULT_MIN_PAUSE_MSEC = null;

		// Максимальная пауза после скачивания
		public int? MaxPauseMsec { get; private set; } = DEFAULT_MAX_PAUSE_MSEC;
		static readonly private string KEY_MAX_PAUSE_MSEC = "-maxPause";
		static readonly private int? DEFAULT_MAX_PAUSE_MSEC = null;

		// Путь к файлу с заголовками, utf-8
		public string HeadersFilePath { get; private set; } = DEFAULT_HEADERS_FILE_PATH;
		static readonly private string KEY_HEADERS_FILE_PATH = "-headersFile";
		static readonly private string DEFAULT_HEADERS_FILE_PATH = "";

		public Dictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>();

		static readonly private string[] KEYS_HELP = new string[] { "-help", "/help", "/?" };

		/// <summary>
		/// Конструктор по параметрам из командной строки
		/// </summary>
		public Options( string[] args )
		{
			CheckIsHelp( args );

			if( args.Count() == 0 )
				throw new OptionsError( "Need list file path." );

			ListFilePath = args.Last();
			if( !File.Exists( ListFilePath ) )
				throw new OptionsError( "List file is not exists." );

			int args_count = args.Count() - 1;
			for( int i = 0; i < args_count; )
				ExtractArg( args, ref i );

			if( FolderPath == "")
			{
				FolderPath = Path.Combine(
					Path.GetDirectoryName( ListFilePath ),
					Path.GetFileNameWithoutExtension( ListFilePath )
				);
				if( !Directory.Exists( FolderPath ) )
					Directory.CreateDirectory( FolderPath );
			}

			if( MinPauseMsec.HasValue && MaxPauseMsec.HasValue )
			{
				if( MinPauseMsec.Value > MaxPauseMsec.Value )
				{
					int temp = MinPauseMsec.Value;
					MinPauseMsec = MaxPauseMsec;
					MaxPauseMsec = temp;
				}
			}

			if( HeadersFilePath != "" )
			{
				if( !File.Exists( HeadersFilePath ) )
					throw new OptionsError( "List file is not exists." );
				FillHeaders();
			}
		}

		/// <summary>
		/// Напечатать справку по параметрам в консоль
		/// </summary>
		public static void PrintHelp()
		{
			string exe_name = System.AppDomain.CurrentDomain.FriendlyName;
			Console.WriteLine();
			Console.WriteLine( exe_name + " [Attributes] PathToLinksFile" );
			Console.WriteLine();
			Console.WriteLine( "PathToLinksFile" );
			Console.WriteLine( "  Файл, из которого нужно извлечь ссылки для скачки и соответствующие имена файлов." );
			Console.WriteLine();

			Console.WriteLine( "[Attributes]:" );
			Console.WriteLine( "  " + KEY_MAX_PARALLEL + " NUM" );
			Console.WriteLine( "    Количество качаемых файлов одновременно." );
			Console.WriteLine( "    По умолчанию " + DEFAULT_MAX_PARALLEL.ToString() + "." );

			Console.WriteLine( "  " + KEY_NUMERATE_FILES );
			Console.WriteLine( "    У результирующих файлов в имени будет добавлен префикс-номер." );
			Console.WriteLine( "    По умолчанию выключено." );

			Console.WriteLine( "  " + KEY_ENCODING + " ENCODING" );
			Console.WriteLine( "    Кодировка, в которой читаем файл-список." );
			Console.WriteLine( "    По умолчанию " + DEFAULT_ENCODING + "." );

			Console.WriteLine( "  " + KEY_UPDATE_INFO_MSEC + " NUM" );
			Console.WriteLine( "    Пауза в миллисекундах между обновлением консоли." );
			Console.WriteLine( "    По умолчанию " + DEFAULT_UPDATE_INFO_MSEC.ToString() + "." );

			Console.WriteLine( "  " + KEY_FOLDER_PATH + " PATH_TO_DIR" );
			Console.WriteLine( "    Папка, в которую будут сохраняться файлы." );
			Console.WriteLine( "    По умолчанию - папка рядом с файлом со ссылками, с таким же именем." );

			Console.WriteLine( "  " + KEY_DELETE_DOWNLOADED_LINKS );
			Console.WriteLine( "    Удалять из списка файлов линки и имена файлов после их скачки." );
			Console.WriteLine( "    По умолчанию выключено." );

			Console.WriteLine( "  " + KEY_NO_READ_KEY );
			Console.WriteLine( "    Не ждать в конце ввод символа." );
			Console.WriteLine( "    По умолчанию ждётся." );

			Console.WriteLine( "  " + KEY_MOVE_URL_AUTH_TO_BASIC_HTTP_AUTH );
			Console.WriteLine( "    Перемещать URL-аутентификацию в http-хедер Authorization как Basic-аутентификацию." );
			Console.WriteLine( "        Т.е. был у нас URL:" );
			Console.WriteLine( "        https://username:password@example.com/arch.zip" );
			Console.WriteLine( "        А станет URL:" );
			Console.WriteLine( "        https://example.com/arch.zip" );
			Console.WriteLine( "        С хедером:" );
			Console.WriteLine( "        Authorization=Basic dXNlcm5hbWU6cGFzc3dvcmQ=" );
			Console.WriteLine( "    По умолчанию выключено." );
			Console.WriteLine( "    Использовать URL-аутентификацию в нешифрованных протоколах (http без s, например) опасно! Ваши логин и пароль передаются простым текстом!" );

			Console.WriteLine( "  " + KEY_COPY_URL_AUTH_TO_BASIC_HTTP_AUTH );
			Console.WriteLine( "    Копировать URL-аутентификацию в http-хедер Authorization как Basic-аутентификацию." );
			Console.WriteLine( "        Т.е. был у нас URL:" );
			Console.WriteLine( "        https://username:password@example.com/arch.zip" );
			Console.WriteLine( "        И он останется как есть:" );
			Console.WriteLine( "        https://username:password@example.com/arch.zip" );
			Console.WriteLine( "        Но добавится хедер:" );
			Console.WriteLine( "        Authorization=Basic dXNlcm5hbWU6cGFzc3dvcmQ=" );
			Console.WriteLine( "    По умолчанию выключено." );
			Console.WriteLine( "    Использовать URL-аутентификацию в нешифрованных протоколах (http без s, например) опасно! Ваши логин и пароль передаются простым текстом!" );

			Console.WriteLine( "  " + KEY_MIN_PAUSE_MSEC );
			Console.WriteLine( "    Минимальная пауза в миллисекундах после скачки файла у потока." );
			Console.WriteLine( "    По умолчанию нет." );

			Console.WriteLine( "  " + KEY_MAX_PAUSE_MSEC );
			Console.WriteLine( "    Максимальная пауза в миллисекундах после скачки файла у потока." );
			Console.WriteLine( "    По умолчанию нет." );

			Console.WriteLine( "  " + KEY_HEADERS_FILE_PATH );
			Console.WriteLine( "    Путь к файлу заголовков, которые будут подставляться в запрос." );
			Console.WriteLine( "    По умолчанию нет." );

			Console.WriteLine( "  " + KEYS_HELP[0] );
			Console.WriteLine( "    Справка по параметрам." );
		}


		// Private:

		/// <summary>
		/// Проверить, не позвали ли нас только для справки.
		/// Если да, то кидает PrintHelpQueryException
		/// </summary>
		void CheckIsHelp( string[] args )
		{
			if( args.Count() <= 0 )
				return;
			if( KEYS_HELP.Contains( args[0].ToLowerInvariant() ) )
				throw new PrintHelpQueryException();
		}

		/// <summary>
		/// Вытащить следующий параметр и его значение
		/// </summary>
		void ExtractArg( string[] args, ref int i )
		{
			string s_key = args[i++];
			if( s_key == KEY_MAX_PARALLEL )
			{
				string s_value = args[i++];
				MaxParallel = ExtractPositiveInt( s_key, s_value );
			}
			else if( s_key == KEY_NUMERATE_FILES )
			{
				IsNumerateFiles = true;
			}
			else if( s_key == KEY_ENCODING )
			{
				string s_value = args[i++];
				Encoding = Encoding.GetEncoding( s_value );
			}
			else if( s_key == KEY_FOLDER_PATH )
			{
				FolderPath = args[i++];
			}
			else if( s_key == KEY_DELETE_DOWNLOADED_LINKS )
			{
				IsDeleteDownloadedLinks = true;
			}
			else if( s_key == KEY_NO_READ_KEY )
			{
				IsReadKey = false;
			}
			else if( s_key == KEY_MOVE_URL_AUTH_TO_BASIC_HTTP_AUTH )
			{
				IsMoveUrlAuthToBasicHttpAuth = true;
			}
			else if( s_key == KEY_COPY_URL_AUTH_TO_BASIC_HTTP_AUTH )
			{
				IsCopyUrlAuthToBasicHttpAuth = true;
			}
			else if( s_key == KEY_MIN_PAUSE_MSEC )
			{
				string s_value = args[i++];
				MinPauseMsec = ExtractPositiveInt( s_key, s_value );
			}
			else if( s_key == KEY_MAX_PAUSE_MSEC )
			{
				string s_value = args[i++];
				MaxPauseMsec = ExtractPositiveInt( s_key, s_value );
			}
			else if( s_key == KEY_HEADERS_FILE_PATH )
			{
				HeadersFilePath = args[i++];
			}
			else
			{
				throw new OptionsError( "Unknown key '" + s_key + "'" );
			}
		}

		/// <summary>
		/// Извлечь из s_value целое число больше нуля
		/// </summary>
		int ExtractPositiveInt( string s_key, string s_value )
		{
			int result = 0;
			if( !int.TryParse( s_value, out result ) )
				throw new OptionsError( "Value of key " + s_key + " '" + s_value.ToString() + "' is not a number" );
			if( result <= 0 )
				throw new OptionsError( "Value of key " + s_key + " '" + s_value.ToString() + "' must be > 0." );
			return result;
		}

		void FillHeaders()
		{
			var lines = File.ReadAllLines( HeadersFilePath );
			foreach( var line in lines )
			{
				if( line == "" )
					continue;
				int eq_idx = line.IndexOf( '=' );
				if( eq_idx <= 0 )
					continue;
				string param = line.Substring( 0, eq_idx );
				string value = line.Substring( eq_idx + 1 );
				Headers[param] = value;
			}
		}
	}
}
