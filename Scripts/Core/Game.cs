﻿using Net.Packets.Clientbound;
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
		private DateTimeOffset answerStartTime;
		private bool isAnswerAllowed;
		private int answerCount;
		private bool continueGame;

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

			room.Broadcast(new GameStartedPacket(quiz.Name, questions));
			await Task.Delay(3000);

			room.Broadcast(new TimerStartedPacket());
			await Task.Delay(3000);

			for (int i = 0; i < questions.Length; i++)
			{
				currentQuestion = questions[i];

				int delay = CalculateDelay();
				room.Broadcast(new RoundStartedPacket(i, delay));
				await Task.Delay(delay);

				room.Broadcast(new ShowQuestionPacket());
				isAnswerAllowed = true;
				answerStartTime = DateTimeOffset.UtcNow;

				for (int q = 0; q < currentQuestion.Time; q++)
				{
					if (answerCount >= room.Clients.GetCountNoLocks())
						break;

					await Task.Delay(1000);
				}

				isAnswerAllowed = false;

				foreach (var client in room.Clients)
					client.SendPacket(new RightAnswerPacket(currentQuestion.RightAnswer, roundScore.GetValueOrDefault(client)));
				LogAnswers();

				await WaitForContinue();

				room.Broadcast(new RoundEndedPacket(globalScore));
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

			room.Broadcast(new GameEndedPacket(globalScore));
			room.Destroy();
		}

		public void OnClientAnswer(Client client, int id)
		{
			if (!isAnswerAllowed || roundScore.ContainsKey(client))
				return;

			Interlocked.Increment(ref answerCount);

			int score = (int)(id == currentQuestion.RightAnswer ? 100 * (1 - (DateTimeOffset.UtcNow - answerStartTime).TotalSeconds / currentQuestion.Time) : 0);
			roundScore.TryAdd(client, score);
			globalScore[client.RoomId] += score;
		}

		private async Task WaitForContinue()
		{
			continueGame = false;
			while (!continueGame)
			{
				if (room.Host != null)
				{
					await Task.Delay(1000);
					continue;
				}
				else
				{
					await Task.Delay(3000);
					break;
				}
			}
		}

		public void OnGameContinue() => continueGame = true;

		private int CalculateDelay()
		{
			return Math.Max((int)(currentQuestion.Question.Length * 0.075f), 3) * 1000;
		}

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
