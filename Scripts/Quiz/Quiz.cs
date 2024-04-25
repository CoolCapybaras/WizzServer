using SixLabors.ImageSharp;

namespace WizzServer
{
	public class Quiz
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public Image Image { get; set; }
		public string ImagePath { get; set; }
		public string Description { get; set; }
		public int QuestionsCount { get; set; }
		public int AuthorId { get; set; }
		public bool IsHidden { get; set; }
		public QuizQuestion[] Questions { get; set; }

		public Quiz Init()
		{
			ImagePath = Config.GetString("domain") + $"quizzes/{Id}.jpg";

			for (int i = 0; i < Questions.Length; i++)
			{
				Questions[i].Id = i + 1;
				Questions[i].CalculateTimings();
			}
			return this;
		}
	}
}
