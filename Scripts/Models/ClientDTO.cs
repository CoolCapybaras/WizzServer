using SixLabors.ImageSharp;

namespace WizzServer.Models
{
	public class ClientDTO
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public Image Image { get; set; }

		public ClientDTO()
		{

		}

		public ClientDTO(Client client)
		{
			Id = client.Id;
			Name = client.Name;
			Image = client.Image;
		}
	}
}
