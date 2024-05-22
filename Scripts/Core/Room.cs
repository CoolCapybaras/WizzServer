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

			OnClientJoin(client);
		}

		public void OnClientJoin(Client client)
		{
			client.Room = this;
			client.RoomId = Interlocked.Increment(ref currentClientIdx);

			Broadcast(new ClientJoinedPacket(client));

			Clients.Add(client);

			client.SendPacket(new LobbyJoinedPacket(Id, quiz, Clients.ToArray()));
		}

		public void OnClientLeave(Client client)
		{
			Clients.TryRemove(client);

			if (Clients.GetCountNoLocks() <= 1)
			{
				Destroy();
				return;
			}

			if (client == Host)
				Host = null;

			Broadcast(new ClientLeavedPacket(client.RoomId));
		}

		public void OnGameStart(Client client)
		{
			if (client != Host || IsStarted)
				return;

			Task.Run(game.Start);
			IsStarted = true;
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

		public void Broadcast(IPacket packet)
		{
			foreach (var client in Clients)
			{
				client.SendPacket(packet);
			}
		}

		public void Destroy()
		{
			foreach (var client in Clients)
				client.Room = null;

			server.QuizManager.ReturnQuiz(quiz);
			server.Rooms.TryRemove(Id, out _);
			Logger.LogInfo($"Room #{Id} was destroyed");
		}
	}
}
