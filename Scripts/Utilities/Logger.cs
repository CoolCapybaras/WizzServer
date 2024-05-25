using System.Runtime.CompilerServices;
using System.Text;

namespace WizzServer
{
	public static class Logger
	{
		private static StreamWriter console = new(Console.OpenStandardOutput(), new UTF8Encoding(false), 256, true)
		{
			AutoFlush = true,
		};
		private static StreamWriter file;
		private static int logCount = 500000;

		public static void LogInfo(string message) =>
			LogMessage(message, false);

		public static void LogError(string message) =>
			LogMessage(message, true);

		[MethodImpl(MethodImplOptions.Synchronized)]
		private static void LogMessage(string message, bool isError)
		{
			if (logCount++ == 500000)
			{
				Directory.CreateDirectory("logs");
				file?.Close();
				file = File.CreateText($"logs/{DateTimeOffset.Now:dd-MM-yyyy-HH-mm-ss}.log");
				file.AutoFlush = true;
				logCount = 0;
			}

			string text = $"[{DateTimeOffset.Now:dd.MM.yyyy HH:mm:ss}][{(isError ? "ERROR" : "INFO")}] {message}";
#if DEBUG
			Console.WriteLine(text);
#else
			console.WriteLine(text);
#endif
			file.WriteLine(text);
		}
	}
}
