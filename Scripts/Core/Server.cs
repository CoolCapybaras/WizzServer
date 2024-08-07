﻿using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using WizzServer.Database;
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
		public VkAuthService VkAuthService { get; private set; }
		public TelegramBotService TelegramBotService { get; private set; }
		public DbUpdateService DbUpdateService { get; private set; }
		
		private TcpListener tcpListener;
		private Task[] serverTasks;
		private CancellationTokenSource cancellationSource = new();

		public async Task Start()
		{
			if (!Setup())
				return;

			VkAuthService = new VkAuthService(this);
			TelegramBotService = new TelegramBotService(this, cancellationSource.Token);
			DbUpdateService = new DbUpdateService(cancellationSource.Token);
			serverTasks =
			[
				Task.Run(VkAuthService.Start),
				Task.Run(TelegramBotService.Start),
				Task.Run(DbUpdateService.Start)
			];

			tcpListener = new TcpListener(IPAddress.Any, 8887);
			tcpListener.Start();
			Logger.LogInfo("Server started on port 8887");

			while (true)
			{
				Socket socket;
				try
				{
					socket = await tcpListener.AcceptSocketAsync(cancellationSource.Token);
				}
				catch (OperationCanceledException)
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
			Config.SetDefault();
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

			ApplicationDbContext.ConnectionString = $"Host={Config.GetString("dbHost")};Port={Config.GetInt("dbPort")};Database={Config.GetString("dbDatabase")};Username={Config.GetString("dbUsername")};Password={Config.GetString("dbPassword")}";

			return true;
		}

		public void Stop()
		{
			cancellationSource.Cancel();
			VkAuthService.Stop(); // HttpListener doesn't accept cancellation token
		}
	}
}
