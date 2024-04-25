using Net.Packets;
using Net.Packets.Clientbound;
using WizzServer.Models;

namespace WizzServer
{
	public class Room
	{
		private Server server;
		private Quiz quiz;
		private Client host;
		private Game game;
		private bool isStarted;

		public int Id { get; set; }
		public bool IsStarted { get { return isStarted; } }

		public Dictionary<int, Client> Clients { get; } = [];

		public Room(Server server, Quiz quiz, int id, Client client)
		{
			this.server = server;
			this.quiz = quiz;
			this.Id = id;
			this.host = client;
			this.game = new Game(this, quiz);

			OnClientJoin(client);
		}

		public void OnClientJoin(Client client)
		{
			client.Room = this;

			Broadcast(new ClientJoinedPacket(new ClientDTO(client)));

			Clients.Add(client.Id, client);

			client.SendPacket(new LobbyJoinedPacket(Id, quiz, Clients.Values.Select(x => new ClientDTO(x)).ToArray()));
		}

		public void OnClientLeave(Client client)
		{
			client.Room = null!;

			if (Clients.Count == 1)
			{
				server.Rooms.TryRemove(Id, out _);
				Logger.LogInfo($"Room #{Id} was destroyed");
			}

			Clients.Remove(client.Id);

			Broadcast(new ClientLeavedPacket(client.Id));
		}

		public void OnGameStart(Client client)
		{
			if (client != host || game.IsStarted) return;

			isStarted = true;
			Task.Run(game.Start);
		}

		public void OnClientAnswer(Client client, int id)
		{
			game.OnAnswer(client, id);
		}

		public void Broadcast(IPacket packet)
		{
			foreach (Client client in Clients.Values)
			{
				client.SendPacket(packet);
			}
		}
	}
}
