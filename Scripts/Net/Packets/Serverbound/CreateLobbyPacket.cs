using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Serverbound
{
	public class CreateLobbyPacket : IPacket
	{
		public int Id => 4;

		public int QuizId { get; set; }

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
			QuizId = stream.ReadVarInt();
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(QuizId);

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

			var quiz = await server.QuizManager.GetQuiz(QuizId);
			if (quiz == null)
				return;

			int id;

			do id = Random.Shared.Next(100000, 1000000);
			while (server.Rooms.ContainsKey(id));

			var room = new Room(server, quiz, id, client);
			server.Rooms.TryAdd(id, room);
			Logger.LogInfo($"{client.Name} created new room #{id} {quiz.Name}");
		}
	}
}
