using System;
using System.IO;
using System.Net;

namespace ListDownloader
{
	/// <summary>
	/// Закачиватель
	/// </summary>
	class Downloader
	{
		// Информация о закачке
		DownloadInfo mInfo;

		// Размер буфера, порция, которыми качаем
		public int mBufferSize { get; private set; } = 16 * 1024;

		public Downloader(string url, string filepath, int num, bool is_numerate_files )
		{
			mInfo = new DownloadInfo( filepath );
			mInfo.mDownloadStatus = DownloadStatus.NotStarted;
			mInfo.mUrl = url;
			mInfo.mNumber = num;
			mInfo.mIsNumerate = is_numerate_files;
		}

		/// <summary>
		/// Запустить закачку асинхронно
		/// </summary>
		public void DownloadAsync()
		{
			mInfo.mDownloadStatus = DownloadStatus.Started;
			Action action = Download;
			action.BeginInvoke( null, null );
		}

		/// <summary>
		/// Получить текущую информацию о закачке.
		/// Нужно учитывать, что закачка в этот момент идёт параллельно,
		/// и можно получить неконсистентное состояние.
		/// </summary>
		public DownloadInfo GetInfo()
		{
			return mInfo;
		}

		// Private:

		/// <summary>
		/// Стрим по файлу для скачки на диске, в который можно писать
		/// </summary>
		/// <param name="isCanSeek">Умеем ли мы докачивать</param>
		FileStream GetFileStream( bool isCanSeek )
		{
			string file_path = mInfo.GetFilePath();
			if( File.Exists( file_path ) )
			{
				if( isCanSeek )
				{
					mInfo.mDownloadedBytes = new FileInfo( file_path ).Length;
					return File.Open( file_path, FileMode.Append, FileAccess.Write );
				}
				else
				{
					// Увы, докачка не поддерживается, давай по новой
					File.Delete( file_path );
				}
			}

			return File.Create( file_path, mBufferSize, FileOptions.SequentialScan );
		}

		/// <summary>
		/// Синхронная скачка
		/// </summary>
		void Download()
		{
			try
			{
				DownloadSafe();
			}
			catch( WebException ex )
			{
				if( ex.Status == WebExceptionStatus.ProtocolError )
				{
					var response = ex.Response as HttpWebResponse;
					if( response != null )
						mInfo.mHttpErrorCode = (int)response.StatusCode;
				}

				mInfo.mError = ex.ToString();
			}
			catch( Exception ex )
			{
				mInfo.mError = ex.ToString();
			}

			mInfo.mBytes = mInfo.mDownloadedBytes;
			mInfo.mDownloadStatus = DownloadStatus.Finished;
		}

		/// <summary>
		/// Суть синхронной скачки
		/// </summary>
		void DownloadSafe()
		{
			string url = new Uri( mInfo.mUrl ).AbsoluteUri;
			WebRequest request = WebRequest.Create( url );
			using( WebResponse response = request.GetResponse() )
			{
				string filePathAfterLoad = ResultPathCalc( response );
				if( File.Exists( filePathAfterLoad ) )
				{
					mInfo.mDownloadedBytes = new FileInfo( filePathAfterLoad ).Length;
					return;
				}

				using( var responseStream = response.GetResponseStream() )
				{
					mInfo.mBytes = response.ContentLength;
					bool isCanSeek = responseStream.CanSeek;

					using( var outputFileStream = GetFileStream( isCanSeek ) )
					{
						if( isCanSeek )
							responseStream.Seek( mInfo.mDownloadedBytes, SeekOrigin.Begin );

						var buffer = new byte[mBufferSize];
						while( mInfo.mDownloadedBytes < mInfo.mBytes )
						{
							int bytesRead = responseStream.Read( buffer, 0, mBufferSize );
							outputFileStream.Write( buffer, 0, bytesRead );
							mInfo.mDownloadedBytes += bytesRead;
						}
					}
				}

				File.Move( mInfo.GetFilePath(), filePathAfterLoad );
			}
		}

		/// <summary>
		/// Вычислить путь, в который мы переместим .tmp файл после скачки.
		/// По сути сейчас это вычисление расширения, на которое мы поменяем.
		/// </summary>
		string ResultPathCalc( WebResponse response )
		{
			string ext = Helpers.GetExtFromURL( mInfo.mUrl );
			if( string.IsNullOrEmpty( ext ) )
			{
				string mimeType = response.ContentType;
				ext = Helpers.GetDefaultExtension( mimeType );
			}
			return Helpers.ExtReplace( mInfo.GetFilePath(), ext );
		}
	}
}
