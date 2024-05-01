using Net.Packets.Clientbound;
using Newtonsoft.Json.Linq;
using System.Net;
using WizzServer.Database;

namespace WizzServer.Services
{
	public class VkAuthService : IDisposable
	{
		private static readonly char[] separators = ['?', '=', '&'];

		private Server server;
		private HttpListener httpListener = new();
		private HttpClient httpClient = new();
		private bool disposed;

		public VkAuthService(Server server)
		{
			this.server = server;
		}

		public async Task Start()
		{
			httpListener.Prefixes.Add($"{Config.HttpHostname}/");
			httpListener.Start();
			Logger.LogInfo("VK auth service started");

			while (true)
			{
				HttpListenerContext context;
				try
				{
					context = await httpListener.GetContextAsync();
				}
				catch (HttpListenerException)
				{
					break;
				}

				var request = context.Request;
				var response = context.Response;

				string[] args = request.RawUrl!.Split(separators);
				if (args.Length != 5
					|| !server.AuthTokenManager.TryGetToken(args[4], out var authToken)
					|| authToken.ExpirationTime < DateTimeOffset.Now)
				{
					response.OutputStream.Close();
					continue;
				}

				JObject vkResponse = await GetAccessToken(args[2]);
				if (vkResponse.ContainsKey("error"))
				{
					response.OutputStream.Close();
					continue;
				}

				Client client = authToken.Client;

				var token = AuthTokenManager.GenerateToken();
				var realname = (string)vkResponse["user_id"]!;
				var ip = client.GetIP();
				var timestamp = DateTimeOffset.UtcNow;

				using var db = new ApplicationDbContext();
				DbUser? user = db.Users.FirstOrDefault(x => x.Realname == realname && x.IsVk);
				if (user == null)
				{
					vkResponse = await GetUserInfo((string)vkResponse["access_token"]!);

					user = new DbUser()
					{
						Username = (string)vkResponse["first_name"]!,
						Realname = realname,
						Token = token,
						Ip = ip,
						Lastlogin = timestamp,
						Regdate = timestamp,
						Regip = ip,
						IsVk = true
					};
					await db.Users.AddAsync(user);
					await db.SaveChangesAsync();

					using var stream = await httpClient.GetStreamAsync((string)vkResponse["photo_100"]!);
					//client.Id = user.Id;
					client.IsAuthed = true;
					client.Name = user.Username;
					//client.Image = await Image.LoadAsync(stream);
					//await client.Image.SaveAsJpegAsync($"profileImages/{client.Id}.jpg");
				}
				else
				{
					user.Token = token;
					user.Ip = ip;
					user.Lastlogin = timestamp;
					await db.SaveChangesAsync();

					//client.Id = user.Id;
					client.IsAuthed = true;
					client.Name = user.Username;
					//client.Image = await Image.LoadAsync($"profileImages/{client.Id}.jpg");
				}

				client.SendPacket(new AuthResultPacket(client.Id, client.Name, client.Image, token));
				server.AuthTokenManager.RemoveToken(authToken.Token);
				response.OutputStream.Close();

				Logger.LogInfo($"{ip} authed as {client.Name} using vk");
			}

			this.Dispose();
		}

		public void Stop() => httpListener.Close();

		public async Task<JObject> GetAccessToken(string code)
		{
			var response = await httpClient.GetStringAsync($"https://oauth.vk.com/access_token?client_id={Config.VkClientId}&client_secret={Config.VkClientSecret}&redirect_uri={Config.HttpHostname}&code={code}");
			return JObject.Parse(response);
		}

		public async Task<JObject> GetUserInfo(string accessToken)
		{
			var response = await httpClient.GetStringAsync($"https://api.vk.com/method/users.get?fields=photo_100&access_token={accessToken}&v={Config.VkApiVersion}");
			return (JObject)JObject.Parse(response).SelectToken("response[0]")!;
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;

			httpListener.Close();
			httpClient.Dispose();

			GC.SuppressFinalize(this);
		}
	}
}
