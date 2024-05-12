using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Serverbound
{
	public class JoinLobbyPacket : IPacket
	{
		private static readonly Dictionary<int, int> easterEggQuizzes = new()
		{
			{ 250252, 9 }
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

		public async ValueTask HandleAsync(Server server, Client client)
		{
			if (!client.IsAuthed)
				return;

			if (easterEggQuizzes.TryGetValue(LobbyId, out int quizId))
			{
				if (!server.Rooms.TryGetValue(LobbyId, out Room? quizRoom))
				{
					Quiz quiz = (await server.QuizManager.GetQuiz(quizId))!;
					quizRoom = new Room(server, quiz, LobbyId, client);
					server.Rooms.TryAdd(LobbyId, quizRoom);

					Logger.LogInfo($"{client.Name} created new room #{LobbyId} {quiz.Name}");
					return;
				}
				else if (quizRoom.IsStarted)
				{
					client.SendMessage("Unknown error");
					return;
				}

				quizRoom.OnClientJoin(client);
				return;
			}

			if (!server.Rooms.TryGetValue(LobbyId, out Room? room))
			{
				client.SendMessage("Lobby doesn't exist");
				return;
			}
			else if (room.IsStarted)
			{
				client.SendMessage("Lobby already started");
				return;
			}

			room.OnClientJoin(client);
		}
	}
}
