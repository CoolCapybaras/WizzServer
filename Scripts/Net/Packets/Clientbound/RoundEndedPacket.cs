using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Clientbound
{
	public class RoundEndedPacket : IPacket
	{
		public int Id => 20;

		public Dictionary<int, int> Score { get; set; }

		public RoundEndedPacket()
		{

		}

		public RoundEndedPacket(Dictionary<int, int> score)
		{
			this.Score = score;
		}

		public static RoundEndedPacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new RoundEndedPacket();
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
			int count = stream.ReadVarInt();
			Score = new Dictionary<int, int>();
			for (int i = 0; i < count; i++)
			{
				Score.Add(stream.ReadVarInt(), stream.ReadVarInt());
			}
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(Score.Count);
			foreach (var score in Score)
			{
				packetStream.WriteVarInt(score.Key);
				packetStream.WriteVarInt(score.Value);
			}

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
