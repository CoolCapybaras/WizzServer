using SixLabors.ImageSharp;
using System.Text.RegularExpressions;
using WizzServer;
using WizzServer.Net;

namespace Net.Packets.Serverbound
{
	public class UpdateProfilePacket : IPacket
	{
		public static readonly Regex NameRegex = new("^[A-Za-zА-Яа-я0-9_]{3,18}$");

		public int Id => 9;

		public int Type { get; set; }
		public string Name { get; set; }
		public Image Image { get; set; }

		public static UpdateProfilePacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new UpdateProfilePacket();
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
				Image = stream.ReadImage();
			}
			else if (Type == 1)
			{
				Name = stream.ReadString();
			}
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(Type);
			if (Type == 0)
			{
				packetStream.WriteString(Name);
				packetStream.WriteImage(Image);
			}
			else if (Type == 1)
			{
				packetStream.WriteString(Name);
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
			if (!NameRegex.IsMatch(Name))
			{
				Image?.Dispose();
				client.SendMessage("Wrong name");
				return ValueTask.CompletedTask;
			}

			client.Name = Name;

			if (Type == 0)
			{
				Utils.ResizeImage(Image, 100);
				client.Image?.Dispose();
				client.Image = Image;
			}

			return ValueTask.CompletedTask;
		}
	}
}
