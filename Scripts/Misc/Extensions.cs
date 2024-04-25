using Newtonsoft.Json.Linq;
using System.Numerics;

namespace WizzServer
{
	public static class Extensions
	{
		public static int GetVarIntLength(this int value)
		{
			return (BitOperations.LeadingZeroCount((uint)value | 1) - 38) * -1171 >> 13;
		}
	}
}
