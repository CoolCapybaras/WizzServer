using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Clientbound
{
	public class RoundStartedPacket : IPacket
	{
		public int Id => 19;

		public QuizQuestion question { get; set; }

		public RoundStartedPacket()
		{

		}

		public RoundStartedPacket(QuizQuestion question)
		{
			this.question = question;
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
			question = new QuizQuestion();
			question.Question = stream.ReadString();
			int count = stream.ReadVarInt();
			string[] answers = new string[count];
			for (int i = 0; i < count; i++)
				answers[i] = stream.ReadString();
			question.Image = stream.ReadImage();
			question.Time = stream.ReadVarInt();
			question.Countdown = stream.ReadVarInt();
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteString(question.Question);
			packetStream.WriteVarInt(question.Answers.Length);
			for (int i = 0; i < question.Answers.Length; i++)
				packetStream.WriteString(question.Answers[i]);
			packetStream.WriteImage(question.Image);
			packetStream.WriteVarInt(question.Time);
			packetStream.WriteVarInt(question.Countdown);

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
