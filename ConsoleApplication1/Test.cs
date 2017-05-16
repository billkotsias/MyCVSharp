using System;
using OpenCvSharp;
using MyCVSharp;
using System.Diagnostics;

class MainClass
{
	static void Main()
	{
		MyCVSharpTEST.Test t = new MyCVSharpTEST.Test();
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
			for (int i = 1; i <= 8; ++i)
			{
				src = new CvMat( "bbox"+i+".jpg", LoadMode.Color );
				float[] points = rect.calcNextFrame( src );
				ShowPoints( points );
				UUtils.NewWindowShow( src );
			}
			stopwatch.Stop();
			Console.Out.WriteLine( "time taken = " + stopwatch.ElapsedMilliseconds );
			Cv.WaitKey();
			return;

			src = new CvMat( "bbox4.jpg", LoadMode.Color );
			for (int i = 0; i < 40; ++i)
			{
				float[] points = rect.calcNextFrame( src );
				ShowPoints( points );
				UUtils.NewWindowShow( src );
				Cv.WaitKey();
			}
			return;

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

			//CvMat dst = new CvMat( src.Rows, src.Cols, src.ElemType );
			//Cv.Canny( src, dst, 50, 200 ); //src.size == dst.size && src.depth() == CV_8U && dst.type() == CV_8U;
			//dst = MatOps.Convert( src, MatrixType.F32C1, 1.0 / 255.0 );
			//Utils.NewWindowShow( dst );
			//src = Filters.IBO( src );
			//src = MatOps.Convert( src, MatrixType.U8C1, 255 );
			//Utils.NewWindowShow( src );
			//Filters.ContrastEnhancement( src );
			//Utils.NewWindowShow( src );
			//Utils.NewWindowShow( dst );
			Console.Out.WriteLine( "NeighborhoodValleyEmphasis=" + Filters.NeighborhoodValleyEmphasis(src) );

			Cv.WaitKey();
			//       using (	Window dummy = new Window( "src image", src ),
			//dummy2 = new Window( "dst image", dst ))
			//       {
			//           Cv2.WaitKey();
			//       }
		}

		static public void ShowPoints(float[] pts)
		{
			for (int i = 0; i < pts.Length; i+=2)
				Console.Out.WriteLine( "point " + (i>>1) + ":" + pts[i] + "," + pts[i+1]);
		}
	}
}
