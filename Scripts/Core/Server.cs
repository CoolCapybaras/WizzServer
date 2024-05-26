using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using WizzServer.Managers;
using WizzServer.Services;
using WizzServer.Utilities.Collections;

namespace WizzServer
{
	public class Server
	{
		public AuthTokenManager AuthTokenManager { get; } = new();
		public ConcurrentHashSet<Client> Clients { get; } = [];
		public ConcurrentDictionary<int, Room> Rooms { get; } = [];
		public QuizManager QuizManager { get; } = new();
		public VkAuthService VkAuthService { get; set; }
		public TelegramBotService TelegramBotService { get; set; }

		private TcpListener tcpListener;
		private Task[] serverTasks;

		public async Task Start()
		{
			if (!Setup())
				return;

			VkAuthService = new VkAuthService(this);
			TelegramBotService = new TelegramBotService(this);
			serverTasks =
			[
				Task.Run(VkAuthService.Start),
				Task.Run(TelegramBotService.Start)
			];

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

				var client = new Client(this, socket);
				Clients.Add(client);
				_ = Task.Run(client.Start);
			}

			Logger.LogInfo("Shutting down server...");

			foreach (var client in Clients)
				client.Disconnect();

			await Task.WhenAll(serverTasks);
			Logger.Flush();
		}

		private static bool Setup()
		{
			Config.Load();
			Config.SetDefault(new Newtonsoft.Json.Linq.JObject()
			{
				{ "serverPort", 0 },
				{ "vkHttpHostname", "" },
				{ "vkClientId", 0 },
				{ "vkApiVersion", "5.199" },
				{ "vkClientSecret", "" },
				{ "telegramClientSecret", "" },
				{ "telegramChatId", 0 }
			});
			Config.Save();

			if (!Config.CheckNotDefault())
			{
				Logger.LogError("Необходимо заполнить config.json");
				return false;
			}

			Directory.CreateDirectory("profileImages");
			Directory.CreateDirectory("quizzes");

			if (!File.Exists("profileImages/default.jpg"))
			{
				Logger.LogError("Отсутствует стандартная аватарка по пути profileImages/default.jpg");
				return false;
			}

			return true;
		}

		public void Stop()
		{
			tcpListener.Stop();
			VkAuthService.Stop();
			TelegramBotService.Stop();
		}
	}
}
