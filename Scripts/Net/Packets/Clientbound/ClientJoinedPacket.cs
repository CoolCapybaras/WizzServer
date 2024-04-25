using WizzServer;
using WizzServer.Models;
using WizzServer.Net;

namespace Net.Packets.Clientbound
{
	public class ClientJoinedPacket : IPacket
	{
		public int Id => 14;

		public ClientDTO Client { get; set; }

		public ClientJoinedPacket()
		{

		}

		public ClientJoinedPacket(ClientDTO client)
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
			Client.Id = stream.ReadVarInt();
			Client.Name = stream.ReadString();
			Client.Image = stream.ReadImage();
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(Client.Id);
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
