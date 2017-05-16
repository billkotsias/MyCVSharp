using System;
using System.Collections.Generic;
using OpenCvSharp.CPlusPlus;
using OpenCvSharp;

namespace MyCVSharp
{
	static public class PointOps
	{
		static public double Norm(CvPoint pt)
		{
			return Math.Sqrt( (double)pt.X * pt.X + (double)pt.Y * pt.Y );
		}

		static public double LineDistance( CvPoint pt1, CvPoint pt2, CvPoint pt0)
		{
			int difX = pt2.X - pt1.X;
			int difY = pt2.Y - pt1.Y;
			return Math.Abs( difY * pt0.X - difX * pt0.Y + pt2.X * pt1.Y - pt2.Y * pt1.X ) /
				   Math.Sqrt( difY * difY + difX * difX );
		}

		static public bool LineIntersection( CvPoint A1, CvPoint A2, CvPoint B1, CvPoint B2, out CvPoint intersection )
		{
			intersection = new CvPoint(); // in case of false

			CvPoint a = (A2 - A1);
			CvPoint b = (B2 - B1);
			double f = (a.Y * b.X) - (a.X * b.Y);
			if (f == 0)      // lines are parallel
				return false;

			CvPoint c = (B2 - A2);
			double aa = (a.Y * c.X) - (a.X * c.Y);
			double bb = (b.Y * c.X) - (b.X * c.Y);
			// NOTE : nice optimization!
			if (f < 0)
			{
				if (aa > 0) return false;
				if (bb > 0) return false;
				if (aa < f) return false;
				if (bb < f) return false;
			}
			else
			{
				if (aa < 0) return false;
				if (bb < 0) return false;
				if (aa > f) return false;
				if (bb > f) return false;
			}
			double out_ = 1.0 - (aa / f);
			intersection = ((B2 - B1) * out_) + B1;

			return true;
		}

		// ============ copying

		// =>	source = generic "CvPoint" container
		//		dest = Point array. WARNING : must be: dest size >= source size !!!
		static public void CopyCvPointsToPoints( IEnumerable<CvPoint> source, Point[] dest )
		{
			IEnumerator<CvPoint> it = source.GetEnumerator();
			int index = 0;
			while (it.MoveNext())
				dest[index++] = it.Current;
		}

		// =>	source = generic "Point" container
		//		dest = CvPoint array. WARNING : must be: dest size >= source size !!!
		static public void CopyPointsToCvPoints( IEnumerable<Point> source, CvPoint[] dest )
		{
			IEnumerator<Point> it = source.GetEnumerator();
			int index = 0;
			while (it.MoveNext())
				dest[index++] = it.Current;
		}

		// Copy abstract array of "CvPoints" to "List" of abstract class that takes 2 int constructor parameters (this class should
		// probably represent a 2D point)
		// this ain't C++ so it is a tad slow, but much better than having cluttered code all over the place
		// =>	source = generic "CvPoint" container
		//		dest = array to copy "CvPoint"s into. WARNING : dest size >= source size !!!
		static public void CopyCvPointsToGenericPointsArray<ClassDest>( IEnumerable<CvPoint> source, ClassDest[] dest )
		{
			IEnumerator<CvPoint> it = source.GetEnumerator();
			int index = 0;
			while (it.MoveNext())
			{
				CvPoint current = it.Current;
				dest[index++] = ((ClassDest)Activator.CreateInstance( typeof( ClassDest ), new object[] { current.X, current.Y } ));
			}
		}

		static public void CopyPointsToGenericPointsArray<ClassDest>( IEnumerable<Point> source, ClassDest[] dest )
		{
			IEnumerator<Point> it = source.GetEnumerator();
			int index = 0;
			while (it.MoveNext())
			{
				Point current = it.Current;
				dest[index++] = ((ClassDest)Activator.CreateInstance( typeof( ClassDest ), new object[] { current.X, current.Y } ));
			}
		}

		// unsuccessful attemp at making a generic PointX to PointY copy
		// this ain't C++ so it would be slow, anyway
		//static public void CopyPoints<CSrc, CDest>( IEnumerable<CSrc> source, List<CDest> dest ) where CDest : class, new()
		//{
		//	IEnumerator<CSrc> it = source.GetEnumerator();
		//	while (it.MoveNext())
		//	{
		//		CSrc current = it.Current;
		//		dest.Add( (CDest)Activator.CreateInstance( typeof( CDest ), new object[] { current.X, current.Y } );
		//	}
		//}
	}
}
