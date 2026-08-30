using System;
using System.Collections.Generic;

namespace Network.Sending;

internal static class SortedMerge
{
	public static void Into<T>(List<T> destination, List<T> left, List<T> right, Comparison<T> compare)
	{
		int a = 0, b = 0, i = 0;
		while (a < left.Count && b < right.Count)
		{
			destination[i++] = compare(left[a], right[b]) <= 0 ? left[a++] : right[b++];
		}

		while (a < left.Count)
		{
			destination[i++] = left[a++];
		}

		while (b < right.Count)
		{
			destination[i++] = right[b++];
		}
	}
}
