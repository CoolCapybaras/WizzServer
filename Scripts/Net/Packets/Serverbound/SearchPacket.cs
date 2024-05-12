using Microsoft.EntityFrameworkCore;
using Net.Packets.Clientbound;
using WizzServer;
using WizzServer.Database;
using WizzServer.Net;

namespace Net.Packets.Serverbound
{
	public class SearchPacket : IPacket
	{
		public int Id => 3;

		public string QuizName { get; set; }

		public static SearchPacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new SearchPacket();
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

		public async ValueTask HandleAsync(Server server, Client client)
		{
			if (!client.IsAuthed)
				return;

			using var db = new ApplicationDbContext();
			var quizzes = db.Quizzes.AsNoTracking().Where(x => EF.Functions.ILike(x.Name, $"{QuizName}%") && x.IsShown).Take(5).ToArray();
			foreach (var quiz in quizzes)
				quiz.Image = await File.ReadAllBytesAsync($"quizzes/{quiz.Id}/thumbnail.jpg");
			client.SendPacket(new SearchResultPacket(quizzes));
		}
	}
}
