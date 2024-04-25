namespace WizzServer
{
	public class GameQuiz
	{
		private int roomId;

		public string Id { get; set; }
		public string Name { get; set; }
		public int AuthorId { get; set; }
		public Queue<QuizQuestion> Questions { get; set; }

		public GameQuiz(Quiz quiz, int roomId)
		{
			Id = quiz.Id;
			Name = quiz.Name;
			AuthorId = quiz.AuthorId;
			Questions = new Queue<QuizQuestion>(quiz.Questions.Select(x => new QuizQuestion(x).ShuffleAnswers()).ToArray().Shuffle().Take(quiz.QuestionsCount));

			this.roomId = roomId;
		}

		public void CreateCache()
		{
			string domain = Config.GetString("domain");
			Directory.CreateDirectory("cache/" + roomId);
			foreach (QuizQuestion question in Questions)
			{
				string url = $"cache/{roomId}/{Utils.RandomId()}.jpg";
				question.ImagePath = domain + url;
				File.CreateSymbolicLink(url, Directory.GetCurrentDirectory() + $"/quizzes/{Id}/{question.Id}.jpg");
			}
		}

		public void DeleteCache()
		{
			Directory.Delete("cache/" + roomId, true);
		}
	}
}
