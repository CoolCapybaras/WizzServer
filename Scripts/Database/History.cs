namespace WizzServer.Database
{
	public class History
	{
		public int Id { get; set; }
		public int UserId { get; set; }
		public int QuizId { get; set; }
		public Quiz Quiz { get; set; }
		public DateTimeOffset CompleteDate { get; set; }
	}
}
