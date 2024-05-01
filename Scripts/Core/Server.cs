using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using WizzServer.Services;

namespace WizzServer
{
	public class Server
	{
		public AuthTokenManager AuthTokenManager { get; } = new();
		public ConcurrentDictionary<int, Client> Clients { get; } = [];
		public ConcurrentDictionary<int, Room> Rooms { get; } = [];
		public ConcurrentDictionary<string, Quiz> Quizzes { get; } = [];

		private VkAuthService vkAuthService;
		private TelegramAuthService telegramAuthService;
		private TcpListener tcpListener;

		public async Task Start()
		{
			if (!File.Exists("profileImages/default.jpg"))
			{
				Logger.LogError("Отсутствует стандартная аватарка по пути profileImages/default.jpg");
				return;
			}

			Config.Load();

			foreach (string dir in Directory.GetDirectories("quizzes"))
			{
				string id = Path.GetFileName(dir);
				Quizzes.TryAdd(id, JsonConvert.DeserializeObject<Quiz>(File.ReadAllText(dir + "/quiz.json"))!.Init());
			}

			vkAuthService = new VkAuthService(this);
			telegramAuthService = new TelegramAuthService(this);
			_ = Task.Run(vkAuthService.Start);
			_ = Task.Run(telegramAuthService.Start);

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
			vkAuthService.Stop();
			telegramAuthService.Stop();
		}
	}
}
