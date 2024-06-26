﻿using Microsoft.EntityFrameworkCore;
using Net.Packets.Clientbound;
using WizzServer;
using WizzServer.Database;
using WizzServer.Net;

namespace Net.Packets.Serverbound
{
	public enum AuthType
	{
		Anonymous,
		Token,
		VK,
		Telegram
	}

	public class AuthPacket : IPacket
	{
		public int Id => 2;

		public AuthType Type { get; set; }
		public string Name { get; set; }
		public string Token { get; set; }

		public AuthPacket()
		{

		}

		public AuthPacket(AuthType type, string? data)
		{
			Type = type;
			if (type == AuthType.Anonymous)
				Name = data!;
			else if (type == AuthType.Token)
				Token = data!;
		}

		public static AuthPacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new AuthPacket();
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
			Type = (AuthType)stream.ReadVarInt();
			Name = stream.ReadString();
			Token = stream.ReadString();
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(Type);
			packetStream.WriteString(Name);
			packetStream.WriteString(Token);

			stream.Lock.Wait();
			stream.WriteVarInt(Id.GetVarIntLength() + (int)packetStream.Length);
			stream.WriteVarInt(Id);
			packetStream.Position = 0;
			packetStream.CopyTo(stream);
			stream.Lock.Release();
		}

		public async ValueTask HandleAsync(Server server, Client client)
		{
			if (client.IsAuthed)
				return;

			if (Type == AuthType.Anonymous)
			{
				if (!UpdateProfilePacket.NameRegex.IsMatch(Name))
				{
					await client.SendMessageAsync("Wrong name");
					return;
				}

				await client.AuthAsync(0, Name);

				Logger.LogInfo($"{client.GetIP()} authed as {client.Name} anonymously");
			}
			else if (Type == AuthType.Token)
			{
				using var db = new ApplicationDbContext();
				var user = await db.Users.FirstOrDefaultAsync(x => x.Token == Token);
				if (user == null)
					return;

				user.Ip = client.GetIP();
				user.Lastlogin = DateTimeOffset.UtcNow;
				await db.SaveChangesAsync();

				var image = await File.ReadAllBytesAsync($"profileImages/{user.Id}.jpg");
				await client.AuthAsync(user.Id, user.Username, image);

				Logger.LogInfo($"{user.Ip} authed as {client.Name} using token");
			}
			else if (Type == AuthType.VK)
			{
				string token = server.AuthTokenManager.CreateToken(client);
				await client.QueuePacketAsync(new AuthResultPacket(server.VkAuthService.GetAuthUrl(token)));
			}
			else if (Type == AuthType.Telegram)
			{
				string token = server.AuthTokenManager.CreateToken(client);
				await client.QueuePacketAsync(new AuthResultPacket(server.TelegramBotService.GetAuthUrl(token)));
			}
		}
	}
}
