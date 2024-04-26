namespace WizzServer
{
	public class AuthToken
	{
		public string Token { get; set; }
		public DateTimeOffset ExpirationTime { get; set; }
	}
}
