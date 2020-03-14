using System;
using System.Collections.Generic;
using System.Linq;

namespace ListDownloader
{
	/// <summary>
	/// Основной работащий класс программы
	/// </summary>
	class Worker
	{
		// Опции работы
		Options mOptions;

		public Worker( Options options )
		{
			if( options == null )
				throw new NullReferenceException( "options" );
			mOptions = options;
		}

		/// <summary>
		/// Запуск основной работы
		/// </summary>
		public void Run()
		{
			// Получаем кучу линков и валидируем их
			ILinksExtractor links_extractor = LinksTools.CreateExtractor(
				mOptions.ListFilePath, mOptions.Encoding );
			List<LinkInfo> links = links_extractor.ExtractLinks();
			LinksTools.DeleteEmptyLinks( links );
			LinksTools.FillEmptyCaptions( links );
			if( links.Count() == 0 )
				throw new LogicError( "Links is not found." );

			// Настраиваем закачиватель и закачиваем им линки
			ParallelDownloader downloader = new ParallelDownloader(
				mOptions.FolderPath,
				mOptions.MaxParallel,
				mOptions.UpdateInfoMsec,
				mOptions.IsNumerateFiles
			);
			downloader.Add( links );
			downloader.Run();
		}
	}
}
