using WizzServer;
using WizzServer.Models;
using WizzServer.Net;

namespace Net.Packets.Clientbound
{
	public class LobbyJoinedPacket : IPacket
	{
		public int Id => 13;

		public int LobbyId { get; set; }
		public Quiz Quiz { get; set; }
		public ClientDTO[] Clients { get; set; }

		public LobbyJoinedPacket()
		{

		}

		public LobbyJoinedPacket(int lobbyId, Quiz quiz, ClientDTO[] clients)
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
			Quiz = new Quiz
			{
				Id = stream.ReadString(),
				Name = stream.ReadString(),
				Image = stream.ReadImage(),
				Description = stream.ReadString(),
				QuestionsCount = stream.ReadVarInt(),
				AuthorId = stream.ReadVarInt()
			};
			int count = stream.ReadVarInt();
			Clients = new ClientDTO[count];
			for (int i = 0; i < count; i++)
			{
				Clients[i] = new ClientDTO
				{
					Id = stream.ReadVarInt(),
					Name = stream.ReadString(),
					Image = stream.ReadImage()
				};
			}
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(LobbyId);
			packetStream.WriteString(Quiz.Id);
			packetStream.WriteString(Quiz.Name);
			packetStream.WriteImage(Quiz.Image);
			packetStream.WriteString(Quiz.Description);
			packetStream.WriteVarInt(Quiz.QuestionsCount);
			packetStream.WriteVarInt(Quiz.AuthorId);
			packetStream.WriteVarInt(Clients.Length);
			for (int i = 0; i < Clients.Length; i++)
			{
				packetStream.WriteVarInt(Clients[i].Id);
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
