using Newtonsoft.Json.Linq;

namespace WizzServer
{
	public static class Config
	{
		private static JObject config = new();

		public static void Load()
		{
			if (!File.Exists("config.json"))
				return;

			using var file = File.OpenText($"config.json");
			config = (JObject)Misc.JsonSerializer.Deserialize(file, typeof(JObject))!;
		}

		public static void SetDefault()
		{
			var defaultConfig = new JObject()
			{
				{ "serverPort", 0 },

				{ "dbHost", "" },
				{ "dbPort", 0 },
				{ "dbDatabase", "" },
				{ "dbUsername", "" },
				{ "dbPassword", "" },

				{ "vkHttpHostname", "" },
				{ "vkClientId", 0 },
				{ "vkClientSecret", "" },
				{ "vkApiVersion", "5.199" },

				{ "tgUsername", "" },
				{ "tgToken", "" },
				{ "tgChatId", 0 }
			};

			foreach (var property in defaultConfig.Properties())
			{
				if (config.ContainsKey(property.Name))
					continue;

				config.Add(property);
			}
		}

		public static bool CheckNotDefault()
		{
			foreach (var property in config.Properties())
			{
				if ((property.Value.Type == JTokenType.String
					&& (string)property.Value! == "")
					|| (property.Value.Type == JTokenType.Integer
					&& (long)property.Value! == 0))
					return false;
			}

			return true;
		}

		public static void Save()
		{
			using var file = File.CreateText($"config.json");
			Misc.JsonSerializer.Serialize(file, config);
		}

		public static int GetInt(string name)
		{
			return (int)config[name]!;
		}

		public static long GetLong(string name)
		{
			return (long)config[name]!;
		}

		public static string GetString(string name)
		{
			return (string)config[name]!;
		}
	}
}
