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

		// Папка, куда сохраняем файлы
		string mFolderPath;

		// Сколько файлов качаем одновременно
		int mMaxParallel;

		// Слип между обновлениями статистики
		int mSleepMsec;

		// Нумеровать ли файлы
		bool mIsNumerateFiles;

		public ParallelDownloader( string folder_path, int max_parallel, int sleep_msec, bool is_numerate )
		{
			mFolderPath = folder_path;
			mMaxParallel = max_parallel;
			mSleepMsec = sleep_msec;
			mIsNumerateFiles = is_numerate;
		}

		/// <summary>
		/// Добавить список линков
		/// </summary>
		public void Add( List<LinkInfo> links )
		{
			foreach( var link in links )
				Add( link );
		}

		/// <summary>
		/// Добавить линк
		/// </summary>
		public void Add( LinkInfo link )
		{
			string tmp_file_path = Helpers.GetFilePath( mFolderPath, link.mCaption, ".tmp" );
			mDownloaders.Add( new Downloader( link.mUrl, tmp_file_path, mDownloaders.Count() + 1, mIsNumerateFiles ) );
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
						if( !info.IsStarted() && count_current < mMaxParallel )
						{
							downloader.DownloadAsync();
							++count_current;
						}
						else if( info.IsFinished() )
						{
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

					Thread.Sleep( mSleepMsec );
				}
			}

			Console.WriteLine( "Downloaded: {0}, Error: {1}",
				ok_count, error_count );
		}
	}
}
