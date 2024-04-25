using Newtonsoft.Json.Linq;

namespace WizzServer
{
	public static class VkApi
	{
		private static HttpClient httpClient = new HttpClient();

		public static async Task<string> GetAccessToken(string code)
		{
			using HttpResponseMessage httpResponse = await httpClient.GetAsync($"https://oauth.vk.com/access_token?client_id=7691413&client_secret=PBzY6D4rJFE1EbePGwKe&redirect_uri=http://localhost:8888/auth&code={code}");
			JObject response = JObject.Parse(await httpResponse.Content.ReadAsStringAsync());
			return (string)response["access_token"]!;
		}

		public static async Task<JObject> GetUserInfo(string accessToken)
		{
			using HttpResponseMessage httpResponse = await httpClient.GetAsync($"https://api.vk.com/method/users.get?fields=photo_100&access_token={accessToken}&v=5.199");
			JObject response = JObject.Parse(await httpResponse.Content.ReadAsStringAsync());
			return (JObject)response.SelectToken("response[0]")!;
		}
	}
}
