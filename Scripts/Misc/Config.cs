using Newtonsoft.Json.Linq;

namespace WizzServer
{
	public static class Config
	{
		private static JObject config = new JObject();

		public static void Load()
		{
			if (!File.Exists("config.json")) return;
			config = JObject.Parse(File.ReadAllText("config.json"));
		}

		public static void SetDefault(JObject obj)
		{
			foreach (JProperty property in obj.Properties())
			{
				if (config.ContainsKey(property.Name)) continue;
				config.Add(property.Name, property.Value);
			}
		}

		public static void Save()
		{
			File.WriteAllText("config.json", config.ToString());
		}

		public static string GetString(string name)
		{
			return (string)config.GetValue(name);
		}
	}
}
