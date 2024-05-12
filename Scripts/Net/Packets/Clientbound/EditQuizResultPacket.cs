using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Clientbound
{
	public class EditQuizResultPacket : IPacket
	{
		public int Id => 25;

		public Quiz Quiz { get; set; }

		public EditQuizResultPacket()
		{

		}

		public EditQuizResultPacket(Quiz quiz)
		{
			Quiz = quiz;
		}

		public static EditQuizResultPacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new EditQuizResultPacket();
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
			Quiz = Quiz.Deserialize(stream);
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
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
