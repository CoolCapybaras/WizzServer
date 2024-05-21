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
			QuizId = stream.ReadVarInt();
			Quiz = Quiz.Deserialize(stream);
		}

		public void Serialize(WizzStream stream)
		{
			using var packetStream = new WizzStream();
			packetStream.WriteVarInt(Type);
			packetStream.WriteVarInt(QuizId);
			Quiz.Serialize(packetStream, false);

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
				if (quiz == null || quiz.IsModerating)
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
				if (quiz != null && quiz.IsModerating)
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
						for (int q = 0; q < i; q++)
							questionImages[q].Dispose();
						return;
					}

					foreach (var answer in question.Answers)
					{
						if (answer.Length > 256)
						{
							for (int q = 0; q < i; q++)
								questionImages[q].Dispose();
							return;
						}
					}

					try
					{
						questionImages[i] = Image.Load(question.Image);
					}
					catch (ImageFormatException)
					{
						for (int q = 0; q < i; q++)
							questionImages[q].Dispose();
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
					for (int q = 0; q < questionImages.Length; q++)
						questionImages[q].Dispose();
					return;
				}

				Quiz.QuestionCount = Quiz.Questions.Length;
				Quiz.AuthorId = client.ProfileId;

				if (quiz == null)
					await db.Quizzes.AddAsync(Quiz);
				else
					db.Quizzes.Entry(Quiz).State = EntityState.Modified;
				await db.SaveChangesAsync();

				Misc.ResizeImage(quizImage, 300);
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
				for (int i = 0; i < questionImages.Length; i++)
					questionImages[i].Dispose();
			}
			else if (Type == EditQuizType.Delete)
			{
				using var db = new ApplicationDbContext();
				var quiz = await db.Quizzes.FirstOrDefaultAsync(x => x.Id == QuizId && x.AuthorId == client.ProfileId);
				if (quiz == null)
					return;

				quiz.IsShown = false;
				quiz.AuthorId = 0;
				await db.SaveChangesAsync();

				client.SendPacket(new EditQuizPacket()
				{
					Type = EditQuizType.Delete,
					QuizId = Quiz.Id
				});
			}
			else if (Type == EditQuizType.Publish)
			{
				using var db = new ApplicationDbContext();
				var quiz = await db.Quizzes.FirstOrDefaultAsync(x => x.Id == QuizId && x.AuthorId == client.ProfileId);
				if (quiz == null || quiz.IsModerating)
					return;

				using var file = File.OpenText($"quizzes/{QuizId}/questions.json");
				quiz.Questions = (QuizQuestion[])Misc.JsonSerializer.Deserialize(file, typeof(QuizQuestion[]))!;

				quiz.IsModerating = true;
				await db.SaveChangesAsync();

				await server.TelegramBotService.SendQuiz(quiz, -1002047778489);

				client.SendPacket(new EditQuizPacket()
				{
					Type = EditQuizType.Publish,
					QuizId = Quiz.Id
				});
			}
		}
	}
}
