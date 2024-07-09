using Microsoft.EntityFrameworkCore;
using WizzServer.Database;
using WizzServer.Utilities.Collections;

namespace WizzServer.Services
{
	public class DbUpdateService
	{
		private CancellationToken cancellationToken;
		private ConcurrentHashSet<int> quizzesToUpdate = [];

		public DbUpdateService(CancellationToken cancellationToken)
		{
			this.cancellationToken = cancellationToken;
		}

		public async Task Start()
		{
			try
			{
				await ProcessService();
			}
			catch (Exception e)
			{
				Logger.LogError(e.ToString());
			}
		}

		public async Task ProcessService()
		{
			Logger.LogInfo("Database update service started");

			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					await Task.Delay(10000, cancellationToken);
				}
				catch (OperationCanceledException) { }

				if (quizzesToUpdate.IsEmpty)
					continue;

				using var db = new ApplicationDbContext();
				await db.Quizzes.Where(x => quizzesToUpdate.Contains(x.Id)).ExecuteUpdateAsync(x => x.SetProperty(y => y.Score, y => db.Ratings.Where(z => z.QuizId == y.Id).Average(z => z.Score)));

				quizzesToUpdate.Clear();
			}

			Logger.LogInfo("Shutting down database update service...");
		}

		public void AddToUpdate(int quizId)
		{
			quizzesToUpdate.Add(quizId);
		}
	}
}
