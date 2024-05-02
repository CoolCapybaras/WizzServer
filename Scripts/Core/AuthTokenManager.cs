using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace WizzServer
{
	public class AuthToken
	{
		public Client Client { get; set; }
		public string Token { get; set; }
		public DateTimeOffset ExpirationTime { get; set; }

		public AuthToken(Client client)
		{
			this.Client = client;
			this.Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(6));
			this.ExpirationTime = DateTimeOffset.Now.AddSeconds(60);
		}
	}

	public class AuthTokenManager
	{
		private ConcurrentDictionary<string, AuthToken> tokens = [];
		private ConcurrentDictionary<Client, AuthToken> clientToToken = [];

		public string CreateToken(Client client)
		{
			RemoveToken(client);

			var token = new AuthToken(client);
			tokens.TryAdd(token.Token, token);
			clientToToken.TryAdd(client, token);
			return token.Token;
		}

		public bool TryGetToken(string token, [MaybeNullWhen(false)] out AuthToken result)
		{
			return tokens.TryGetValue(token, out result);
		}

		public void RemoveToken(string token)
		{
			if (tokens.TryRemove(token, out var _token))
				clientToToken.TryRemove(_token.Client, out _);
		}

		public void RemoveToken(Client client)
		{
			if (clientToToken.TryRemove(client, out var _token))
				tokens.TryRemove(_token.Token, out _);
		}

		public static string GenerateToken()
		{
			return Convert.ToBase64String(RandomNumberGenerator.GetBytes(30));
		}
	}
}
