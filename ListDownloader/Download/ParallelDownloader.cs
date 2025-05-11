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
		Random mRandom = new Random();

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
				DownloadInfo info = new DownloadInfo( tmp_file_path );
				info.mUrl = link.mUrl;
				info.mNumber = mDownloaders.Count() + 1;
				info.mIsNumerate = mOptions.IsNumerateFiles;
				info.mIsMoveUrlAuthToBasicHttpAuth = mOptions.IsMoveUrlAuthToBasicHttpAuth;
				info.mIsCopyUrlAuthToBasicHttpAuth = mOptions.IsCopyUrlAuthToBasicHttpAuth;
				info.mExtraData = link;
				info.mPauseMsec = calculatePause();
				info.mHeaders = mOptions.Headers;

				mDownloaders.Add( new Downloader( info ) );
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
						else if( info.mDownloadStatus == DownloadStatus.Started || info.mDownloadStatus == DownloadStatus.Paused )
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

		private int calculatePause()
		{
			if( mOptions.MinPauseMsec.HasValue && mOptions.MaxPauseMsec.HasValue )
			{
				if( mOptions.MinPauseMsec.Value < mOptions.MaxPauseMsec )
					return mRandom.Next( mOptions.MinPauseMsec.Value, mOptions.MaxPauseMsec.Value + 1 );
				return mOptions.MinPauseMsec.Value;
			}
			else if( mOptions.MinPauseMsec.HasValue )
				return mOptions.MinPauseMsec.Value;
			else if( mOptions.MaxPauseMsec.HasValue )
				return mOptions.MaxPauseMsec.Value;

			return 0;
		}
	}
}
