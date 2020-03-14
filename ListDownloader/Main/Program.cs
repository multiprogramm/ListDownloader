using System;

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
				Options options = new Options( args );
				Worker worker = new Worker( options );
				worker.Run();
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
	}
}
