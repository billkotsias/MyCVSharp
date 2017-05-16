using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenCvSharp;

namespace MyCVSharp
{
	public struct ContourData
	{
		public CvPoint[][] contours;
		public double[] areas;
	}

	static public partial class Filters
	{
		// The OpenCVSharp is for some reason complicated and needs to be abstracted. So here is the abstraction layer...
		// TODO : The CV docs specifically state that the image should be in binary format. Check if it is.
		// see usage here: http://stackoverflow.com/questions/35418714/opencvsharps-findcontour-returns-wrong-data
		static public ContourData FindContours(CvMat input, ContourRetrieval retrievalMode, ContourChain chainMode, CvPoint offset, double minArea = 0)
		{
			List<CvPoint[]> pointsArrays = new List<CvPoint[]>();
			List<double> areas = new List<double>();

			CvSeq<CvPoint> contoursRaw;
			using (CvMemStorage storage = new CvMemStorage())
			{
				Cv.FindContours( input, storage, out contoursRaw, CvContour.SizeOf, retrievalMode, chainMode, offset );
				using (CvContourScanner scanner = new CvContourScanner( input, storage, CvContour.SizeOf, retrievalMode, chainMode, offset ))
				{
					foreach (CvSeq<CvPoint> c in scanner)
					{
						List<CvPoint> points = new List<CvPoint>();

						//Some contours have negative area!
						double area = c.ContourArea();
						if (Math.Abs(area) >= minArea)
						{
							areas.Add( area );
							foreach (CvPoint p in c.ToArray())
								points.Add( p );

							pointsArrays.Add( points.ToArray() );
						}
					}
				}
			}

			ContourData data = new ContourData();
			data.contours = pointsArrays.ToArray();
			data.areas = areas.ToArray();

			return data;
		}
	}
}
