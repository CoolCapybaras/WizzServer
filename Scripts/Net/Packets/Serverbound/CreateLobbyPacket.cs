using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Serverbound
{
	public class CreateLobbyPacket : IPacket
	{
		public int Id => 4;

		public string QuizId { get; set; }

		public static CreateLobbyPacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new CreateLobbyPacket();
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
			QuizId = stream.ReadString();
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteString(QuizId);

			stream.Lock.Wait();
			stream.WriteVarInt(Id.GetVarIntLength() + (int)packetStream.Length);
			stream.WriteVarInt(Id);
			packetStream.Position = 0;
			packetStream.CopyTo(stream);
			stream.Lock.Release();
		}

		public ValueTask HandleAsync(Server server, Client client)
		{
			if (!client.IsAuthed || !server.Quizzes.ContainsKey(QuizId))
				return ValueTask.CompletedTask;

			int id = Utils.RandomRoomId();
			var room = new Room(server, server.Quizzes[QuizId], id, client);
			server.Rooms.TryAdd(id, room);
			Logger.LogInfo($"{client.Name} created new room #{id} {server.Quizzes[QuizId].Name}");

			return ValueTask.CompletedTask;
		}
	}
}
