using Microsoft.EntityFrameworkCore;
using WizzServer;
using WizzServer.Database;
using WizzServer.Net;

namespace Net.Packets.Serverbound
{
	public class UpdateRatingPacket : IPacket
	{
		public int Id => 25;

		public int Score { get; set; }

		public static UpdateRatingPacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new UpdateRatingPacket();
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
			Score = stream.ReadVarInt();
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(Score);

			stream.Lock.Wait();
			stream.WriteVarInt(Id.GetVarIntLength() + (int)packetStream.Length);
			stream.WriteVarInt(Id);
			packetStream.Position = 0;
			packetStream.CopyTo(stream);
			stream.Lock.Release();
		}

		public async ValueTask HandleAsync(Server server, Client client)
		{
			if (client.ProfileId == 0 || client.LastPlayedQuizId == 0)
				return;

			using var db = new ApplicationDbContext();
			var rating = await db.Ratings.FirstOrDefaultAsync(x => x.UserId == client.ProfileId && x.QuizId == client.LastPlayedQuizId);
			if (rating == null)
			{
				await db.Ratings.AddAsync(new Rating()
				{
					UserId = client.ProfileId,
					QuizId = client.LastPlayedQuizId,
					Score = this.Score
				});
			}
			else
				rating.Score = this.Score;

			await db.SaveChangesAsync();

			server.DbUpdateService.AddToUpdate(client.LastPlayedQuizId);
		}
	}
}
