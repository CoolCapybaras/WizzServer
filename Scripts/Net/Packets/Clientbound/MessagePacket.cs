using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Clientbound
{
	public class MessagePacket : IPacket
	{
		public int Id => 1;

		public int Type { get; set; }
		public string Text { get; set; }

		public MessagePacket()
		{

		}

		public MessagePacket(int type, string text)
		{
			this.Type = type;
			this.Text = text;
		}

		public static MessagePacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new MessagePacket();
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
			Type = stream.ReadVarInt();
			Text = stream.ReadString();
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(Type);
			packetStream.WriteString(Text);

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
