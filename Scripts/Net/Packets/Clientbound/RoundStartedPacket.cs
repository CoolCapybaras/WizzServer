using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Clientbound
{
	public class RoundStartedPacket : IPacket
	{
		public int Id => 22;

		public int QuestionIdx { get; set; }
		public int Delay { get; set; }

		public RoundStartedPacket()
		{

		}

		public RoundStartedPacket(int questionIdx, int delay)
		{
			this.QuestionIdx = questionIdx;
			this.Delay = delay;
		}

		public static RoundStartedPacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new RoundStartedPacket();
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
			QuestionIdx = stream.ReadVarInt();
			Delay = stream.ReadVarInt();
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(QuestionIdx);
			packetStream.WriteVarInt(Delay);

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
