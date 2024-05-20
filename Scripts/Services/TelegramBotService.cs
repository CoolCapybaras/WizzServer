using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Text;
using WizzServer.Database;
using WizzServer.Managers;

namespace WizzServer.Services
{
	public class TelegramBotService : IDisposable
	{
		private Server server;
		private HttpClient httpClient = new();
		private SemaphoreSlim _lock = new(1, 1);
		private int offset;
		private bool disposed;

		public TelegramBotService(Server server)
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
			Logger.LogInfo("Telegram bot service started");

			while (true)
			{
				string response;
				try
				{
					response = await httpClient.GetStringAsync($"https://api.telegram.org/bot{Config.TelegramClientSecret}/getUpdates?offset={offset}&timeout=25&allowed_updates=[\"message\",\"callback_query\"]");
				}
				catch (OperationCanceledException)
				{
					break;
				}

				foreach (JObject update in JObject.Parse(response)["result"]!)
				{
					offset = (int)update["update_id"]! + 1;

					if (update.ContainsKey("message"))
					{
						JObject message = (JObject)update["message"]!;

						if (!message.ContainsKey("text"))
							continue;

						string[] args = ((string)message["text"]!).Split();
						if (args.Length != 2
							|| args[0] != "/start"
							|| !server.AuthTokenManager.TryGetToken(args[1], out var authToken)
							|| authToken.ExpirationTime < DateTimeOffset.UtcNow)
							continue;

						Client client = authToken.Client;

						var token = AuthTokenManager.GenerateToken();
						var realname = (string)message["from"]!["id"]!;
						var ip = client.GetIP();
						var timestamp = DateTimeOffset.UtcNow;

						using var db = new ApplicationDbContext();
						DbUser? user = await db.Users.FirstOrDefaultAsync(x => x.Realname == realname && !x.IsVk);
						if (user == null)
						{
							user = new DbUser()
							{
								Username = (string)message["from"]!["first_name"]!,
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
					else if (update.ContainsKey("callback_query"))
					{
						int[] args = Array.ConvertAll(((string)update["callback_query"]!["data"]!).Split(), int.Parse);
						string chatId = (string)update["callback_query"]!["message"]!["chat"]!["id"]!;
						int messageId = (int)update["callback_query"]!["message"]!["message_id"]!;

						using var db = new ApplicationDbContext();
						var quiz = await db.Quizzes.FirstAsync(x => x.Id == args[0]);
						if (!quiz.IsModerating)
						{
							await ClearQuiz(chatId, messageId, args[2]);
							continue;
						}

						quiz.IsModerating = false;
						quiz.IsShown = args[1] == 1;
						await db.SaveChangesAsync();

						string text = quiz.IsShown ? $"✅ Викторина #{quiz.Id} {quiz.Name} была одобрена"
							: $"❌ Викторина #{quiz.Id} {quiz.Name} была отклонена";

						var content = new FormUrlEncodedContent(
						[
							new KeyValuePair<string, string>("chat_id", chatId),
							new KeyValuePair<string, string>("text", text)
						]);

						// using var _ = await httpClient.GetAsync($"https://api.telegram.org/bot{Config.TelegramClientSecret}/answerCallbackQuery?callback_query_id={(string)update["callback_query"]!["id"]!}");
						await ClearQuiz(chatId, messageId, args[2]);
						using var _ = await httpClient.PostAsync($"https://api.telegram.org/bot{Config.TelegramClientSecret}/sendMessage", content);
					}
				}
			}

			this.Dispose();
		}

		public void Stop() => httpClient.Dispose();

		private async Task<string> GetUserPhoto(string realname)
		{
			var response = JObject.Parse(await httpClient.GetStringAsync($"https://api.telegram.org/bot{Config.TelegramClientSecret}/getUserProfilePhotos?user_id={realname}&limit=1"));
			response = JObject.Parse(await httpClient.GetStringAsync($"https://api.telegram.org/bot{Config.TelegramClientSecret}/getFile?file_id={response["result"]!["photos"]![0]![0]!["file_id"]}"));
			return $"https://api.telegram.org/file/bot{Config.TelegramClientSecret}/{response["result"]!["file_path"]}";
		}

		public async Task SendQuiz(Quiz quiz, string chatId)
		{
			await _lock.WaitAsync();

			int messageCount = 0;

			for (int i = 0; i < quiz.QuestionCount; i += 10)
			{
				int count = Math.Clamp(1, 10, quiz.QuestionCount - i);

				var content = new MultipartFormDataContent
				{
					{ new StringContent(chatId), "chat_id" },
					{ new StringContent($"[{string.Join(',', Enumerable.Range(i, count).Select((x) => $"{{\"type\":\"photo\",\"media\":\"attach://{x}.jpg\"}}"))}]"), "media" }
				};

				messageCount += count;
				int endIdx = count + i;
				for (int q = i; q < endIdx; q++)
					content.Add(new StreamContent(new FileStream($"quizzes/{quiz.Id}/{q}.jpg", FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true)), $"{q}.jpg", $"{q}.jpg");

				using var _ = await httpClient.PostAsync($"https://api.telegram.org/bot{Config.TelegramClientSecret}/sendMediaGroup", content);
			}

			var sb = new StringBuilder();
			sb.Append($"Викторина #{quiz.Id} {quiz.Name}\n- {quiz.Description}\n\n");

			for (int i = 0; i < quiz.QuestionCount;)
			{
				for (; i < quiz.QuestionCount; i++)
				{
					string question = $"{i + 1}. {quiz.Questions[i].Question}\n{string.Join('\n', quiz.Questions[i].Answers.Select(x => $"- {x}"))}\n\n";
					if (sb.Length + question.Length > 4096)
						break;

					sb.Append(question);
				}

				var content = new Dictionary<string, string>
				{
					{ "chat_id", chatId },
					{ "text", sb.ToString() }
				};

				messageCount++;
				if (i == quiz.QuestionCount)
					content.Add("reply_markup", $"{{\"inline_keyboard\":[[{{\"text\":\"✅\",\"callback_data\":\"{quiz.Id} 1 {messageCount}\"}},{{\"text\":\"❌\",\"callback_data\":\"{quiz.Id} 0 {messageCount}\"}}]]}}");

				using var _ = await httpClient.PostAsync($"https://api.telegram.org/bot{Config.TelegramClientSecret}/sendMessage", new FormUrlEncodedContent(content));
				sb.Clear();
			}

			_lock.Release();
		}

		private async Task ClearQuiz(string chatId, int messageId, int messageCount)
		{
			using var _ = await httpClient.GetAsync($"https://api.telegram.org/bot{Config.TelegramClientSecret}/deleteMessages?chat_id={chatId}&message_ids=[{string.Join(',', Enumerable.Range(messageId - messageCount + 1, messageCount))}]");
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;

			httpClient.Dispose();
			_lock.Dispose();

			GC.SuppressFinalize(this);
		}
	}
}
