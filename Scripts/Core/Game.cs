using Net.Packets.Clientbound;

namespace WizzServer
{
	public class Game
	{
		private Room room;
		private GameQuiz quiz;
		private QuizQuestion currentQuestion;
		private Dictionary<int, int> globalScore;
		private DateTime answerStartTime;
		private bool isAnswerAllowed;
		private int answers;
		private bool isStarted;
		private bool isEnded;

		public bool IsStarted { get { return isStarted; } }
		public bool IsEnded { get { return isEnded; } }

		public Game(Room room, Quiz quiz)
		{
			this.room = room;
			this.quiz = new GameQuiz(quiz, room.Id);
		}

		public async Task Start()
		{
			try
			{
				await StartGame();
			}
			catch (Exception ex)
			{
				Logger.LogError(ex.ToString());
			}
		}

		private async Task StartGame()
		{
			isStarted = true;
			globalScore = room.Clients.ToDictionary(x => x.Key, x => 0);
			//quiz.CreateCache();
			room.Broadcast(new GameStartedPacket(quiz.Name));

			await Task.Delay(3000);

			room.Broadcast(new TimerStartedPacket());

			await Task.Delay(3000);

			while (quiz.Questions.Count > 0)
			{
				currentQuestion = quiz.Questions.Dequeue();
				room.Broadcast(new RoundStartedPacket(currentQuestion));

				await Task.Delay(currentQuestion.Countdown * 1000);

				isAnswerAllowed = true;
				answerStartTime = DateTime.Now;

				for (int i = 0; i < currentQuestion.Time; i++)
				{
					if (answers == room.Clients.Count)
						break;

					await Task.Delay(1000);
				}

				isAnswerAllowed = false;

				room.Broadcast(new RightAnswerPacket(currentQuestion.RightAnswer));
				await Task.Delay(3000);

				room.Broadcast(new RoundEndedPacket(globalScore));
				answers = 0;

				await Task.Delay(5000);
			}

			room.Broadcast(new GameEndedPacket(globalScore));
			//quiz.DeleteCache();
			isEnded = true;
		}

		public void OnAnswer(Client client, int id)
		{
			if (!isAnswerAllowed) return;
			globalScore[client.Id] += (int)(id == currentQuestion.RightAnswer ? ((currentQuestion.Time * 1000) - (DateTime.Now - answerStartTime).TotalMilliseconds) / 300 : 0);
			answers++;
		}

		public void DeleteCache()
		{
			quiz.DeleteCache();
		}
	}
}
