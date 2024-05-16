using System.Numerics;

namespace WizzServer
{
	public static class Extensions
	{
		public static void Shuffle<T>(this T[] array)
		{
			int n = array.Length;
			while (n > 1)
			{
				int k = Random.Shared.Next(n--);
				T temp = array[n];
				array[n] = array[k];
				array[k] = temp;
			}
		}

		public static int GetVarIntLength(this int value)
		{
			return (BitOperations.LeadingZeroCount((uint)value | 1) - 38) * -1171 >> 13;
		}
	}
}
