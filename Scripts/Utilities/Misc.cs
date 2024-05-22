using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace WizzServer
{
	public static class Misc
	{
		public static JsonSerializer JsonSerializer { get; } = new()
		{
			ContractResolver = new CamelCasePropertyNamesContractResolver(),
			Formatting = Formatting.Indented
		};

		public static void ResizeImage(Image image, int size)
		{
			if (image.Width <= size && image.Height <= size)
				return;

			float ratio = Math.Min((float)size / image.Width, (float)size / image.Height);
			image.Mutate(x => x.Resize((int)(image.Width * ratio), (int)(image.Height * ratio)));
		}

		public static async Task<byte[]> SaveProfileImage(Stream stream, int userId)
		{
			using var image = await Image.LoadAsync(stream);
			return await SaveProfileImage(image, userId);
		}

		public static async Task<byte[]> SaveProfileImage(Image image, int userId)
		{
			ResizeImage(image, 100);

			using var memoryStream = new MemoryStream();
			await image.SaveAsJpegAsync(memoryStream);

			using var fileStream = new FileStream($"profileImages/{userId}.jpg", FileMode.Create, FileAccess.Write, FileShare.Read, 4096, true);
			memoryStream.Position = 0;
			await memoryStream.CopyToAsync(fileStream);

			return memoryStream.ToArray();
		}
	}
}
