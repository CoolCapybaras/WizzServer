using Net.Packets;
using Net.Packets.Clientbound;
using WizzServer.Utilities.Collections;

namespace WizzServer
{
	public class Room
	{
		private Server server;
		private Quiz quiz;
		private Game game;
		private int currentClientIdx;

		public int Id { get; set; }
		public bool IsStarted { get; set; }
		public Client? Host { get; set; }
		public ConcurrentHashSet<Client> Clients { get; set; } = [];

		public Room(Server server, Quiz quiz, int id, Client client)
		{
			this.server = server;
			this.quiz = quiz;
			this.Id = id;
			this.Host = client;
			this.game = new Game(this, quiz);

			Logger.LogInfo($"{client.Name} created new room #{id} {quiz.Name}");
		}

		public async Task OnClientJoinAsync(Client client)
		{
			client.Room = this;
			client.RoomId = Interlocked.Increment(ref currentClientIdx);

			await BroadcastAsync(new ClientJoinedPacket(client));

			Clients.Add(client);

			await client.QueuePacketAsync(new LobbyJoinedPacket(Id, quiz, Clients.ToArray()));
		}

		public async Task OnClientLeaveAsync(Client client)
		{
			if (!IsStarted && Clients.GetCountNoLocks() <= 1)
			{
				Destroy(true);
				return;
			}

			Clients.TryRemove(client);

			if (client == Host)
				Host = null;

			if (!IsStarted)
				await BroadcastAsync(new ClientLeavedPacket(client.RoomId));
			else
				game.OnClientLeave(client);
		}

		public void OnGameStart(Client client)
		{
			if (client != Host || IsStarted)
				return;

			Task.Run(game.Start);
			IsStarted = true;

			Logger.LogInfo($"Room #{Id} was started with {Clients.GetCountNoLocks()} clients");
		}

		public void OnClientAnswer(Client client, int id)
		{
			game.OnClientAnswer(client, id);
		}

		public void OnGameContinue(Client client)
		{
			if (client != Host)
				return;

			game.OnGameContinue();
		}

		public async Task BroadcastAsync(IPacket packet)
		{
			foreach (var client in Clients)
			{
				await client.QueuePacketAsync(packet);
			}
		}

		public void Destroy(bool noClients = false)
		{
			foreach (var client in Clients)
				client.Room = null;

			server.QuizManager.ReturnQuiz(quiz);
			server.Rooms.TryRemove(Id, out _);
			Logger.LogInfo($"Room #{Id} was destroyed" + (noClients ? " due to lack of clients" : ""));
		}
	}
}
