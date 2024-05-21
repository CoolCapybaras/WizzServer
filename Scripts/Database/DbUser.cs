namespace WizzServer.Database
{
	public class DbUser
	{
		public int Id { get; set; }
		public string Username { get; set; }
		public long Realname { get; set; }
		public string Token { get; set; }
		public string Ip { get; set; }
		public DateTimeOffset Lastlogin { get; set; }
		public DateTimeOffset Regdate { get; set; }
		public string Regip { get; set; }
		public bool IsVk { get; set; }
	}
}
