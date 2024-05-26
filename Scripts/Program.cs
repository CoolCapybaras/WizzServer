namespace WizzServer
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var server = new Server();
			var task = Task.Run(server.Start);

			while (Console.ReadLine() != "stop") { }

			server.Stop();
			task.Wait();
		}
	}
}
