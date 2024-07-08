using WizzServer.Net;

namespace WizzServer
{
	public class QuizAnswer
	{
		public QuizQuestionType? Type { get; set; }
		public int? Id { get; set; }
		public byte[] Ids { get; set; }
		public string Input { get; set; }

		public void Serialize(WizzStream stream)
		{
			stream.WriteVarInt(Type);
			if (Type == QuizQuestionType.Default
				|| Type == QuizQuestionType.TrueOrFalse)
				stream.WriteVarInt((int)Id);
			else if (Type == QuizQuestionType.Multiple
				|| Type == QuizQuestionType.Match)
				stream.WriteByteArray(Ids);
			else
				stream.WriteString(Input);
		}

		public static QuizAnswer Deserialize(WizzStream stream)
		{
			var answer = new QuizAnswer();
			answer.Type = (QuizQuestionType)stream.ReadVarInt();
			if (answer.Type == QuizQuestionType.Default
				|| answer.Type == QuizQuestionType.TrueOrFalse)
				answer.Id = stream.ReadVarInt();
			else if (answer.Type == QuizQuestionType.Multiple
				|| answer.Type == QuizQuestionType.Match)
				answer.Ids = stream.ReadByteArray(4);
			else
				answer.Input = stream.ReadString();
			return answer;
		}

		public override bool Equals(object? obj)
		{
			if (obj is not QuizAnswer answer || answer.Type != Type)
				return false;

			if (Type == QuizQuestionType.Default || Type == QuizQuestionType.TrueOrFalse)
				return Id == answer.Id;
			else if (Type == QuizQuestionType.Multiple || Type == QuizQuestionType.Match)
			{
				for (int i = 0; i < 4; i++)
				{
					if (Ids[i] != answer.Ids[i])
						return false;
				}
				return true;
			}
			else
				return Input == answer.Input;
		}
	}
}
