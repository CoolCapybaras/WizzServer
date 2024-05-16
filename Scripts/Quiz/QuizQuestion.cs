using Newtonsoft.Json;
using WizzServer.Net;

namespace WizzServer
{
	public enum QuizQuestionType
	{
		Default,
		TrueOrFalse
	}

	public class QuizQuestion
	{
		public QuizQuestionType Type { get; set; }
		public string Question { get; set; }
		public string[] Answers { get; set; }
		[JsonIgnore]
		public byte[] Image { get; set; }
		public int Time { get; set; }
		[JsonIgnore]
		public int RightAnswer { get; set; }

		public void ShuffleAnswers()
		{
			string answer = Answers[RightAnswer];
			Answers.Shuffle();
			RightAnswer = Array.IndexOf(Answers, answer);
		}

		public void Serialize(WizzStream stream)
		{
			stream.WriteVarInt(Type);
			stream.WriteString(Question);
			stream.WriteVarInt(Answers.Length);
			for (int i = 0; i < Answers.Length; i++)
				stream.WriteString(Answers[i]);
			stream.WriteImage(Image);
			stream.WriteVarInt(Time);
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
			return question;
		}
	}
}
