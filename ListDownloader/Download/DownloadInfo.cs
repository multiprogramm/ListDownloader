﻿using System.Collections.Generic;
using System.IO;

namespace ListDownloader
{
	/// <summary>
	/// Статус закачки
	/// </summary>
	enum DownloadStatus
	{
		NotStarted, // Не начиналась
		Started, // Запущена
		Paused, // Пауза
		Finished // Завершена (успешно или с ошибкой)
	}

	/// <summary>
	/// Инфа о закачке
	/// </summary>
	class DownloadInfo
	{
		// Закачиваемый URL-адрес
		public string mUrl { get; set; }

		// Какой размер скачиваемого файла
		public long mBytes { get; set; } = 0;

		// Сколько байт скачано
		public long mDownloadedBytes { get; set; } = 0;

		// Статус закачки
		public DownloadStatus mDownloadStatus { get; set; } = DownloadStatus.NotStarted;

		// Ошибка, с которой завершилась закачка
		public string mError { get; set; } = "";

		// Если произошла ошибка и она уровня протокола,
		// то сюда запишется код этой ошибки
		public int mHttpErrorCode { get; set; } = 0;

		// Номер закачки
		public int mNumber { get; set; } = 0;

		// Нужно ли добавить номер закачки в имя файла
		public bool mIsNumerate { get; set; } = false;

		// Любые дополнительные данные, связанные с закачкой
		public object mExtraData { get; set; } = null;

		// Пауза после завершения запроса
		public int mPauseMsec { get; set; } = 0;

		// Заголовки для запроса
		public Dictionary<string, string> mHeaders { get; set; } = null;

		// Путь к файлу (без номера), получается через GetFilePath()
		// а вот там уже добавится номер, если нужно
		string mFilePath { get; set; }

		// Копирование/перемещение аутентификации из URL в
		// хедер, см. в Options
		public bool mIsMoveUrlAuthToBasicHttpAuth { get; set; } = false;
		public bool mIsCopyUrlAuthToBasicHttpAuth { get; set; } = false;

		public DownloadInfo( string file_path )
		{
			mFilePath = file_path;
		}

		/// <summary>
		/// Была ли закачка запущена
		/// </summary>
		public bool IsStarted()
		{
			return mDownloadStatus != DownloadStatus.NotStarted;
		}

		/// <summary>
		/// Была ли закачка завершена
		/// </summary>
		public bool IsFinished()
		{
			return mDownloadStatus == DownloadStatus.Finished;
		}

		/// <summary>
		/// Имя файла без номера и расширения
		/// </summary>
		public string GetFileCaption()
		{
			return Path.GetFileNameWithoutExtension( mFilePath );
		}

		/// <summary>
		/// Путь к файлу (с номером, если нужно)
		/// </summary>
		public string GetFilePath()
		{
			if( mIsNumerate )
				return Helpers.ApplyFilePrefix( mFilePath, mNumber.ToString() + ". " );
			else
				return mFilePath;
		}
	}
}
