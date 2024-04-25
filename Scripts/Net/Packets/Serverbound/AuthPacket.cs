using Net.Packets.Clientbound;
using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Serverbound
{
	public class AuthPacket : IPacket
	{
		public int Id => 2;

		public int Type { get; set; }
		public string Name { get; set; }
		public string Token { get; set; }

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
			Type = stream.ReadVarInt();
			if (Type == 0)
			{
				Name = stream.ReadString();
				Token = string.Empty;
			}
			else
			{
				Token = stream.ReadString();
				Name = string.Empty;
			}
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(Type);
			if (Type == 0)
			{
				packetStream.WriteString(Name);
			}
			else
			{
				packetStream.WriteString(Token);
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
			if (client.IsAuthed)
			{
				client.SendMessage("Already authed");
				return ValueTask.CompletedTask;
			}

			if (Type == 0)
			{
				if (!UpdateProfilePacket.NameRegex.IsMatch(Name))
				{
					client.SendMessage("Wrong name");
					return ValueTask.CompletedTask;
				}

				client.IsAuthed = true;
				client.Name = Name;
				client.SendPacket(new AuthSuccessPacket(client.Id, Name));
				Logger.LogInfo($"{client.GetIP()} authed as {client.Name} anonymously");
			}

			return ValueTask.CompletedTask;
		}
	}
}
