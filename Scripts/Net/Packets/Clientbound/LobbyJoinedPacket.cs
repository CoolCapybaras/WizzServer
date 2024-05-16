using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Clientbound
{
	public class LobbyJoinedPacket : IPacket
	{
		public int Id => 15;

		public int LobbyId { get; set; }
		public Quiz Quiz { get; set; }
		public Client[] Clients { get; set; }

		public LobbyJoinedPacket()
		{

		}

		public LobbyJoinedPacket(int lobbyId, Quiz quiz, Client[] clients)
		{
			this.LobbyId = lobbyId;
			this.Quiz = quiz;
			this.Clients = clients;
		}

		public static LobbyJoinedPacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new LobbyJoinedPacket();
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
			LobbyId = stream.ReadVarInt();
			Quiz = Quiz.Deserialize(stream);
			int count = stream.ReadVarInt();
			// Clients = new ClientDTO[count];
			for (int i = 0; i < count; i++)
			{
				// Clients[i] = new ClientDTO();
				Clients[i].RoomId = stream.ReadVarInt();
				Clients[i].Name = stream.ReadString();
				Clients[i].Image = stream.ReadImage();
			}
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(LobbyId);
			Quiz.Serialize(packetStream);
			packetStream.WriteVarInt(Clients.Length);
			for (int i = 0; i < Clients.Length; i++)
			{
				packetStream.WriteVarInt(Clients[i].RoomId);
				packetStream.WriteString(Clients[i].Name);
				packetStream.WriteImage(Clients[i].Image);
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
