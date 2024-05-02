using Newtonsoft.Json.Linq;

#pragma warning disable CS8600 // Преобразование литерала, допускающего значение NULL или возможного значения NULL в тип, не допускающий значение NULL.
#pragma warning disable CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
#pragma warning disable CS8604 // Возможно, аргумент-ссылка, допускающий значение NULL.
namespace WizzServer
{
	public static class Config
	{
		public static int ServerPort { get; set; }
		public static string HttpHostname { get; set; } = string.Empty;

		public static int VkClientId { get; set; }
		public static string VkApiVersion { get; set; } = "5.199";
		public static string VkClientSecret { get; set; } = string.Empty;

		public static string TelegramClientSecret { get; set; } = string.Empty;

		public static void Load()
		{
			if (!File.Exists("config.json"))
			{
				Save();
				return;
			}

			var config = JObject.Parse(File.ReadAllText("config.json"));

			ServerPort = (int)config["serverPort"];
			HttpHostname = (string)config["httpHostname"];
			VkClientId = (int)config["vkClientId"];
			VkApiVersion = (string)config["vkApiVersion"];
			VkClientSecret = (string)config["vkClientSecret"];
			TelegramClientSecret = (string)config["telegramClientSecret"];
		}

		public static void Save()
		{
			var config = new JObject();
			config.Add("serverPort", ServerPort);
			config.Add("httpHostname", HttpHostname);
			config.Add("vkClientId", VkClientId);
			config.Add("vkApiVersion", VkApiVersion);
			config.Add("vkClientSecret", VkClientSecret);
			config.Add("telegramClientSecret", TelegramClientSecret);
			File.WriteAllText("config.json", config.ToString());
		}
	}
}
