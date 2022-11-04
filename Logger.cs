using System;

namespace SeleniumBot
{
	public static class Logger
	{
		public static void Log(string message)
		{
			Console.WriteLine(message + " at " + DateTime.Now.ToShortTimeString());
		}
	}
}
