using Newtonsoft.Json;
using SixLabors.ImageSharp;

namespace WizzServer
{
	public class QuizQuestion
	{
		[JsonIgnore]
		public int Id { get; set; }
		public string Question { get; set; }
		public string[] Answers { get; set; }
		public Image Image { get; set; }
		public string ImagePath { get; set; }
		public int Time { get; set; }
		public int Countdown { get; set; }
		[JsonIgnore]
		public int RightAnswer { get; set; }

		public QuizQuestion()
		{

		}

		public QuizQuestion(QuizQuestion question)
		{
			Id = question.Id;
			Question = question.Question;
			Answers = (string[])question.Answers.Clone();
			Time = question.Time;
			Countdown = question.Countdown;
			RightAnswer = question.RightAnswer;
		}

		public void CalculateTimings()
		{
			Time = 30;
			Countdown = Math.Max((int)(Question.Length * 0.075f), 3);
		}

		public QuizQuestion ShuffleAnswers()
		{
			string answer = Answers[RightAnswer];
			Answers.Shuffle();
			RightAnswer = Array.IndexOf(Answers, answer);
			return this;
		}
	}
}
