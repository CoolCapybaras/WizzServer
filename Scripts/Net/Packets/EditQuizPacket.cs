using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using WizzServer;
using WizzServer.Database;
using WizzServer.Net;

namespace Net.Packets
{
	public enum EditQuizType
	{
		Get,
		Upload,
		Delete,
		Publish
	}

	public class EditQuizPacket : IPacket
	{
		public int Id => 11;

		public EditQuizType Type { get; set; }
		public int QuizId { get; set; }
		public Quiz Quiz { get; set; }

		public static EditQuizPacket Deserialize(byte[] data)
		{
			using var stream = new WizzStream(data);
			var packet = new EditQuizPacket();
			packet.Populate(stream);
			return packet;
		}

		public void Populate(byte[] data)
		{
			using var stream = new WizzStream(data);
			Populate(stream);
		}

		public void Populate(WizzStream stream)
		{
			Type = (EditQuizType)stream.ReadVarInt();
			if (Type == EditQuizType.Upload)
				Quiz = Quiz.Deserialize(stream);
			else
				QuizId = stream.ReadVarInt();
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(Type);
			if (Type == EditQuizType.Get)
				Quiz.Serialize(packetStream, false);
			else
				packetStream.WriteVarInt(QuizId);

			stream.Lock.Wait();
			stream.WriteVarInt(Id.GetVarIntLength() + (int)packetStream.Length);
			stream.WriteVarInt(Id);
			packetStream.Position = 0;
			packetStream.CopyTo(stream);
			stream.Lock.Release();
		}

		public async ValueTask HandleAsync(Server server, Client client)
		{
			if (client.ProfileId == 0)
				return;

			if (Type == EditQuizType.Get)
			{
				using var db = new ApplicationDbContext();
				var quiz = await db.Quizzes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == QuizId && x.AuthorId == client.ProfileId);
				if (quiz == null || quiz.ModerationStatus == ModerationStatus.InModeration)
					return;

				using var file = File.OpenText($"quizzes/{QuizId}/questions.json");
				quiz.Image = await File.ReadAllBytesAsync($"quizzes/{QuizId}/thumbnail.jpg");
				quiz.Questions = (QuizQuestion[])Misc.JsonSerializer.Deserialize(file, typeof(QuizQuestion[]))!;
				for (int i = 0; i < quiz.Questions.Length; i++)
					quiz.Questions[i].Image = await File.ReadAllBytesAsync($"quizzes/{QuizId}/{i}.jpg");

				client.SendPacket(new EditQuizPacket()
				{
					Type = EditQuizType.Get,
					Quiz = quiz
				});
			}
			else if (Type == EditQuizType.Upload)
			{
				using var db = new ApplicationDbContext();
				var quiz = await db.Quizzes.FirstOrDefaultAsync(x => x.Id == QuizId && x.AuthorId == client.ProfileId);
				if (quiz != null && quiz.ModerationStatus == ModerationStatus.InModeration)
					return;

				if (Quiz.Name.Length < 3 || Quiz.Name.Length > 48)
					return;

				if (Quiz.Description.Length < 3 || Quiz.Description.Length > 128)
					return;

				if (Quiz.Questions.Length == 0 || Quiz.Questions.Length > 50)
					return;

				var questionImages = new Image[Quiz.Questions.Length];

				for (int i = 0; i < Quiz.Questions.Length; i++)
				{
					QuizQuestion question = Quiz.Questions[i];
					if (question.Question.Length > 256)
					{
						DisposeImages(questionImages);
						return;
					}

					if (question.Answers.Length != 2 && question.Answers.Length != 4)
					{
						DisposeImages(questionImages);
						return;
					}

					foreach (var answer in question.Answers)
					{
						if (answer.Length > 256)
						{
							DisposeImages(questionImages);
							return;
						}
					}

					try
					{
						questionImages[i] = Image.Load(question.Image);
					}
					catch (ImageFormatException)
					{
						DisposeImages(questionImages);
						return;
					}

					if (question.Time > 60)
						question.Time = 60;
				}

				Image quizImage;
				try
				{
					quizImage = Image.Load(Quiz.Image);
				}
				catch (ImageFormatException)
				{
					DisposeImages(questionImages);
					return;
				}

				Quiz.Id = 0;
				Quiz.QuestionCount = Quiz.Questions.Length;
				Quiz.AuthorId = client.ProfileId;
				Quiz.ModerationStatus = ModerationStatus.NotModerated;

				if (quiz == null)
					await db.Quizzes.AddAsync(Quiz);
				else
					db.Quizzes.Entry(Quiz).State = EntityState.Modified;
				await db.SaveChangesAsync();

				Misc.ResizeImage(quizImage, 300);
				Directory.CreateDirectory($"quizzes/{Quiz.Id}");
				await quizImage.SaveAsJpegAsync($"quizzes/{Quiz.Id}/thumbnail.jpg");

				using var file = File.CreateText($"quizzes/{Quiz.Id}/questions.json");
				Misc.JsonSerializer.Serialize(file, Quiz.Questions);

				for (int i = 0; i < questionImages.Length; i++)
				{
					Image image = questionImages[i];
					Misc.ResizeImage(image, 300);
					await image.SaveAsJpegAsync($"quizzes/{Quiz.Id}/{i}.jpg");
				}

				client.SendPacket(new EditQuizPacket()
				{
					Type = EditQuizType.Upload,
					QuizId = Quiz.Id
				});

				quizImage.Dispose();
				DisposeImages(questionImages);
			}
			else if (Type == EditQuizType.Delete)
			{
				using var db = new ApplicationDbContext();
				var quiz = await db.Quizzes.FirstOrDefaultAsync(x => x.Id == QuizId && x.AuthorId == client.ProfileId);
				if (quiz == null)
					return;

				quiz.AuthorId = 0;
				quiz.ModerationStatus = ModerationStatus.NotModerated;
				await db.SaveChangesAsync();

				client.SendPacket(new EditQuizPacket()
				{
					Type = EditQuizType.Delete,
					QuizId = QuizId
				});
			}
			else if (Type == EditQuizType.Publish)
			{
				using var db = new ApplicationDbContext();
				var quiz = await db.Quizzes.FirstOrDefaultAsync(x => x.Id == QuizId && x.AuthorId == client.ProfileId);
				if (quiz == null || quiz.ModerationStatus != ModerationStatus.NotModerated)
					return;

				using var file = File.OpenText($"quizzes/{QuizId}/questions.json");
				quiz.Questions = (QuizQuestion[])Misc.JsonSerializer.Deserialize(file, typeof(QuizQuestion[]))!;
				quiz.ModerationStatus = ModerationStatus.InModeration;
				await db.SaveChangesAsync();

				await server.TelegramBotService.SendQuiz(quiz, -1002047778489);

				client.SendPacket(new EditQuizPacket()
				{
					Type = EditQuizType.Publish,
					QuizId = QuizId
				});
			}
		}

		private static void DisposeImages(Image[] images)
		{
			foreach (var image in images)
				image?.Dispose();
		}
	}
}
