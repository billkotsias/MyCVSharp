using OpenCvSharp;
using OpenCvSharp.CPlusPlus;

namespace MyCVSharp
{
	static public partial class Filters
	{
		static public CvPoint[] DistillContours(
			CvMat inputMat_grey, int maxContourPoints, CvPoint offset,
			ContourRetrieval retr = ContourRetrieval.External, ContourChain chain = ContourChain.ApproxSimple )
		{
			// maxContourPoints (original name "maxContours"); 5 or 100??!!!

			// TODO : The CV docs specifically state that the image should be in binary format. Check if it is.
			//std::vector<std::vector<cv::Point>> updateContours;
			//std::vector<cv::Vec4i> m_hierarchy; // TODO : Not used currently, but determine if it's gonna be needed for points-tracking
			// see usage here: http://stackoverflow.com/questions/35418714/opencvsharps-findcontour-returns-wrong-data
			ContourData contoursData = Filters.FindContours( inputMat_grey, retr, chain, offset );
			CvPoint[][] contoursFound = contoursData.contours; // original name: "updateContours"
			if (contoursFound.Length == 0)
				return null; // TODO : cannot process frame, no contours found. Maybe it's time to rethink about that strategy and not simply
							 // return empty handed!!!

			double[] contourAreas = contoursData.areas;
			Point[] newPtV;
			// find index of max-area contour
			int index = 0;
			double maxArea = 0;
			for (int i = contourAreas.Length - 1; i >= 0; --i)
			{
				double area = contourAreas[i];
				if (area > maxArea)
				{
					index = i;
					maxArea = area;
				}
			}

			// approximate contour down to 4 points
			// TODO : This idea sounds weird. Why not check all contours and find one that is best approximated by 4 points???
			CvPoint[] biggestContour = contoursFound[index];
			newPtV = new Point[biggestContour.Length];
			PointOps.CopyCvPointsToPoints( biggestContour, newPtV ); //PointOps.CopyCvPointsToGenericPointsArray( biggestContour, newPtV );
			double epsilon = 1;
			while (newPtV.Length > maxContourPoints)
			{
				newPtV = Cv2.ApproxPolyDP( newPtV, epsilon++, true );
				// TODO : Is incrementing epsilon by 1 a bit stupid? Maybe increment exponentially?
			}

			// finally
			CvPoint[] cvPoints = new CvPoint[newPtV.Length];
			PointOps.CopyPointsToCvPoints( newPtV, cvPoints );
			return cvPoints;
		}
	}
}
