using OpenCvSharp;
using System;

namespace MyCVSharp
{
	public class Const
	{
		// CvPoint
		static public readonly CvPoint PointZero = new CvPoint( 0, 0 );

		// CvScalar
		static public readonly CvScalar ScalarWhite =	new CvScalar( 255, 255, 255 );
		static public readonly CvScalar ScalarBlack =	new CvScalar( 0, 0, 0 );
		static public readonly CvScalar ScalarRed =		new CvScalar( 255, 0, 0 );
		static public readonly CvScalar ScalarGreen =	new CvScalar( 0, 255, 0 );
		static public readonly CvScalar ScalarBlue =	new CvScalar( 0, 0, 255 );
		static public readonly CvScalar ScalarMagenta =	new CvScalar( 0, 255, 255 );

		// not a const, but doesn't feel bad to put in here
		static private Random Rnd = new Random();
		static public CvScalar ScalarRandom() { return new CvScalar( Rnd.Next( 256 ), Rnd.Next( 256 ), Rnd.Next( 256 ), Rnd.Next( 256 ) ); }
	}
}
