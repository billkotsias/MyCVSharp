using System;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;

namespace MyCVSharp
{
	public class FindTheBox
	{
		private int currentFrame = -1;

		// only calculated/used in a single frame; can be reset again
		private bool hintPosSet = false;
		private CvPoint hintPos = new CvPoint();

		// flood-fill search parameter
		public double floodHueTolerance = 0.08;		// initial tollerances, may change if can't detect sensible box area
		public double floodNormTolerance = 5;
		private double floodMinAreaPercent = 1f/9;	// minimum fraction of input area accepted; logically shouldn't be smaller than 1/9
		private double floodMaxAreaPercent = 2f/3;	// maximum fraction of input area accepted; logically shouldn't be larger than 2/3
		private double floodBroadenFactor = 2;
		private double floodNarrowFactor = 3;		// broaden and narrow should probably not be the same as it could get in a see-saw situation
		// IDEA : Better still, when we CHANGE floodXXXFactor direction (e.g 1st retry we broaden, 2nd retry we narrow), THEN the factor should
		// lessen. Like this: 0.04 (broaden * 2) -> 0.08 (narrow / 1.5) -> 0.0533
		enum BoxEstimationType
		{
			NONE,
			HUE,
			NORMALIZE,
		}
		private BoxEstimationType boxEstimationType = BoxEstimationType.NONE;
		private CvScalar boxEstimatedValue = new CvScalar(); // if 1st value is NaN => not initialized/estimated yet

		public FindTheBox()
		{
		}

		public FindTheBox( int x, int y )
		{
			setBoxHint( x, y );
		}
		
		public void setBoxHint( int x, int y )
		{
			hintPos.X = x;
			hintPos.Y = y;
			hintPosSet = true;
			//Console.Out.WriteLine( "Setting box hint at " + hintPos );
		}

		// => accepts everything OpenCv.FloodFill accepts
		// <= return true if succeeded
		private bool calcBoxHint( CvMat input, ref double tollerance )
		{
			// IDEA :
			// Get area around hinted point and find the range of box colors (due to lighting etc it can't be a single color)

			CvConnectedComp filledAreaData;

			double tol = tollerance; // don't affect value unless we are successful
			int hueArea = input.Rows * input.Cols;
			double floodMaxArea = floodMaxAreaPercent * hueArea;
			double floodMinArea = floodMinAreaPercent * hueArea;

			int retries = 0;
			const int MaxRetries = 4;
			do
			{
				Console.Out.WriteLine( "FLOOD FILLING AT " + hintPos);

				CvScalar scalarTol = new CvScalar( tol, tol, tol, tol ); // cause CvScalar doesn't now how to properly multiply itself with a number!!!
				CvMat filledArea = null;
				filledAreaData = MatOps.GetAreaOfSimilarPixels( input, hintPos, scalarTol, scalarTol, ref filledArea );
				//MatOps.NewWindowShow( filledArea, hue.ElemType+" try:" + retries );
				if (filledAreaData.Area >= floodMinArea)
				{
					if (filledAreaData.Area <= floodMaxArea)
					{
						// keep new values in order to adapt faster next time this function is called!!!
						tollerance = tol;
						break; // we're good to go!!!

					} else
						tol /= floodNarrowFactor; // too big; try again with NARROWER search range
				} else
					tol *= floodBroadenFactor; // too small; try again with BROADER search range

				Console.WriteLine( "On next retry tollerance will be:" + tol );
				Console.Out.WriteLine( retries + ") must retry COL={0} area={1} rect={2}", filledAreaData.Value, filledAreaData.Area, filledAreaData.Rect );
				if (++retries > MaxRetries)
				{
					// can't search for ever, maybe hint wasn't good enough?
					return false;
				}

			} while (true);

			boxEstimatedValue = filledAreaData.Value;
			// TODO : Also get minimum and maximum values in the area returned!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
			// It's a shame this ain't returned upfront by FloodFill!
			Console.Out.WriteLine( retries + ")YEAH! COL={0} area={1} rect={2}", boxEstimatedValue, filledAreaData.Area, filledAreaData.Rect);
			return true;
		}

		// => mat = 8bit binary image (0/255)
		// <= return true if of the 9 pixels in the point's area, at least 5 are white?
		public bool checkFeatureArea(CvMat mat, CvPoint2D32f point, int areaSize = 2)
		{
			float countWhites = 0;

			int x0 = (int)point.X - areaSize;
			if (x0 < 0) return false;
			int y0 = (int)point.Y - areaSize;
			if (y0 < 0) return false;
			int x1 = x0 + areaSize * 2;
			if (x1 >= mat.Cols) return false;
			int y1 = y0 + areaSize * 2;
			if (y1 >= mat.Rows) return false;

			for (int j = y0; j <= y1; ++j)
				for (int i = x0; i <= x1; ++i)
				{
					//Console.WriteLine( "Checking " + i + "," + j + "=" + mat.Get2D( i, j ) );
					if (mat.Get2D( j, i ) != 0) ++countWhites;
				}

			// we don't want white dots in black areas, or black dots in white areas
			float area = (x1 - x0 + 1) * (y1 - y0 + 1);
			if (countWhites >= area * 0.4f && countWhites <= area * 0.7f) return true;
			return false;
		}

		// => input = 24bit rgb
		public void calcNextFrame(CvMat input)
		{
			++currentFrame;

			// IDEA 1 :
			// Get Hue out of input (get rid of lighting)
			// TODO : make following functions into multi-core
			// NOTE : Both below give alternatively good results with different pictures
			CvMat hue = null;
			CvMat normalize = null;

			// no estimation of box color yet, get input's center
			if (!hintPosSet && boxEstimationType == BoxEstimationType.NONE)
				setBoxHint( input.Cols / 2, input.Rows / 2 );

			// new hint set
			if (hintPosSet)
			{
				hintPosSet = false; // don't ever re-enter, unless required
				boxEstimationType = BoxEstimationType.NONE;

				// this logic is here (and not somewhere else) so that we don't have to calculate hue/normalize twice in a single frame
				// that's because hue/normalize may also be needed at a later state of frame processing (to be determined)

				// this check right here sounds stupid, but I want to easily change priority between hue and normalize
				// in the end, one of the 2 will stay at 1st place and the check will be removed...
				if (boxEstimationType == BoxEstimationType.NONE)
				{
					hue = MatOps.BGRtoHue( input );
					//MatOps.NewWindowShow( hue, "HUE-processed" );
					if (calcBoxHint( hue, ref floodHueTolerance ))
						boxEstimationType = BoxEstimationType.HUE;
				}

				if (boxEstimationType == BoxEstimationType.NONE)
				{
					normalize = MatOps.MyNormalize( input );
					MatOps.NewWindowShow( normalize, "NORMALIZE-processed" );
					if (calcBoxHint( normalize, ref floodNormTolerance ))
						boxEstimationType = BoxEstimationType.NORMALIZE;
				}
			}

			// IDEA 2 :
			// Before trying to extract any features, lets try to set a good ROI. The ROI returned by floodfill is certainly not good, as it may as well
			// hold only part of the box. We want to keep the whole region that contains pixels close to the box's estimated.


			{
				CvPoint2D32f[] corners; // extracted features
				int cornerCount; // not exactly "count", but rather "maximum number of corners to return"
				// TODO : Like I said above, if I get the minimum/maximum values, I have an accurate lowerBound/upperBound pair to work with!!!
				CvMat roi;
				CvScalar lowerBound;
				CvScalar upperBound;

				// IDEA 3:
				// Determine if I should check for "features" in the "thresholded" image, or in a cropped grayscale version of the original one!!
				// For now, lets search the thresholded one...
				if (boxEstimationType == BoxEstimationType.HUE)
				{
					roi = MatOps.CopySize( input, MatrixType.U8C1 );
					lowerBound = boxEstimatedValue - floodHueTolerance/1; // TODO : this should be +-(MAX VALUE)
					upperBound = boxEstimatedValue + floodHueTolerance/1;
					if (hue == null)
						hue = MatOps.BGRtoHue( input );
					hue.InRangeS( lowerBound, upperBound, roi );
				}
				else if (boxEstimationType == BoxEstimationType.NORMALIZE)
				{
					// TODO : must investigate, range doesn't return anything
					roi = MatOps.CopySize( input, MatrixType.U8C1 );
					lowerBound = boxEstimatedValue - floodNormTolerance;
					upperBound = boxEstimatedValue + floodNormTolerance;
					if (normalize == null)
						normalize = MatOps.MyNormalize( input );
					normalize.InRangeS( lowerBound, upperBound, roi );
				}
				else
				{
					// Couldn't estimate either way? We are off to a bad start, but lets try to see if features can be extracted anyway.
					roi = MatOps.ConvertChannels( input ); // we are already losing valuable info here!!
					// for this to work, a lesser "qualityLevel" may be required...
				}

				// IDEA 3 :
				// Extract features (actual box corners?!) from ROI with corner detection
				double qualityLevel = 0.1;
				double minimumDistance = 25; // maybe this has to be a percentage of the input-size, rather than an absolute value?!?!?
				bool useHarris = false;
				int blockSize = 3;

				cornerCount = 100;
				Cv.GoodFeaturesToTrack(
					roi, MatOps.CopySize( roi, MatrixType.F32C1, Const.ScalarBlack ), MatOps.CopySize( roi, MatrixType.F32C1, Const.ScalarBlack ),
					out corners, ref cornerCount, qualityLevel, minimumDistance, null, blockSize, useHarris );
				CvMat roiClone = roi.Clone();
				roiClone.SaveImage( "roiClone.png" );
				for (int i = 0; i < cornerCount; ++i)
				{
					// remove "isolated" features
					CvPoint2D32f feature = corners[i];
					if (checkFeatureArea(roiClone, feature))
						roiClone.Circle( feature, 10, 127 );
				}
				MatOps.NewWindowShow( roiClone, "ROI!" );
				Console.WriteLine( "corners=" + cornerCount );

				cornerCount = 100;
				Cv.GoodFeaturesToTrack(
					hue, MatOps.CopySize( roi, MatrixType.F32C1, Const.ScalarBlack ), MatOps.CopySize( roi, MatrixType.F32C1, Const.ScalarBlack ),
					out corners, ref cornerCount, qualityLevel, minimumDistance, null, blockSize, useHarris );
				CvMat hueClone = hue.Clone();
				for (int i = 0; i < cornerCount; ++i)
					hueClone.Circle( corners[i], 10, 127 );
				MatOps.NewWindowShow( hueClone, "HUE-processed" );
				Console.WriteLine( "corners=" + cornerCount );

				// IDEA 4 :
				// INSTEAD of "GoodFeaturesToTrack", go straight for Canny edges and HoughLinesP
				CvMat edgesMat = MatOps.CopySize( roi, MatrixType.U8C1 );
				roi.Canny( edgesMat, 10, 200, ApertureSize.Size3 ); // Size5 also works good; 7 not; rest crash!
				// these values work fine with "box7.png"
				double rho = 1; // 1
				double theta = 1 * Cv.PI/180; // 1*Cv.PI/180
				int threshold = 75; // 75 (quality?)
				double minLength = 1; // 1
				double maxGap = 10000; // 1000000, but not Infinity, for some dumb reason
				CvLineSegmentPoint[] lines = edgesMat.HoughLinesProbabilistic( rho, theta, threshold, minLength, maxGap );
				CvMat linesMat = MatOps.CopySize( edgesMat, MatrixType.U8C3, 0 );
				for (int i = 0; i < lines.Length; ++i)
				{
					linesMat.Line( lines[i].P1, lines[i].P2, Const.ScalarRandom(), 3, LineType.AntiAlias );
				}
				MatOps.NewWindowShow( edgesMat, "edgesMat Canny-Hough" );
				MatOps.NewWindowShow( linesMat, "linesMat" );
				Console.WriteLine( "lines=" + lines.Length );
			}

		}
	}
}
