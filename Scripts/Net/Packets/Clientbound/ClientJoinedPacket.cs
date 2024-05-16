using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Clientbound
{
	public class ClientJoinedPacket : IPacket
	{
		public int Id => 16;

		public Client Client { get; set; }

		public ClientJoinedPacket()
		{

		}

		public ClientJoinedPacket(Client client)
		{
			this.Client = client;
		}

		public static ClientJoinedPacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new ClientJoinedPacket();
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
			// Client = new ClientDTO();
			Client.RoomId = stream.ReadVarInt();
			Client.Name = stream.ReadString();
			Client.Image = stream.ReadImage();
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(Client.RoomId);
			packetStream.WriteString(Client.Name);
			packetStream.WriteImage(Client.Image);

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
