using Net.Packets.Clientbound;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using WizzServer.Database;

namespace WizzServer.Services
{
	public class TelegramAuthService : IDisposable
	{
		private Server server;
		private HttpClient httpClient = new();
		private int offset;
		private bool disposed;

		public TelegramAuthService(Server server)
		{
			this.server = server;
		}

		public async Task Start()
		{
			Logger.LogInfo("Telegram auth service started");

			while (true)
			{
				string response;
				try
				{
					response = await httpClient.GetStringAsync($"https://api.telegram.org/bot{Config.TelegramClientSecret}/getUpdates?offset={offset}&timeout=25&allowed_updates=[\"message\"]");
				}
				catch (OperationCanceledException)
				{
					break;
				}

				foreach (var message in JObject.Parse(response)["result"]!)
				{
					string text = (string)message.SelectToken("message.text")!;
					offset = (int)message["update_id"]! + 1;

					string[] args = text.Split();
					if (args.Length != 2
						|| args[0] != "/start"
						|| !server.AuthTokenManager.TryGetToken(args[1], out var authToken)
						|| authToken.ExpirationTime < DateTimeOffset.Now)
						continue;

					Client client = authToken.Client;

					var token = AuthTokenManager.GenerateToken();
					var realname = (string)message.SelectToken("message.from.id")!;
					var ip = client.GetIP();
					var timestamp = DateTimeOffset.UtcNow;

					using var db = new ApplicationDbContext();
					DbUser? user = db.Users.FirstOrDefault(x => x.Realname == realname && !x.IsVk);
					if (user == null)
					{
						user = new DbUser()
						{
							Username = (string)message.SelectToken("message.from.first_name")!,
							Realname = realname,
							Token = token,
							Ip = ip,
							Lastlogin = timestamp,
							Regdate = timestamp,
							Regip = ip,
							IsVk = false
						};
						await db.Users.AddAsync(user);
						await db.SaveChangesAsync();

						using var stream = await httpClient.GetStreamAsync(await GetUserPhoto(realname));
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

					Logger.LogInfo($"{ip} authed as {client.Name} using telegram");
				}
			}

			this.Dispose();
		}

		public void Stop() => httpClient.Dispose();

		public async Task<string> GetUserPhoto(string realname)
		{
			var response = JObject.Parse(await httpClient.GetStringAsync($"https://api.telegram.org/bot{Config.TelegramClientSecret}/getUserProfilePhotos?user_id={realname}&limit=1"));
			response = JObject.Parse(await httpClient.GetStringAsync($"https://api.telegram.org/bot{Config.TelegramClientSecret}/getFile?file_id={response.SelectToken("result.photos[0][0].file_id")}"));
			return $"https://api.telegram.org/file/bot{Config.TelegramClientSecret}/{response.SelectToken("result.file_path")}";
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;

			httpClient.Dispose();

			GC.SuppressFinalize(this);
		}
	}
}
