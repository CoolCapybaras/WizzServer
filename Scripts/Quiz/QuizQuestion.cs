using Newtonsoft.Json;
using WizzServer.Net;

namespace WizzServer
{
	public enum QuizQuestionType
	{
		Default,
		TrueOrFalse,
		Multiple,
		Input,
		Match
	}

	public class QuizQuestion
	{
		public QuizQuestionType Type { get; set; }
		public string Question { get; set; }
		public string[] Answers { get; set; }
		[JsonIgnore]
		public byte[] Image { get; set; }
		public int Time { get; set; }
		public QuizAnswer RightAnswer { get; set; }

		public void ShuffleAnswers()
		{
			if (Type == QuizQuestionType.Default || Type == QuizQuestionType.TrueOrFalse)
			{
				string answer = Answers[(int)RightAnswer.Id!];
				Answers.Shuffle();
				RightAnswer.Id = Array.IndexOf(Answers, answer);
			}
			else if (Type == QuizQuestionType.Multiple)
			{
				var answers = RightAnswer.Ids.Select((x, y) => (Answers[y], x)).ToDictionary(x => x.Item1, x => x.x);
				Answers.Shuffle();
				RightAnswer.Ids = Answers.Select(x => answers[x]).ToArray();
			}
			else if (Type == QuizQuestionType.Match)
			{
				var answers = RightAnswer.Ids.Select((x, y) => (Answers[y], Answers[x + 4])).ToDictionary(x => x.Item1, x => x.Item2);
				var subquestions = Answers[..4];
				var subanswers = Answers[4..];
				subquestions.Shuffle();
				subanswers.Shuffle();
				RightAnswer.Ids = subquestions.Select(x => (byte)Array.IndexOf(subanswers, answers[x])).ToArray();
				Answers = [.. subquestions, .. subanswers];
			}
		}

		public void Serialize(WizzStream stream, bool includeRightAnswer = false)
		{
			stream.WriteVarInt(Type);
			stream.WriteString(Question);
			stream.WriteVarInt(Answers.Length);
			for (int i = 0; i < Answers.Length; i++)
				stream.WriteString(Answers[i]);
			stream.WriteImage(Image);
			stream.WriteVarInt(Time);
			stream.WriteBoolean(includeRightAnswer);
			if (includeRightAnswer)
				RightAnswer.Serialize(stream);
		}

		public static QuizQuestion Deserialize(WizzStream stream)
		{
			var question = new QuizQuestion();
			question.Type = (QuizQuestionType)stream.ReadVarInt();
			question.Question = stream.ReadString();

			int count = stream.ReadVarInt();
			question.Answers = new string[count];
			for (int i = 0; i < count; i++)
				question.Answers[i] = stream.ReadString();

			question.Image = stream.ReadImage();
			question.Time = stream.ReadVarInt();
			if (stream.ReadBoolean())
				question.RightAnswer = QuizAnswer.Deserialize(stream);
			return question;
		}
	}
}
