using System;
using System.Linq;
using System.Text;

namespace ListDownloader
{
	/// <summary>
	/// Класс для отображения прогресса в консоли
	/// </summary>
	class DownloaderView : IDisposable
	{
		// Формат для вывода номера
		string mNumberFormat = "0";
		
		// Максимальный номер, который будем выводить
		int mMaxNumber = 0;

		// Чтоб выводить инфу позиционно
		ConsoleWrapper mConsole;

		public DownloaderView()
		{
			mConsole = new ConsoleWrapper();
		}

		public void Dispose()
		{
			if( mConsole != null )
			{
				mConsole.Dispose();
				mConsole = null;
			}
		}

		/// <summary>
		/// Установить максимальный номер строки, которую будем выводить
		/// </summary>
		public void SetMaxNumber( int number )
		{
			mMaxNumber = number;
			if( number <= 0 )
				mNumberFormat = "0";
			else
				mNumberFormat = new string( '0', (int)Math.Log10( number ) + 1 );
		}

		/// <summary>
		/// Обновить информацию о конкретной закачке
		/// </summary>
		public void UpdateInfo( DownloadInfo info )
		{
			if( info.mDownloadStatus == DownloadStatus.NotStarted )
				return; // Не выводим инфу о неначатых
			int progressStringSize = ( 10 * 2 + 3 + 2 );


			var paintedString = new PaintedConsoleString();
			paintedString.Append( info.mNumber.ToString( mNumberFormat ) );
			paintedString.Append( "/" );
			paintedString.Append( mMaxNumber.ToString( mNumberFormat ) );
			paintedString.Append( " " );

			int count_progress_symbols = 0;
			string progress_string = "";
			ConsoleColor colorForProgress = Console.BackgroundColor;
			if( info.mDownloadStatus == DownloadStatus.Started )
			{
				StringBuilder sb_prog = new StringBuilder();
				if( info.mBytes > 0 )
				{
					string left = Helpers.FormatBytes( info.mDownloadedBytes );
					string center = " ";
					string right = Helpers.FormatBytes( info.mBytes );

					progress_string = ProgressStringAligner( left, center, right, progressStringSize );
					count_progress_symbols = calcProgressSymbols(
						info.mDownloadedBytes, info.mBytes, progress_string.Count() );
				}
				else
				{
					progress_string = ProgressStringAligner( " ... ", progressStringSize );
					count_progress_symbols = calcProgressSymbols( 0, 1, progress_string.Count() );
				}

				colorForProgress = ConsoleColor.DarkGray;
			}
			else if( info.mDownloadStatus == DownloadStatus.Paused )
			{
				string left = info.mPauseMsec.ToString() + "ms";
				string center = " ";
				string right = "";

				if( info.mError != "" )
				{
					right = getErrorString( info );
					colorForProgress = ConsoleColor.DarkRed;
				}
				else
				{
					right = Helpers.FormatBytes( info.mBytes );
					colorForProgress = ConsoleColor.DarkGreen;
				}

				progress_string = ProgressStringAligner( left, center, right, progressStringSize );
				count_progress_symbols = calcProgressSymbols( 1, 1, progress_string.Count() );
			}
			else if( info.mDownloadStatus == DownloadStatus.Finished )
			{
				if( info.mError != "" )
				{
					string center = getErrorString( info );
					colorForProgress = ConsoleColor.DarkRed;
					progress_string = ProgressStringAligner( center, progressStringSize );
				}
				else
				{
					string str_in_prog = Helpers.FormatBytes( info.mBytes );
					colorForProgress = ConsoleColor.DarkGreen;
					progress_string = ProgressStringAligner( "", "", str_in_prog, progressStringSize );
				}

				count_progress_symbols = calcProgressSymbols( 1, 1, progress_string.Count() );
			}

			int start_progress = paintedString.SetColor( colorForProgress );
			paintedString.Append( progress_string );
			paintedString.SetDefaultColor( start_progress + count_progress_symbols );

			paintedString.Append( " " );
			paintedString.Append( info.GetFileCaption() );

			mConsole.WriteLine( info.mNumber, paintedString );
		}

		// Private:

		string ProgressStringAligner(
			string str,
			int strLength )
		{
			if( str.Count() > strLength )
				throw new Exception( "1: str > strLength" );
			
			int leftSpan = ( strLength - str.Count() ) / 2;
			int rightSpan = strLength - str.Count() - leftSpan;
			string result = "[" + new string( ' ', leftSpan - 1 ) + str + new string( ' ', rightSpan - 1 ) + "]";
			if( result.Count() != strLength )
				throw new Exception( "1: wrong result" );
			return result;
		}

		string ProgressStringAligner(
			string left,
			string center,
			string right,
			int strLength )
		{
			int sum = left.Count() + right.Count() + center.Count();
			if( sum > strLength )
				throw new Exception( "3: sum > strLength" );

			int leftSpan = ( strLength - sum ) / 2;
			int rightSpan = strLength - sum - leftSpan;

			string result = "[ "
				+ left
				+ new string( ' ', leftSpan - 2 )
				+ center
				+ new string( ' ', rightSpan - 2 )
				+ right
				+ " ]";

			if( result.Count() != strLength )
				throw new Exception( "3: wrong result" );

			return result;
		}

		int calcProgressSymbols(
			long currentProgress,
			long maxProgress,
			int norm )
		{
			int count_progress_symbols = 0;
			if( maxProgress != 0 )
				count_progress_symbols = (int)( (double)currentProgress / (double)maxProgress * (double)norm );
			return count_progress_symbols;
		}

		string getErrorString( DownloadInfo info )
		{
			if( info.mError != "" )
			{
				if( info.mHttpErrorCode != 0 )
					return info.mHttpErrorCode.ToString();
				return "ERROR";
			}

			return "";
		}
	}
}
