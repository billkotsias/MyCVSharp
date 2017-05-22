using System;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
using MyCVSharp;
using System.Diagnostics;
using System.Collections;

class MainClass
{
	static void Main()
	{
		//CvMat test = new CvMat( "box1.png", LoadMode.Color );
		CvMat test = new CvMat( "box7.png", LoadMode.Color );
		MatOps.NewWindowShow( test, "ORIGINAL" );
		FindTheBox findit = new FindTheBox(260,100);
		//CvMat test = new CvMat( "f1.png", LoadMode.Color );
		//FindTheBox findit = new FindTheBox(400,400);

		findit.calcNextFrame( test );
		Cv.WaitKey();

		return;

		MyCVSharpTEST.Test t = new MyCVSharpTEST.Test();
		return;

		Stopwatch w = new Stopwatch();
		w.Start();
		CvMat normalized = MatOps.MyNormalize( test );
		w.Stop();
		Console.Out.WriteLine( "MyNorm = " + w.ElapsedMilliseconds);
		w.Reset();
		MatOps.NewWindowShow( normalized );

		w.Start();
		CvMat myhue = MatOps.BGRtoHue( test );
		w.Stop();
		Console.Out.WriteLine( "MyHue = " + w.ElapsedMilliseconds );
		w.Reset();
		MatOps.NewWindowShow( myhue, "MyHue" );

		w.Start();
		CvMat hsl = MatOps.ConvertChannels( test, MatrixType.U8C3, ColorConversion.BgrToHsv_Full );
		CvMat hue = MatOps.CopySize( test, MatrixType.U8C1 );
		CvMat lum = hue.EmptyClone();
		hsl.Split( hue, null, lum, null );
		w.Stop();
		Console.Out.WriteLine( "OpenCV = " + w.ElapsedMilliseconds );
		w.Reset();
		MatOps.NewWindowShow( hue );
		MatOps.NewWindowShow( lum );

		Cv.WaitKey();
	}
}

namespace MyCVSharpTEST
{
	public class Test
	{
		public Test()
		{
			RectPoints rect = new RectPoints();

			CvMat src;
			
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			ArrayList boxExamples = new ArrayList() { /*"test.png", */1, 2, 3};
			while (boxExamples.Count > 0)
			{
				string imageName = boxExamples[0].ToString();
				if (imageName.Length == 1) imageName = "box" + imageName + ".png";
				src = new CvMat( imageName, LoadMode.Color );
				boxExamples.RemoveAt( 0 );
				float[] points = rect.calcNextFrame( src );
				Console.Out.WriteLine( "Processing " + imageName );
				//ShowPoints( points );
				//MatOps.NewWindowShow( src );
			}
			stopwatch.Stop();
			Console.Out.WriteLine( "time taken = " + stopwatch.ElapsedMilliseconds );
			Cv.WaitKey();
			return;

			//

			ContourData data = Filters.FindContours( src, ContourRetrieval.Tree, ContourChain.ApproxSimple, Const.PointZero, 0000 );
			Console.Out.WriteLine( data.contours.Length );
			for (int i = 0; i < data.areas.Length; ++i)
			{
				Console.Out.WriteLine( i.ToString() + "=" + data.areas[i] );
			}
			for (int j = 0; j < data.contours.Length; ++j)
			{
				Console.Out.WriteLine( "--- Contour " + j +"---");
				for (int i = 0; i < data.contours[j].Length; ++i)
				{
					Console.Out.WriteLine( i.ToString() + " " + data.contours[j][i] );
				}
			}
		}

		static public void ShowPoints(float[] pts)
		{
			for (int i = 0; i < pts.Length; i+=2)
				Console.Out.WriteLine( "point " + (i>>1) + ":" + pts[i] + "," + pts[i+1]);
		}
	}
}
