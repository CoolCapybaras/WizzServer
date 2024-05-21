using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using System.Text.RegularExpressions;
using WizzServer;
using WizzServer.Database;
using WizzServer.Net;

namespace Net.Packets.Serverbound
{
	public class UpdateProfilePacket : IPacket
	{
		public static readonly Regex NameRegex = new("^[A-Za-zА-Яа-я0-9_ ]{3,24}$");

		public int Id => 10;

		public int Type { get; set; }
		public string Name { get; set; }
		public byte[] Image { get; set; }

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

		public async ValueTask HandleAsync(Server server, Client client)
		{
			if (!client.IsAuthed)
				return;

			if (!NameRegex.IsMatch(Name))
			{
				client.SendMessage("Wrong name");
				return;
			}

			client.Name = Name;

			if (client.ProfileId != 0)
			{
				using var db = new ApplicationDbContext();
				var user = (await db.Users.FirstOrDefaultAsync(x => x.Id == client.ProfileId))!;
				user.Username = Name;
				await db.SaveChangesAsync();

				if (Type == 0)
				{
					Image image;
					try
					{
						image = SixLabors.ImageSharp.Image.Load(Image);
					}
					catch (ImageFormatException)
					{
						return;
					}

					client.Image = await Misc.SaveProfileImage(image, client.ProfileId);

					image.Dispose();
				}
			}
		}
	}
}
