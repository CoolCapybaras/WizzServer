namespace WizzServer.Database
{
	public class Rating
	{
		public int Id { get; set; }
		public int UserId { get; set; }
		public int QuizId { get; set; }
		public float Score { get; set; }
	}
}
