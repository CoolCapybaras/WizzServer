using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Net;
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
		private int updateId;

		private string tgUsername;
		private string tgToken;
		private string tgChatId;

		private bool disposed;

		public TelegramBotService(Server server)
		{
			this.server = server;

			tgUsername = Config.GetString("tgUsername");
			tgToken = Config.GetString("tgToken");
			tgChatId = Config.GetString("tgChatId");
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
				HttpResponseMessage httpMessage;
				try
				{
					httpMessage = await httpClient.GetAsync($"https://api.telegram.org/bot{tgToken}/getUpdates?offset={updateId}&timeout=25&allowed_updates=[\"message\",\"callback_query\"]");
				}
				catch (HttpRequestException)
				{
					Logger.LogError("Telegram HttpRequestException");
					continue;
				}
				catch (OperationCanceledException)
				{
					break;
				}

				if (httpMessage.StatusCode != HttpStatusCode.OK)
				{
					Logger.LogError($"Telegram wrong status code");
					httpMessage.Dispose();
					continue;
				}

				string response = await httpMessage.Content.ReadAsStringAsync();

				foreach (JObject update in JObject.Parse(response)["result"])
				{
					updateId = (int)update["update_id"]! + 1;

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
						var realname = (long)message["from"]["id"]!;
						var ip = client.GetIP();
						var timestamp = DateTimeOffset.UtcNow;

						using var db = new ApplicationDbContext();
						DbUser? user = await db.Users.FirstOrDefaultAsync(x => x.Realname == realname && !x.IsVk);
						if (user == null)
						{
							user = new DbUser()
							{
								Username = (string)message["from"]["first_name"]!,
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

						await SendMessage((string)message["chat"]["id"]!, "✅ Авторизация успешна");
						Logger.LogInfo($"{ip} authed as {client.Name} using telegram");
					}
					else if (update.ContainsKey("callback_query"))
					{
						int[] args = Array.ConvertAll(((string)update["callback_query"]["data"]!).Split(), int.Parse);
						string chatId = (string)update["callback_query"]["message"]["chat"]["id"]!;
						int messageId = (int)update["callback_query"]["message"]["message_id"]!;

						using var db = new ApplicationDbContext();
						var quiz = await db.Quizzes.FirstOrDefaultAsync(x => x.Id == args[0]);
						if (quiz == null || quiz.ModerationStatus != ModerationStatus.InModeration)
						{
							await ClearQuiz(chatId, messageId, args[2]);
							continue;
						}

						quiz.ModerationStatus = args[1] == 1 ? ModerationStatus.ModerationAccepted : ModerationStatus.ModerationRejected;
						await db.SaveChangesAsync();

						string text = quiz.ModerationStatus == ModerationStatus.ModerationAccepted
							? $"✅ Викторина #{quiz.Id} {quiz.Name} была одобрена"
							: $"❌ Викторина #{quiz.Id} {quiz.Name} была отклонена";

						await ClearQuiz(chatId, messageId, args[2]);
						await SendMessage(chatId, text);

						Logger.LogInfo(text);
					}
				}

				httpMessage.Dispose();
			}

			Logger.LogInfo("Shutting down telegram bot service...");
			this.Dispose();
		}

		public void Stop() => httpClient.Dispose();

		private async Task<string> GetUserPhoto(long realname)
		{
			var response = JObject.Parse(await httpClient.GetStringAsync($"https://api.telegram.org/bot{tgToken}/getUserProfilePhotos?user_id={realname}&limit=1"));
			response = JObject.Parse(await httpClient.GetStringAsync($"https://api.telegram.org/bot{tgToken}/getFile?file_id={response["result"]["photos"][0][0]["file_id"]}"));
			return $"https://api.telegram.org/file/bot{tgToken}/{response["result"]["file_path"]}";
		}

		private async Task SendMessage(string chatId, string text)
		{
			var content = new FormUrlEncodedContent(
			[
				new KeyValuePair<string, string>("chat_id", chatId),
				new KeyValuePair<string, string>("text", text)
			]);

			using var _ = await httpClient.PostAsync($"https://api.telegram.org/bot{tgToken}/sendMessage", content);
		}

		public async Task SendQuiz(Quiz quiz)
		{
			await _lock.WaitAsync();

			int messageCount = quiz.QuestionCount + 1;
			bool needThumbnail = true;

			for (int i = 0; i < quiz.QuestionCount;)
			{
				int count = Math.Clamp(quiz.QuestionCount - i, 1, 10);

				using var content = new MultipartFormDataContent
				{
					{ new StringContent(tgChatId), "chat_id" },
				};

				if (needThumbnail)
				{
					if (count == 10)
						count -= 1;
					content.Add(new StringContent($"[{{\"type\":\"photo\",\"media\":\"attach://thumbnail.jpg\"}},{string.Join(',', Enumerable.Range(i, count).Select((x) => $"{{\"type\":\"photo\",\"media\":\"attach://{x}.jpg\"}}"))}]"), "media");
					content.Add(new StreamContent(new FileStream($"quizzes/{quiz.Id}/thumbnail.jpg", FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true)), "thumbnail.jpg", "thumbnail.jpg");
					needThumbnail = false;
				}
				else
				{
					content.Add(new StringContent($"[{string.Join(',', Enumerable.Range(i, count).Select((x) => $"{{\"type\":\"photo\",\"media\":\"attach://{x}.jpg\"}}"))}]"), "media");
				}

				foreach (int q in Enumerable.Range(i, count))
					content.Add(new StreamContent(new FileStream($"quizzes/{quiz.Id}/{q}.jpg", FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true)), $"{q}.jpg", $"{q}.jpg");

				using var _ = await httpClient.PostAsync($"https://api.telegram.org/bot{tgToken}/sendMediaGroup", content);
				i += count;
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
					{ "chat_id", tgChatId },
					{ "text", sb.ToString() }
				};

				messageCount++;
				if (i == quiz.QuestionCount)
					content.Add("reply_markup", $"{{\"inline_keyboard\":[[{{\"text\":\"✅\",\"callback_data\":\"{quiz.Id} 1 {messageCount}\"}},{{\"text\":\"❌\",\"callback_data\":\"{quiz.Id} 0 {messageCount}\"}}]]}}");

				using var _ = await httpClient.PostAsync($"https://api.telegram.org/bot{tgToken}/sendMessage", new FormUrlEncodedContent(content));
				sb.Clear();
			}

			_lock.Release();
		}

		public string GetAuthUrl(string token) => $"https://t.me/{tgUsername}?start={token}";

		private async Task ClearQuiz(string chatId, int messageId, int messageCount)
		{
			using var _ = await httpClient.GetAsync($"https://api.telegram.org/bot{tgToken}/deleteMessages?chat_id={chatId}&message_ids=[{string.Join(',', Enumerable.Range(messageId - messageCount + 1, messageCount))}]");
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
