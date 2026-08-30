using System;
using System.Collections.Generic;

namespace Network.Sending;

internal static class SortedMerge
{
	public static void Into<T>(List<T> destination, List<T> left, List<T> right, Comparison<T> compare)
	{
		int leftIndex = 0, rightIndex = 0, destinationIndex = 0;
		while (leftIndex < left.Count && rightIndex < right.Count)
		{
			destination[destinationIndex++] = compare(left[leftIndex], right[rightIndex]) <= 0 ? left[leftIndex++] : right[rightIndex++];
		}

		while (leftIndex < left.Count)
		{
			destination[destinationIndex++] = left[leftIndex++];
		}

		while (rightIndex < right.Count)
		{
			destination[destinationIndex++] = right[rightIndex++];
		}
	}
}
