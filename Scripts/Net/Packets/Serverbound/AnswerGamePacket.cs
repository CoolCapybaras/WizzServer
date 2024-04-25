using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Serverbound
{
	public class AnswerGamePacket : IPacket
	{
		public int Id => 8;

		public int AnswerId { get; set; }

		public static AnswerGamePacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new AnswerGamePacket();
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
			AnswerId = stream.ReadVarInt();
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(AnswerId);

			stream.Lock.Wait();
			stream.WriteVarInt(Id.GetVarIntLength() + (int)packetStream.Length);
			stream.WriteVarInt(Id);
			packetStream.Position = 0;
			packetStream.CopyTo(stream);
			stream.Lock.Release();
		}

		public ValueTask HandleAsync(Server server, Client client)
		{
			if (client.Room == null)
				return ValueTask.CompletedTask;

			client.Room.OnClientAnswer(client, AnswerId);

			return ValueTask.CompletedTask;
		}
	}
}
