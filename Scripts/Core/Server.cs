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
		public TelegramBotService TelegramBotService { get; set; }

		private VkAuthService vkAuthService;
		private TcpListener tcpListener;

		public async Task Start()
		{
			if (!File.Exists("profileImages/default.jpg"))
			{
				Logger.LogError("Отсутствует стандартная аватарка по пути profileImages/default.jpg");
				return;
			}

			Config.Load();

			vkAuthService = new VkAuthService(this);
			TelegramBotService = new TelegramBotService(this);
			_ = Task.Run(vkAuthService.Start);
			_ = Task.Run(TelegramBotService.Start);

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
				client.OnConnect();
				_ = Task.Run(client.Start);
			}

			Logger.LogInfo("Shutting down...");

			foreach (var client in Clients)
				client.Disconnect();
		}

		public void Stop()
		{
			tcpListener.Stop();
			vkAuthService.Stop();
			TelegramBotService.Stop();
		}
	}
}
