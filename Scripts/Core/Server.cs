using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace WizzServer
{
	public class Server
	{
		//public ConcurrentDictionary<int, AuthToken> AuthTokens { get; } = new ConcurrentDictionary<int, AuthToken>();
		public ConcurrentDictionary<int, Client> Clients { get; } = new ConcurrentDictionary<int, Client>();
		public ConcurrentDictionary<int, Room> Rooms { get; } = new ConcurrentDictionary<int, Room>();
		public ConcurrentDictionary<string, Quiz> Quizzes { get; } = new ConcurrentDictionary<string, Quiz>();

		private HttpAuthHandler httpAuthHandler = new HttpAuthHandler();
		private TcpListener tcpListener;

		public async Task Start()
		{
			foreach (string dir in Directory.GetDirectories("quizzes"))
			{
				string id = Path.GetFileName(dir);
				Quizzes.TryAdd(id, JsonConvert.DeserializeObject<Quiz>(File.ReadAllText(dir + "/quiz.json"))!.Init());
			}

			_ = Task.Run(httpAuthHandler.Start);

			tcpListener = new TcpListener(IPAddress.Any, 8887);
			tcpListener.Start();
			Logger.LogInfo("Server started on port 8887");

			while (true)
			{
				Socket socket;
				try
				{
					socket = await tcpListener.AcceptSocketAsync();
				}
				catch (SocketException)
				{
					break;
				}

				var client = new Client(this, socket, Utils.RandomGuestId());
				client.OnConnect();
				Clients.TryAdd(1, client);
				_ = Task.Run(client.Start);
			}

			Logger.LogInfo("Shutting down...");

			foreach (var client in Clients.Values)
				client.Disconnect();
		}

		public void Stop()
		{
			tcpListener.Stop();
			httpAuthHandler.Stop();
		}
	}
}
