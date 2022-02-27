using System;
using System.Collections.Generic;
using System.Net;

namespace ListDownloader
{
	/// <summary>
	/// "Нормальная" ошибка, которую мы корректно покажем пользователю
	/// </summary>
	class LogicError : Exception
	{
		public LogicError( string error ) : base( error ) { }
	}

	class Program
	{
		static int Main( string[] args )
		{
			int result = 0;
			try
			{
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3
					| SecurityProtocolType.Tls
					| SecurityProtocolType.Tls11
					| SecurityProtocolType.Tls12;

				// Заполняем опции
				Options options = new Options( args );

				// Получаем кучу линков и валидируем их
				IListLinksFormat list_format = LinksTools.CreateListLinksFormat( options.ListFilePath, options.Encoding );
				List<LinkInfo> links = list_format.ExtractLinks();
				LinksTools.DeleteEmptyLinks( links );
				LinksTools.FillEmptyCaptions( links );
				if( links.Count == 0 )
					throw new LogicError( "Links is not found." );

				// Настраиваем закачиватель и закачиваем им линки
				ParallelDownloader downloader = new ParallelDownloader( options );
				downloader.Add( links );
				if( options.IsDeleteDownloadedLinks )
				{
					// После скачки файла будет запускаться удалялка
					// линка из файла-списка
					downloader.OnSuccessDownload += ( LinkInfo link_info ) => {
						list_format.DeleteLink( link_info );
					};
				}

				downloader.Run();

				Console.ReadKey();
			}
			catch( LogicError error )
			{
				Console.WriteLine();
				Console.WriteLine();
				Console.WriteLine( error.ToString() );
				result = 1;
			}
			catch( PrintHelpQueryException )
			{
				Options.PrintHelp();
				result = 0;
			}

			return result;
		}

		private static void Downloader_OnSuccessDownload( LinkInfo obj )
		{
			throw new NotImplementedException();
		}
	}
}
