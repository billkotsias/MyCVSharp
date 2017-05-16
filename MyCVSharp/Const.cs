using OpenCvSharp;

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
		static public readonly CvScalar ScalarPurple =	new CvScalar( 0, 255, 255 );
	}
}
