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
			if (Type == AuthType.Anonymous)
				Name = stream.ReadString();
			else if (Type == AuthType.Token)
				Token = stream.ReadString();
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(Type);
			if (Type == AuthType.Anonymous)
				packetStream.WriteString(Name);
			else if (Type == AuthType.Token)
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
			{
				client.SendMessage("Already authed");
				return;
			}

			if (Type == AuthType.Anonymous)
			{
				if (!UpdateProfilePacket.NameRegex.IsMatch(Name))
				{
					client.SendMessage("Wrong name");
					return;
				}

				client.IsAuthed = true;
				client.Name = Name;
				client.SendPacket(new AuthResultPacket(client.Id, client.Name, null));
				Logger.LogInfo($"{client.GetIP()} authed as {client.Name} anonymously");
			}
			else if (Type == AuthType.Token)
			{
				using var db = new ApplicationDbContext();
				var user = db.Users.FirstOrDefault(x => x.Token == Token);
				if (user == null)
					return;

				user.Ip = client.GetIP();
				user.Lastlogin = DateTimeOffset.UtcNow;
				await db.SaveChangesAsync();

				//client.Id = user.Id;
				client.IsAuthed = true;
				client.Name = user.Username;
				//client.Image = await Image.LoadAsync($"profileImages/{client.Id}.jpg");
				client.SendPacket(new AuthResultPacket(client.Id, client.Name, client.Image));
				Logger.LogInfo($"{user.Ip} authed as {client.Name} using token");
			}
			else if (Type == AuthType.VK)
			{
				var token = server.AuthTokenManager.CreateToken(client);
				client.SendPacket(new AuthResultPacket($"https://oauth.vk.com/authorize?client_id={Config.VkClientId}&display=page&redirect_uri={Config.HttpHostname}&response_type=code&state={token}&v={Config.VkApiVersion}"));
			}
			else if (Type == AuthType.Telegram)
			{
				var token = server.AuthTokenManager.CreateToken(client);
				client.SendPacket(new AuthResultPacket($"https://t.me/wizz_bot?start={token}"));
			}

			return;
		}
	}
}
