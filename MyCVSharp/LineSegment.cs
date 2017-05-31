using System;
using OpenCvSharp;

namespace MyCVSharp
{
	// https://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line
	// https://en.wikipedia.org/wiki/Parametric_equation
	// https://en.wikipedia.org/wiki/Line_(geometry)

	class LineSegment
	{
		// Linear equation
		// A*x + B*y + C = 0
		private float A = float.NaN;
		private float B = float.NaN;
		private float C = float.NaN;

		// Parametric representation
		// x = x1 + B * t
		// y = y1 + A * t
		// where cvline.P1 = {x1,y1}, cvline.P2 = {x2,y2}, B = x2 - x1, A = y2 - y1,
		// and for t = 0 -> {x,y} = {x1,y1}, t = 1 -> {x,y} = {x2,y2}
		private CvLineSegmentPoint cvline;

		// cache common operations
		private float INV_A2_P_B2; // inverted segment length
		private float A_DIV;
		private float B_DIV;
		private float AC_DIV;
		private float BC_DIV;
		private bool A_GT_B;

		//public LineSegment() { }

		public LineSegment(CvLineSegmentPoint cvline)
		{
			this.cvline = cvline;
			CvPoint p1 = cvline.P1;
			CvPoint p2 = cvline.P2;
			A = p2.Y - p1.Y;
			B = p2.X - p1.X;
			C = p2.X * p1.Y - p2.Y * p1.X;
			// cache
			A_GT_B = Math.Abs( A ) > Math.Abs( B );
			INV_A2_P_B2 = 1 / (A * A + B * B);
			A_DIV = A * INV_A2_P_B2;
			B_DIV = B * INV_A2_P_B2;
			AC_DIV = C * A_DIV;
			BC_DIV = C * B_DIV;
		}

		public CvPoint getParametricPoint(float t)
		{
			// x = x1 + B * t
			// y = y1 + A * t
			return new CvPoint( (int)Math.Round( cvline.P1.X + B * t ), (int)Math.Round( cvline.P1.Y + A * t ) );
		}

		public float squaredDistanceFrom(CvPoint pnt)
		{
			float top = A * pnt.X + B * pnt.Y + C;
			return top * top * INV_A2_P_B2;
		}

		public CvPoint getProjectionOf( CvPoint pnt )
		{
			float pntFactor = B * pnt.X - A * pnt.Y;
			return new CvPoint( (int)Math.Round( B_DIV * pntFactor - AC_DIV ), (int)Math.Round( -A_DIV * pntFactor - BC_DIV ) );
		}

		// calculate projection of a point if it is close enough to the segment
		// NOTE :	accurate but a little slow method. Can me made faster by NOT getting projection, but rather using pnt.X or Y
		//			directly to get parameter t. Use variable according to which is larger, |A| or |B| ! But in that case, the
		//			threshold should be quite small!
		// => distanceSquaredCutoff = reject point altogether if it has SQUARED distance greater than this
		// <= NaN if not close enough
		public float getParametricProjectionOf(CvPoint pnt, float distanceSquaredCutoff)
		{
			if (squaredDistanceFrom( pnt ) > distanceSquaredCutoff) return float.NaN;

			// point is close enough to line (not segment!)
			CvPoint proj = getProjectionOf( pnt );
			// x = x1 + B * t
			// y = y1 + A * t
			float t;
			if (A_GT_B)
				t = (proj.Y - cvline.P1.Y) / A;
			else
				t = (proj.X - cvline.P1.X) / B;
			return t;
		}

		// NOTE : no need to pass bigger line in "lineBig" parameter, function takes care of that
		static public LineSegment Merge( LineSegment lineBig, LineSegment lineSmall, float distanceSquaredThreshold )
		{
			// - both "other" points must be close enough to "this"
			// - new line will be extended to contain all 4 points
			if (lineBig.INV_A2_P_B2 > lineSmall.INV_A2_P_B2)
				Utils.Swap( ref lineBig, ref lineSmall );

			// project small line on big line
			float t1 = lineBig.getParametricProjectionOf( lineSmall.cvline.P1, distanceSquaredThreshold );
			if (float.IsNaN( t1 )) return null;
			float t2 = lineBig.getParametricProjectionOf( lineSmall.cvline.P2, distanceSquaredThreshold );
			if (float.IsNaN( t2 )) return null;
			// sort parameters
			if (t1 > t2) Utils.Swap( ref t1, ref t2 ); // now t1 < t2

			// create new line which is equal or bigger to "lineBig"
			CvPoint p1, p2;
			p1 = (t1 >= 0) ? lineBig.cvline.P1 : lineBig.getParametricPoint( t1 );
			p2 = (t2 <= 1) ? lineBig.cvline.P2 : lineBig.getParametricPoint( t2 );

			return new LineSegment( new CvLineSegmentPoint(p1, p2) );
		}
	}
}
