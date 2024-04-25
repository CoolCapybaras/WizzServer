using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Serverbound
{
	public class StartGamePacket : IPacket
	{
		public int Id => 7;

		public static StartGamePacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new StartGamePacket();
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

		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();

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

			client.Room.OnGameStart(client);

			return ValueTask.CompletedTask;
		}
	}
}
