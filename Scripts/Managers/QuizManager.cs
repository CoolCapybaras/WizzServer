using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using WizzServer.Database;

namespace WizzServer.Managers
{
	public class QuizManager
	{
		private ConcurrentDictionary<int, Quiz> cachedQuizzes = new();

		public async Task<Quiz?> GetQuiz(int id)
		{
			if (cachedQuizzes.TryGetValue(id, out var quiz))
			{
				Interlocked.Increment(ref quiz.ReferenceCount);
				return quiz;
			}

			using var db = new ApplicationDbContext();
			quiz = await db.Quizzes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
			if (quiz == null)
				return null;

			using var file = File.OpenText($"quizzes/{id}/questions.json");
			quiz.Image = await File.ReadAllBytesAsync($"quizzes/{id}/thumbnail.jpg");
			quiz.Questions = (QuizQuestion[])Misc.JsonSerializer.Deserialize(file, typeof(QuizQuestion[]))!;
			for (int i = 0; i < quiz.Questions.Length; i++)
				quiz.Questions[i].Image = await File.ReadAllBytesAsync($"quizzes/{id}/{i}.jpg");

			Interlocked.Increment(ref quiz.ReferenceCount);
			cachedQuizzes.TryAdd(id, quiz);

			return quiz;
		}

		public void ReturnQuiz(Quiz quiz)
		{
			if (Interlocked.Decrement(ref quiz.ReferenceCount) == 0)
				cachedQuizzes.TryRemove(quiz.Id, out _);
		}
	}
}
