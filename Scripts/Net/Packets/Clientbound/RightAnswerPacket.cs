using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Clientbound
{
	public class RightAnswerPacket : IPacket
	{
		public int Id => 24;

		public QuizAnswer Answer { get; set; }
		public int RoundScore { get; set; }

		public RightAnswerPacket()
		{

		}

		public RightAnswerPacket(QuizAnswer answer, int rountScore)
		{
			this.Answer = answer;
			this.RoundScore = rountScore;
		}

		public static RightAnswerPacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new RightAnswerPacket();
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
			Answer = QuizAnswer.Deserialize(stream);
			RoundScore = stream.ReadVarInt();
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			Answer.Serialize(packetStream);
			packetStream.WriteVarInt(RoundScore);

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
