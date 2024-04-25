using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace WizzServer
{
	public static class Utils
	{
		public static readonly JsonSerializer JsonSerializer = new JsonSerializer()
		{
			NullValueHandling = NullValueHandling.Ignore,
			ContractResolver = new CamelCasePropertyNamesContractResolver()
		};

		public static JsonSerializerSettings JsonSerializerSettings { get; } = new JsonSerializerSettings()
		{
			NullValueHandling = NullValueHandling.Ignore,
			ContractResolver = new CamelCasePropertyNamesContractResolver()
		};	

		public static Random Random { get; } = new Random();

		public static int RandomId()
		{
			return Random.Next();
		}

		public static int RandomGuestId()
		{
			return Random.Next() - int.MaxValue;
		}

		public static int RandomRoomId()
		{
			return Random.Next(100000, 1000000);
		}

		public static JObject Serialize(object value)
		{
			return JObject.FromObject(value, JsonSerializer);
		}

		public static void ResizeImage(Image image, int size)
		{
			if (image.Width <= size && image.Height <= size) return;
			float ratio = Math.Min((float)size / image.Width, (float)size / image.Height);
			image.Mutate(x => x.Resize((int)(image.Width * ratio), (int)(image.Height * ratio)));
		}
	}
}
