using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Clientbound
{
	public class SearchResultPacket : IPacket
	{
		public int Id => 14;

		public Quiz[] Quizzes { get; set; }

		public SearchResultPacket()
		{

		}

		public SearchResultPacket(Quiz[] quizzes)
		{
			this.Quizzes = quizzes;
		}

		public static SearchResultPacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new SearchResultPacket();
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
			Quizzes = new Quiz[count];
			for (int i = 0; i < count; i++)
				Quizzes[i] = Quiz.Deserialize(stream);
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(Quizzes.Length);
			for (int i = 0; i < Quizzes.Length; i++)
				Quizzes[i].Serialize(packetStream);

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
