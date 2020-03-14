using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ListDownloader
{
	/// <summary>
	/// Раскрашенная строка
	/// </summary>
	class PaintedConsoleString
	{
		static readonly ConsoleColor DefaultBgColor = Console.BackgroundColor;

		// Строка
		public StringBuilder mStrBuilder { get; private set; } = new StringBuilder();

		// Позиция -> Смена цвета
		public Dictionary<int, ConsoleColor> mBgColors { get; private set; } = new Dictionary<int, ConsoleColor>();

		public PaintedConsoleString()
		{
			mBgColors[0] = DefaultBgColor;
		}

		// Изменяем цвет в позиции
		public void SetColor( int pos, ConsoleColor color )
		{
			mBgColors[pos] = color;
		}

		// Сбрасываем цвет в позиции в дефолт
		public void SetDefaultColor( int pos )
		{
			mBgColors[pos] = DefaultBgColor;
		}

		// Изменяем цвет в текущей позиции
		public int SetColor( ConsoleColor color )
		{
			int pos = mStrBuilder.Length;
			mBgColors[pos] = color;
			return pos;
		}

		// Сбрасываем цвет в текущей позиции в дефолт
		public int SetDefaultColor()
		{
			int pos = mStrBuilder.Length;
			mBgColors[pos] = DefaultBgColor;
			return pos;
		}

		// Дописать строчку
		public void Append( string str )
		{
			mStrBuilder.Append( str );
		}

		// Напечатать разукрашенную строку в текущую позицию консоли
		public void Print()
		{
			string str = mStrBuilder.ToString();
			str = str.Substring( 0, Math.Min( Console.BufferWidth - 1, str.Count() ) );

			Console.BackgroundColor = DefaultBgColor;
			int prev = 0;
			bool is_first = true;
			foreach( var p in mBgColors )
			{
				if( is_first )
				{
					Console.BackgroundColor = p.Value;
					prev = p.Key;
					is_first = false;
					continue;
				}

				int cur = p.Key;
				Console.Write( ExtractStr( str, prev, cur ) );
				Console.BackgroundColor = p.Value;
				prev = cur;
			}

			Console.Write( ExtractStr( str, prev ) );
			Console.BackgroundColor = DefaultBgColor;
		}

		// Private:

		string ExtractStr( string str, int prev, int cur )
		{
			prev = Math.Min( str.Count(), prev );
			cur = Math.Min( str.Count(), cur );
			if( prev >= cur )
				return "";
			int lenght = cur - prev;
			return str.Substring( prev, lenght );
		}

		string ExtractStr( string str, int cur )
		{
			return str.Substring( cur );
		}
	}
}
