﻿using System.ComponentModel.DataAnnotations.Schema;
using WizzServer.Net;

namespace WizzServer
{
	public class Quiz
	{
		public int Id { get; set; }
		public string Name { get; set; }
		[NotMapped]
		public byte[] Image { get; set; }
		public string Description { get; set; }
		public int QuestionCount { get; set; }
		public int AuthorId { get; set; }
		public bool IsShown { get; set; }
		public bool IsModerating { get; set; }
		[NotMapped]
		public QuizQuestion[] Questions { get; set; }
		public int ReferenceCount;

		public QuizQuestion[] GetGameQuestions()
		{
			QuizQuestion[] questions = (QuizQuestion[])Questions.Clone();
			foreach (var question in questions)
				question.ShuffleAnswers();
			questions.Shuffle();
			return questions;
		}

		public void Serialize(WizzStream stream, bool ignoreQuestions = true)
		{
			if (Id == 0)
			{
				stream.WriteByte(0);
				return;
			}

			stream.WriteVarInt(Id);
			stream.WriteString(Name);
			stream.WriteImage(Image);
			stream.WriteString(Description);
			stream.WriteVarInt(QuestionCount);
			stream.WriteVarInt(AuthorId);

			if (ignoreQuestions)
			{
				stream.WriteByte(0);
			}
			else
			{
				stream.WriteVarInt(Questions.Length);
				for (int i = 0; i < Questions.Length; i++)
					Questions[i].Serialize(stream);
			}
		}

		public static Quiz Deserialize(WizzStream stream)
		{
			int quizId = stream.ReadVarInt();
			if (quizId == 0)
				return null!;

			var quiz = new Quiz();
			quiz.Id = quizId;
			quiz.Name = stream.ReadString();
			quiz.Image = stream.ReadImage();
			quiz.Description = stream.ReadString();
			quiz.QuestionCount = stream.ReadVarInt();
			quiz.AuthorId = stream.ReadVarInt();

			int count = stream.ReadVarInt();
			quiz.Questions = new QuizQuestion[count];
			for (int i = 0; i < count; i++)
				quiz.Questions[i] = QuizQuestion.Deserialize(stream);

			return quiz;
		}
	}
}
