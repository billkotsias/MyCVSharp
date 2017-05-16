using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyCVSharp
{
	static public class Utils
	{
		static public T[] Populate<T>( this T[] arr, T value )
		{
			for (int i = arr.Length - 1; i >= 0; --i)
				arr[i] = value;

			return arr;
		}

		// <= -1 if not found
		static public int FindIndex<T>(IEnumerable<T> arr, T value)
		{
			IEnumerator<T> it = arr.GetEnumerator();
			int index = 0;
			while (it.MoveNext())
			{
				if (value.Equals(it.Current)) return index;
				++index;
			}
			return -1;
		}

		static public int FindMaxIndex<T>( this IEnumerable<T> source ) where T : IComparable<T>
		{
			using (var e = source.GetEnumerator())
			{
				if (!e.MoveNext()) return -1;
				T maxValue = e.Current;
				int maxIndex = 0;
				for (int i = 1; e.MoveNext(); ++i)
				{
					if (maxValue.CompareTo( e.Current ) < 0)
					{
						maxIndex = i;
						maxValue = e.Current;
					}
				}
				return maxIndex;
			}
		}

		static public int FindMinIndex<T>( this IEnumerable<T> source ) where T : IComparable<T>
		{
			using (var e = source.GetEnumerator())
			{
				if (!e.MoveNext()) return -1;
				T minValue = e.Current;
				int minIndex = 0;
				for (int i = 1; e.MoveNext(); ++i)
				{
					if (minValue.CompareTo( e.Current ) > 0)
					{
						minIndex = i;
						minValue = e.Current;
					}
				}
				return minIndex;
			}
		}

		// only works with weird type casting; this just isn't C++
		static public int FindMaxIndex<T>( IEnumerable<IComparable<T>> arr )
		{
			IEnumerator<IComparable<T>> it = arr.GetEnumerator();
			if (!it.MoveNext()) return -1;
			int index = 1, maxIndex = 0;
			IComparable<T> max = it.Current;
			while (it.MoveNext())
			{
				if (max.CompareTo( (T)(it.Current) ) < 0)
				{
					maxIndex = index;
					max = it.Current;
				}
				++index;
			}
			return maxIndex;
		}

		static public int FindMaxIndex( IEnumerable<double> arr )
		{
			IEnumerator<double> it = arr.GetEnumerator();
			if (!it.MoveNext()) return -1;
			int index = 1, maxIndex = 0;
			double max = it.Current;
			while (it.MoveNext())
			{
				if (max < it.Current)
				{
					maxIndex = index;
					max = it.Current;
				}
				++index;
			}
			return maxIndex;
		}
	}
}
