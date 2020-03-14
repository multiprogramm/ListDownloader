using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ListDownloader
{
	/// <summary>
	/// Обёртка над консолью для позиционированного вывода
	/// </summary>
	class ConsoleWrapper : IDisposable
	{
		// Размер консоли
		int mMaxXAbs;
		int mMaxYAbs;

		// Смещение относительно ненаших строк сверху
		int mSpan;

		// Максимальный номер, который мы можем напечатать сейчас
		int mMax;
		
		// Следующий свободный номер строки
		int mNextLine;

		// id -> line num
		Dictionary<int, int> mConsoleLine = new Dictionary<int, int>();

		public ConsoleWrapper()
		{
			mMaxXAbs = Console.BufferWidth - 1;
			mMaxYAbs = Console.BufferHeight - 1;

			mSpan = Console.CursorTop;
			mMax = mMaxYAbs;
			mNextLine = 0;
		}

		public void Dispose()
		{
			SetCursorPosNextLine();
		}

		/// <summary>
		/// Напечатать строку с позиционированием по идентификатору
		/// </summary>
		public void WriteLine( int id, string str )
		{
			if( !SetCursorPos( LineByID( id ) ) )
				return;
			Console.Write( str );
		}

		/// <summary>
		/// Напечатать цветную строку с позиционированием по идентификатору
		/// </summary>
		public void WriteLine( int id, PaintedConsoleString str )
		{
			if( !SetCursorPos( LineByID( id ) ) )
				return;
			str.Print();
		}

		// Private:

		/// <summary>
		/// Получить номер строки по идентификатору
		/// </summary>
		int LineByID( int id )
		{
			int line;
			if( !mConsoleLine.TryGetValue( id, out line ) )
			{
				line = mNextLine;
				mConsoleLine.Add( id, line );
				++mNextLine;
			}
			return line;
		}

		/// <summary>
		/// Установить курсор на строку с номером
		/// </summary>
		/// <returns>Получилось ли. Если false - то строка уже за пределами буфера</returns>
		bool SetCursorPos( int line )
		{
			if( ( line + mSpan ) < ( mMax - mMaxYAbs ) )
				return false;

			while( ( line + mSpan ) > mMax )
			{
				Console.SetCursorPosition( mMaxXAbs, mMaxYAbs );
				Console.WriteLine();

				if( mSpan > 0 )
					--mSpan; // Происходит стирание верхних ненаших строк
				else
				{
					// Теперь все строки наши, поэтому мы двигаем рамку
					++mMax;
				}
			}

			Console.SetCursorPosition( 0, line - ( mMax - mMaxYAbs ) + mSpan );
			return true;
		}

		/// <summary>
		/// На следующую строку
		/// </summary>
		void SetCursorPosNextLine()
		{
			int line = mNextLine;
			++mNextLine;
			SetCursorPos( line );
		}
	}
}
