using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Clientbound
{
	public class GameStartedPacket : IPacket
	{
		public int Id => 16;

		public string QuizName { get; set; }

		public GameStartedPacket()
		{

		}

		public GameStartedPacket(string quizName)
		{
			this.QuizName = quizName;
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
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteString(QuizName);

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
