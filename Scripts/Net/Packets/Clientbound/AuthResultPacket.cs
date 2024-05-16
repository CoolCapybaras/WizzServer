using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Clientbound
{
	[Flags]
	public enum AuthResultFlags
	{
		Ok = 1,
		HasUrl = 2,
		HasToken = 4,
	}

	public class AuthResultPacket : IPacket
	{
		public int Id => 13;

		public AuthResultFlags Flags;
		public int ClientId { get; set; }
		public string Name { get; set; }
		public byte[] Image { get; set; }
		public string Url { get; set; }
		public string Token { get; set; }

		public AuthResultPacket()
		{

		}

		public AuthResultPacket(string url)
		{
			this.Flags = AuthResultFlags.HasUrl;
			this.Url = url;
		}

		public AuthResultPacket(int clientId, string name, byte[] image, string? token)
		{
			this.Flags = AuthResultFlags.Ok;
			this.ClientId = clientId;
			this.Name = name;
			this.Image = image;

			if (token != null)
			{
				this.Token = token;
				this.Flags |= AuthResultFlags.HasToken;
			}
		}

		public static AuthResultPacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new AuthResultPacket();
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
			Flags = (AuthResultFlags)stream.ReadVarInt();
			if (Flags.HasFlag(AuthResultFlags.Ok))
			{
				ClientId = stream.ReadVarInt();
				Name = stream.ReadString();
				Image = stream.ReadImage();
				if (Flags.HasFlag(AuthResultFlags.HasToken))
					Token = stream.ReadString();
			}
			else if (Flags.HasFlag(AuthResultFlags.HasUrl))
			{
				Url = stream.ReadString();
			}
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(Flags);
			if (Flags.HasFlag(AuthResultFlags.Ok))
			{
				packetStream.WriteVarInt(ClientId);
				packetStream.WriteString(Name);
				packetStream.WriteImage(Image);
				if (Flags.HasFlag(AuthResultFlags.HasToken))
					packetStream.WriteString(Token);
			}
			else if (Flags.HasFlag(AuthResultFlags.HasUrl))
			{
				packetStream.WriteString(Url);
			}

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
