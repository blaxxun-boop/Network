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
			if (compare(left[leftIndex], right[rightIndex]) <= 0)
			{
				destination[destinationIndex] = left[leftIndex];
				++leftIndex;
			}
			else
			{
				destination[destinationIndex] = right[rightIndex];
				++rightIndex;
			}

			++destinationIndex;
		}

		while (leftIndex < left.Count)
		{
			destination[destinationIndex] = left[leftIndex];
			++destinationIndex;
			++leftIndex;
		}

		while (rightIndex < right.Count)
		{
			destination[destinationIndex] = right[rightIndex];
			++destinationIndex;
			++rightIndex;
		}
	}
}
