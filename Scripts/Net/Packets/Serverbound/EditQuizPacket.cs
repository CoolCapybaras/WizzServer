using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Serverbound
{
	public class EditQuizPacket : IPacket
	{
		public int Id => 11;

		public int QuizId { get; set; }
		public Quiz Quiz { get; set; }

		public static EditQuizPacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new EditQuizPacket();
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
			Quiz = Quiz.Deserialize(stream);
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(QuizId);
			Quiz.Serialize(packetStream, false);

			stream.Lock.Wait();
			stream.WriteVarInt(Id.GetVarIntLength() + (int)packetStream.Length);
			stream.WriteVarInt(Id);
			packetStream.Position = 0;
			packetStream.CopyTo(stream);
			stream.Lock.Release();
		}

		public ValueTask HandleAsync(Server server, Client client)
		{
			throw new NotImplementedException();
		}
	}
}
