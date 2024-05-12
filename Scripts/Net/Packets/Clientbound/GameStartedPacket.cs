using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Clientbound
{
	public class GameStartedPacket : IPacket
	{
		public int Id => 18;

		public string QuizName { get; set; }
		public QuizQuestion[] QuizQuestions { get; set; }

		public GameStartedPacket()
		{

		}

		public GameStartedPacket(string quizName, QuizQuestion[] quizQuestions)
		{
			this.QuizName = quizName;
			this.QuizQuestions = quizQuestions;
		}

		public static GameStartedPacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new GameStartedPacket();
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
			QuizName = stream.ReadString();

			int count = stream.ReadVarInt();
			QuizQuestions = new QuizQuestion[count];
			for (int i = 0; i < count; i++)
				QuizQuestions[i] = QuizQuestion.Deserialize(stream);
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteString(QuizName);
			packetStream.WriteVarInt(QuizQuestions.Length);
			for (int i = 0; i < QuizQuestions.Length; i++)
				QuizQuestions[i].Serialize(packetStream);

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
