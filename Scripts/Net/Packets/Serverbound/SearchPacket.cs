﻿using Microsoft.EntityFrameworkCore;
using Net.Packets.Clientbound;
using WizzServer;
using WizzServer.Database;
using WizzServer.Net;

namespace Net.Packets.Serverbound
{
	public enum SearchType
	{
		Default,
		Author,
		History
	}

	public class SearchPacket : IPacket
	{
		public int Id => 3;

		public string QuizName { get; set; }
		public SearchType SearchType { get; set; }
		public int Offset { get; set; }
		public int Count { get; set; }

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
			SearchType = (SearchType)stream.ReadVarInt();
			Offset = stream.ReadVarInt();
			Count = stream.ReadVarInt();
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteString(QuizName);
			packetStream.WriteVarInt(SearchType);
			packetStream.WriteVarInt(Offset);
			packetStream.WriteVarInt(Count);

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

			if (Offset < 0)
				Offset = 0;
			Count = Math.Clamp(Count, 0, 10);

			using var db = new ApplicationDbContext();
			Quiz[] quizzes = SearchType switch
			{
				SearchType.Author => db.Quizzes.AsNoTracking().Where(x => EF.Functions.ILike(x.Name, $"{QuizName}%") && x.AuthorId == client.ProfileId).Skip(Offset).Take(Count).ToArray(),
				SearchType.History => db.Histories.AsNoTracking().Where(x => x.UserId == client.ProfileId).Skip(Offset).Take(Count).Include(h => h.Quiz).Select(x => x.Quiz).ToArray(),
				_ => db.Quizzes.AsNoTracking().Where(x => EF.Functions.ILike(x.Name, $"{QuizName}%") && x.ModerationStatus == ModerationStatus.ModerationAccepted).Skip(Offset).Take(Count).ToArray(),
			};

			foreach (var quiz in quizzes)
				quiz.Image = await File.ReadAllBytesAsync($"quizzes/{quiz.Id}/thumbnail.jpg");
			await client.QueuePacketAsync(new SearchResultPacket(quizzes));
		}
	}
}
