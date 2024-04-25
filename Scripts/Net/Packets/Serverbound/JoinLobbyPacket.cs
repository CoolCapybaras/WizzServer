using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Serverbound
{
	public class JoinLobbyPacket : IPacket
	{
		private static readonly Dictionary<int, string> easterEggQuizzes = new()
		{
			{ 250252, "easteregg" }
		};

		public int Id => 5;

		public int LobbyId { get; set; }

		public static JoinLobbyPacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new JoinLobbyPacket();
			packet.Populate(stream);
			return packet;
		}

		public void Populate(byte[] data)
		{
			using var stream = new WizzStream(data);
			Populate(stream);
		}

		public void Populate(WizzStream stream)
		{
			LobbyId = stream.ReadVarInt();
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(LobbyId);

			stream.Lock.Wait();
			stream.WriteVarInt(Id.GetVarIntLength() + (int)packetStream.Length);
			stream.WriteVarInt(Id);
			packetStream.Position = 0;
			packetStream.CopyTo(stream);
			stream.Lock.Release();
		}

		public ValueTask HandleAsync(Server server, Client client)
		{
			if (!client.IsAuthed)
				return ValueTask.CompletedTask;

			if (easterEggQuizzes.ContainsKey(LobbyId))
			{
				if (!server.Rooms.ContainsKey(LobbyId))
				{
					Quiz quiz = server.Quizzes[easterEggQuizzes[LobbyId]];
					Room room = new Room(server, quiz, LobbyId, client);
					server.Rooms.TryAdd(LobbyId, room);

					Logger.LogInfo($"{client.Name} created new room #{LobbyId} {quiz.Name}");
					return ValueTask.CompletedTask;
				}
				else if (server.Rooms[LobbyId].IsStarted)
				{
					client.SendMessage("Unknown error");
					return ValueTask.CompletedTask;
				}

				server.Rooms[LobbyId].OnClientJoin(client);
				return ValueTask.CompletedTask;
			}

			if (!server.Rooms.ContainsKey(LobbyId))
			{
				client.SendMessage("Lobby doesn't exist");
				return ValueTask.CompletedTask;
			}
			else if (server.Rooms[LobbyId].IsStarted)
			{
				client.SendMessage("Lobby already started");
				return ValueTask.CompletedTask;
			}

			server.Rooms[LobbyId].OnClientJoin(client);

			return ValueTask.CompletedTask;
		}
	}
}
