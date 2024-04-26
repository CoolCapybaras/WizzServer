using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Clientbound
{
	public class RoundStartedPacket : IPacket
	{
		public int Id => 19;

		public QuizQuestion Question { get; set; }

		public RoundStartedPacket()
		{

		}

		public RoundStartedPacket(QuizQuestion question)
		{
			this.Question = question;
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
			Question = new QuizQuestion();
			Question.Question = stream.ReadString();
			int count = stream.ReadVarInt();
			string[] answers = new string[count];
			for (int i = 0; i < count; i++)
				answers[i] = stream.ReadString();
			Question.Image = stream.ReadImage();
			Question.Time = stream.ReadVarInt();
			Question.Countdown = stream.ReadVarInt();
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteString(Question.Question);
			packetStream.WriteVarInt(Question.Answers.Length);
			for (int i = 0; i < Question.Answers.Length; i++)
				packetStream.WriteString(Question.Answers[i]);
			packetStream.WriteImage(Question.Image);
			packetStream.WriteVarInt(Question.Time);
			packetStream.WriteVarInt(Question.Countdown);

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
