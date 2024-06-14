using Net.Packets.Clientbound;
using System.Collections.Concurrent;

namespace WizzServer
{
	public class Game
	{
		private Room room;
		private Quiz quiz;
		private QuizQuestion[] questions;
		private QuizQuestion currentQuestion;
		private ConcurrentDictionary<Client, int> roundScore = new();
		private ConcurrentDictionary<int, int> globalScore;
		private TaskCompletionSource answerQuestionTask;
		private TaskCompletionSource continueTask;
		private DateTimeOffset answerStartTime;
		private int answerNeededCount;
		private int answerCount;

		public Game(Room room, Quiz quiz)
		{
			this.room = room;
			this.quiz = quiz;
		}

		public async Task Start()
		{
			try
			{
				await ProcessGame();
			}
			catch (Exception e)
			{
				Logger.LogError(e.ToString());
			}
		}

		private async Task ProcessGame()
		{
			questions = quiz.GetGameQuestions();
			globalScore = new ConcurrentDictionary<int, int>(room.Clients.Select(x => new KeyValuePair<int, int>(x.RoomId, 0)));

			await room.BroadcastAsync(new GameStartedPacket(quiz.Name, questions));
			await Task.Delay(3000);

			await room.BroadcastAsync(new TimerStartedPacket());
			await Task.Delay(3000);

			for (int i = 0; i < questions.Length; i++)
			{
				currentQuestion = questions[i];

				int delay = Math.Max((int)(currentQuestion.Question.Length * 0.075f), 3);
				await room.BroadcastAsync(new RoundStartedPacket(i, delay));
				await Task.Delay(delay * 1000);

				answerNeededCount = room.Clients.Count;
				answerQuestionTask = new TaskCompletionSource();
				await room.BroadcastAsync(new ShowQuestionPacket());
				answerStartTime = DateTimeOffset.UtcNow;

				await Task.WhenAny(Task.Delay(currentQuestion.Time * 1000), answerQuestionTask.Task);

				answerNeededCount = 0;

				foreach (var client in room.Clients)
					await client.QueuePacketAsync(new RightAnswerPacket(currentQuestion.RightAnswer, roundScore.GetValueOrDefault(client)));
				LogAnswers();

				await WaitForContinue();

				await room.BroadcastAsync(new RoundEndedPacket(globalScore));
				answerCount = 0;
				roundScore.Clear();

				if (i < questions.Length - 1)
					await WaitForContinue();

				if (room.Clients.GetCountNoLocks() == 0)
				{
					room.Destroy(true);
					return;
				}
			}

			await room.BroadcastAsync(new GameEndedPacket(globalScore));
			room.Destroy();
		}

		public void OnClientAnswer(Client client, int id)
		{
			if (answerNeededCount == 0 || roundScore.ContainsKey(client))
				return;

			int score = (int)(id == currentQuestion.RightAnswer ? 100 * (1 - (DateTimeOffset.UtcNow - answerStartTime).TotalSeconds / currentQuestion.Time) : 0);
			roundScore.TryAdd(client, score);
			globalScore[client.RoomId] += score;

			if (Interlocked.Increment(ref answerCount) == answerNeededCount)
				answerQuestionTask.TrySetResult();
		}

		public void OnClientLeave(Client client)
		{
			if (answerNeededCount == 0 || roundScore.ContainsKey(client)
				|| Interlocked.Decrement(ref answerNeededCount) != answerCount)
				return;

			answerQuestionTask.TrySetResult();
		}

		private async Task WaitForContinue()
		{
			if (room.Host != null)
			{
				continueTask = new TaskCompletionSource();
				await continueTask.Task;
			}
			else
			{
				await Task.Delay(3000);
			}
		}

		public void OnGameContinue() => continueTask?.TrySetResult();

		private void LogAnswers()
		{
			int rightAnswerCount = 0;
			int scoreSum = 0;

			foreach (var client in roundScore)
			{
				if (client.Value > 0)
					rightAnswerCount++;
				scoreSum += client.Value;
			}

			Logger.LogInfo($"Room #{room.Id} answered {rightAnswerCount}/{answerCount} with sum score {scoreSum}");
		}
	}
}
