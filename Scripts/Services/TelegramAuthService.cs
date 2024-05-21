using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using WizzServer.Database;
using WizzServer.Managers;

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
			try
			{
				await ProcessService();
			}
			catch (Exception e)
			{
				Logger.LogError(e.ToString());
			}
		}

		private async Task ProcessService()
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
						|| authToken.ExpirationTime < DateTimeOffset.UtcNow)
						continue;

					Client client = authToken.Client;

					var token = AuthTokenManager.GenerateToken();
					var realname = (string)message.SelectToken("message.from.id")!;
					var ip = client.GetIP();
					var timestamp = DateTimeOffset.UtcNow;

					using var db = new ApplicationDbContext();
					DbUser? user = await db.Users.FirstOrDefaultAsync(x => x.Realname == realname && !x.IsVk);
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
						var image = await Misc.SaveProfileImage(stream, user.Id);

						client.Auth(user.Id, user.Username, image, token);
					}
					else
					{
						user.Token = token;
						user.Ip = ip;
						user.Lastlogin = timestamp;
						await db.SaveChangesAsync();

						var image = await File.ReadAllBytesAsync($"profileImages/{user.Id}.jpg");
						client.Auth(user.Id, user.Username, image, token);
					}

					server.AuthTokenManager.RemoveToken(authToken.Token);

					Logger.LogInfo($"{ip} authed as {client.Name} using telegram");
				}
			}

			this.Dispose();
		}

		public void Stop() => httpClient.Dispose();

		private async Task<string> GetUserPhoto(string realname)
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
