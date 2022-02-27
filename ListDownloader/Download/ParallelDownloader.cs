using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ListDownloader
{
	/// <summary>
	/// Скачиватель нескольких файлов одновременно с отображением статистики
	/// </summary>
	class ParallelDownloader
	{
		// Синхронные качатели, будут удаляться при завершении закачки
		List<Downloader> mDownloaders = new List<Downloader>();

		Options mOptions;

		// Событие, вызываемое после успешной закачки файла
		public event Action<LinkInfo> OnSuccessDownload;

		public ParallelDownloader( Options options )
		{
			if( options == null )
				throw new NullReferenceException( "options" );
			mOptions = options;
		}

		/// <summary>
		/// Добавить список линков
		/// </summary>
		public void Add( List<LinkInfo> links )
		{
			foreach( var link in links )
			{
				string tmp_file_path = Helpers.GetFilePath( mOptions.FolderPath, link.mCaption, ".tmp" );
				mDownloaders.Add( new Downloader( link.mUrl, tmp_file_path, mDownloaders.Count() + 1, mOptions.IsNumerateFiles, link ) );
			}
		}

		/// <summary>
		/// Запустить закачки
		/// </summary>
		public void Run()
		{
			int count = mDownloaders.Count();
			int ok_count = 0;
			int error_count = 0;
			Console.WriteLine( "Links found: {0}", count );
			using( DownloaderView view = new DownloaderView() )
			{
				int count_left = mDownloaders.Count();
				view.SetMaxNumber( count_left );
				int count_current = 0;

				while( count_left > 0 )
				{
					for( int i = 0; i < mDownloaders.Count(); ++i )
					{
						Downloader downloader = mDownloaders[i];
						DownloadInfo info = downloader.GetInfo();
						if( !info.IsStarted() && count_current < mOptions.MaxParallel )
						{
							downloader.DownloadAsync();
							++count_current;
						}
						else if( info.IsFinished() )
						{
							if( info.mError == "" )
							{
								try
								{
									OnSuccessDownload?.Invoke( info.mExtraData as LinkInfo );
								}
								catch( Exception ex )
								{
									info.mError = ex.Message;
								}
							}


							view.UpdateInfo( info );

							if( info.mError == "" )
								++ok_count;
							else
								++error_count;

							--count_left;
							--count_current;
							mDownloaders.RemoveAt( i );
							--i;

							continue;
						}
						else if( info.mDownloadStatus == DownloadStatus.Started )
						{
							view.UpdateInfo( info );
						}
					}

					Thread.Sleep( mOptions.UpdateInfoMsec );
				}
			}

			Console.WriteLine( "Downloaded: {0}, Error: {1}",
				ok_count, error_count );
		}
	}
}
