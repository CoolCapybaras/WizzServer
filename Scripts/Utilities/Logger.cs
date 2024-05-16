namespace WizzServer
{
	public static class Logger
	{
		public static void LogInfo(string message)
		{
			Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}][INFO] {message}");
		}

		public static void LogError(string message)
		{
			Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}][ERROR] {message}");
		}
	}
}
