using System;
using OpenCvSharp;
using UnityEngine;
using System.Collections.Generic;
using OpenCvSharp.CPlusPlus;
using System.Linq;
using OpenCvSharp.Blob;
using OpenCvSharp.Utilities;
using OpenCvSharp.UserInterface;
using OpenCvSharp.Extensions;
using System.Collections;

namespace MyCVSharp
{
	public class RectPoints
	{
		private uint numFrames = 0;
		private bool reset;

		private bool oldPtValid = false;
		// TODO : Check if "CvPoint2D32f" is essential, since all operation is done with "CvPoint"
		private List<CvPoint> oldPt = new List<CvPoint>(); // List<CvPoint2D32f> oldPt = new List<CvPoint2D32f>();


		//****metavlites apo kawane cpp (!)*****//
		private int width;
		private int height;
		private CvMat temp;
		private List<CvPoint> final4P = new List<CvPoint>(4);
		private int minMaskY;

		private double thresholdDist;

		private CvMat imageDest3 = null;
		private CvMat overlay = null;
		private CvPoint2D32f[] inputQuad = new CvPoint2D32f[4]; // Input Quadrilateral or Image plane coordinates
		private CvPoint2D32f[] outputQuad = new CvPoint2D32f[4]; // Output Quadrilateral or World plane coordinates

		// NOTE : updates oldPt array after a long, long, long process
		// was: private void processFrame(CvMat inputMat)
		private CvMat processFrame( TextureData textureData )
		{
			return processFrame( UUtils.TextureToMat24( textureData ) );
		}

		// => inputMat MUST be 24/32 bit
		private CvMat processFrame( CvMat inputMat )
		{
			// return "inputMat" after lots. LOTS. Of processing

			width = inputMat.Cols;
			height = inputMat.Rows;

			if (true) {
				// I have no idea what on earth is the purpose of this:
				//CvMat temp2 = inputMat( new CvRect( inputMat.Cols / 25, inputMat.Cols / 25, inputMat.Cols - 2 * (inputMat.Cols / 25), inputMat.Rows - 2 * (inputMat.Rows / 25) ) );
				//resize( temp2, temp2, inputMat.size() );
				//temp2.copyTo( inputMat );
				int borderX = inputMat.Cols / 25; // 4% of original
				int borderY = inputMat.Rows / 25;
				CvRect roi = new CvRect( borderX, borderY, inputMat.Cols - 2 * borderX, inputMat.Rows - 2 * borderY );
				CvMat temp2 = inputMat.GetSubRect( out temp2, roi ); // stupid to pass "out temp2"?
				inputMat = temp2;
				// =TODO : What? temp2.Copy( inputMat );
				// is it really required to remove 4% of the input image's edges?
			}


			CvMat inputMat_grey8 = MatOps.ConvertChannels( inputMat );
			CvMat inputMat_grey = MatOps.ConvertElements( inputMat_grey8, MatrixType.F32C1, 1.0 / 255.0 );
			// TODO : looks like a waste to make two conversions from inputMat to _grey, instead of 1
			// since OpenCV doesn't support it, it could be made manually

			inputMat_grey = Filters.IBO( inputMat_grey ); // inputMat_grey = 32f
			inputMat_grey = MatOps.ConvertElements( inputMat_grey, MatrixType.U8C1, 255 ); // inputMat_grey = 8u
			Filters.ContrastEnhancement( inputMat_grey ); // TODO : probably not needed

			// mask passed originally in method below was all white, so I optimized it out. Passing the number of pixels was also dumb-o.
			double thresh = Filters.NeighborhoodValleyEmphasis( inputMat_grey );

			Cv.Threshold( inputMat_grey, inputMat_grey, thresh, 255, ThresholdType.BinaryInv );
			IplConvKernel element = new IplConvKernel( 3, 3, 1, 1, ElementShape.Cross );
			Cv.Erode( inputMat_grey, inputMat_grey, element );
			Cv.Dilate( inputMat_grey, inputMat_grey, element );

			// TODO : check if check is required
			if (inputMat_grey.ElemType != MatrixType.U8C1)
				inputMat_grey = MatOps.ConvertElements( inputMat_grey, MatrixType.U8C1, 255.0 );

			// =======
			// is this just a test?
			CvPoint[] newPtV = Filters.DistillContours( inputMat_grey, 5, Const.PointZero );
			CvMat imageDest;
			using (CvMemStorage storage = new CvMemStorage())
			{
				CvSeq<CvPoint> updateContours = CvSeq<CvPoint>.FromArray( newPtV, SeqType.Contour, storage);
				imageDest = new CvMat( inputMat.Rows, inputMat.Cols, MatrixType.U8C1 );
				Cv.DrawContours( imageDest, updateContours, Const.ScalarWhite, 0, 100, 16 );
			}
			// =======

			kawane( newPtV ); // updates thresholdDist, minMaskY, final4P

			//*******************************************set a greater contour for estimation of the missing points*******************************//

			// =======
			newPtV = Filters.DistillContours( inputMat_grey, 100, Const.PointZero );
			using (CvMemStorage storage = new CvMemStorage())
			{
				CvSeq<CvPoint> updateContours = CvSeq<CvPoint>.FromArray( newPtV, SeqType.Contour, storage );
				imageDest = new CvMat( inputMat.Rows, inputMat.Cols, MatrixType.U8C1 );
				Cv.DrawContours( imageDest, updateContours, Const.ScalarWhite, 0, 100, 1, LineType.AntiAlias );
			}
			// =======

			CvMat mask1 = new CvMat( inputMat.Rows, inputMat.Cols, MatrixType.U8C1, 0 );
			Cv.FillConvexPoly( mask1, newPtV, Const.ScalarWhite, 0, 0 );

			temp = MatOps.ConvertChannels( inputMat );
			temp.Copy(imageDest, mask1);
			Cv.Canny( imageDest, imageDest, 150, 300, ApertureSize.Size3 );
			IplConvKernel element2 = new IplConvKernel( 3, 3, 1, 1, ElementShape.Rect );
			Cv.Dilate( imageDest, imageDest, element2 );
			Cv.Erode( imageDest, imageDest, element2 );

			CvLineSegmentPoint[] lines = Cv2.HoughLinesP( new Mat(imageDest), 1, Cv.PI / 180 /*NOTE : 1 degree angle*/, 50, 50, 50 ); // TODO : those 50s..?
			extendLines( lines, 350 ); // TODO : This idea sounds arbitary? And why 350? At least some percentage?

			// draw extended lines
			for (int i = 0; i < lines.Length; ++i)
			{
				CvLineSegmentPoint l = lines[i];
				Cv.Line( imageDest, l.P1, l.P2, Const.ScalarWhite, 1, LineType.AntiAlias );
			}

			Cv.Dilate( imageDest, imageDest, element2 ); // TODO : FIX : Dilate again?!

			// another huge function here...
			fourPoints( lines );

			////////////

			//********************************************************************* replace estimate points with mask corners ********//
			if (oldPt.Count != 0)
			{
				//**
				// BEWARE : great use of the English language following right below:
				// test for each and every one of the last slice delete each one of all the revisited of the above and estimate for only the best the off topic adapt
				//**
				List<int> positions = new List<int>( final4P.Count);
				for (int i = 0; i < final4P.Count; ++i)
				{
					positions.Add( -1 ); // "initialize" positions[i]
					double distmin = 10000;
					for (int j = 0; j < oldPt.Count; ++j)
					{
						double distAB = PointOps.Norm( oldPt[j] - final4P[i] );
						if (distAB < distmin)
						{
							distmin = distAB;
							positions[i] = j;
						}
					}
				}
				int flagFrCounter = 0;
				for (int i = 0; i < final4P.Count; ++i)
				{
					double distA = PointOps.Norm( oldPt[positions[i]] - final4P[i] );
					//********************* threshold pou na orizei tin megisti perioxi gia anazitisi,alliws na krataei to proigoumeno simeio*******//

					if (distA < thresholdDist) //if(distA<80)
					{
						oldPt[positions[i]] = final4P[i];
						--flagFrCounter;
					}
					++flagFrCounter;
				}
				if (reset)
				{
					numFrames = 0;
					oldPt.Clear();
					final4P.Clear();
				}


			}
			//pointsb[0]=thresholdDist;
			//****************************************************************************//

			for (int i = 0; i < oldPt.Count; ++i)
				Cv.Circle( temp, oldPt[i], 2, Const.ScalarRed, 3 );
			MatOps.Convert8To24(temp).Copy( inputMat );
			//MatOps.ConvertChannels( temp, ColorConversion.GrayToBgr ).Copy( inputMat );
			//temp.Copy( inputMat );



			//******************************************************OVERLAY IMAGE***********************************************//////
			if (oldPt.Count == 0)
				return inputMat; // end of line

			CvMat black2;
			if (overlay != null)
			{
				black2 = overlay.Clone(); //=imread("cubes.jpg");
				Cv.Resize( black2, inputMat, Interpolation.NearestNeighbor ); // TODO : check if interpolation type is appropriate
			} else
			{
				black2 = new CvMat( inputMat.Rows, inputMat.Cols, MatrixType.U8C3 );
			}

			List<CvPoint> tempPoint = new List<CvPoint>(4);
			//vector<Point> tempPoint;
			int pp = 0;

			// BEWARE : the guy is copy/pasting needlessly?
			int mini = 1000000;
			for (int i = 0; i < oldPt.Count; ++i)
			{
				if (oldPt[i].Y < mini)
				{
					mini = oldPt[i].Y;
					pp = i;
				}
			}
			tempPoint.Add( oldPt[pp] );
			mini = 1000000;
			for (int i = 0; i < oldPt.Count; ++i)
			{
				if (oldPt[i].Y < mini && oldPt[i] != tempPoint[0])
				{
					mini = oldPt[i].Y;
					pp = i;
				}
			}
			tempPoint.Add( oldPt[pp] );
			mini = 1000000;
			for (int i = 0; i < oldPt.Count; ++i)
			{
				int tempmini = Math.Abs( oldPt[i].X - tempPoint[1].X );
				if (tempmini < mini && oldPt[i] != tempPoint[0] && oldPt[i] != tempPoint[1])
				{
					mini = tempmini;
					pp = i;
				}
			}
			tempPoint.Add( oldPt[pp] );

			for (int i = 0; i < oldPt.Count; ++i)
			{
				CvPoint pt = oldPt[i];
				bool found = false;
				for (int j = 0; j < tempPoint.Count; ++j)
					if (tempPoint[j] == pt) { found = true; break; }
				if (!found)
					tempPoint.Add( pt );
			}

			// only keep up to 4 points
			List<CvPoint> co_ordinates = new List<CvPoint>( 4 );
			{
				int maxIndex = Math.Min( 4, tempPoint.Count );
				for (int i = 0; i < maxIndex; ++i)
				{
					co_ordinates.Add( tempPoint[i] );
				}
			}

			// lost me...
			if (outputQuad[0] == outputQuad[2])
			{
				{
					int maxIndex = Math.Min( 4, tempPoint.Count );
					for (int i = 0; i < maxIndex; ++i)
					{
						outputQuad[i] = tempPoint[i];
					}
				}
			}
			else
			{
				CvPoint2D32f rr;
				for (int i = 0; i < 4; ++i)
				{
					List<double> dist = new List<double>(tempPoint.Count);
					for (int j = 0; j < tempPoint.Count; ++j)
					{
						rr = tempPoint[j];
						dist.Add( PointOps.Norm( outputQuad[i] - rr ) );
					}

					double minimumDist = dist.Min();
					int min_pos = Utils.FindIndex(dist, minimumDist);
					if (tempPoint.Count > 0)
					{
						outputQuad[i] = tempPoint[min_pos];
						tempPoint.RemoveAt( min_pos );
					}
				}
			}


			// The 4 points where the mapping is to be done , from top-left in clockwise order
			inputQuad[0] = new CvPoint2D32f( 0, 0 );
			inputQuad[1] = new CvPoint2D32f( inputMat.Cols - 1, 0 );
			inputQuad[2] = new CvPoint2D32f( inputMat.Cols - 1, inputMat.Rows - 1 );
			inputQuad[3] = new CvPoint2D32f( 0, inputMat.Rows - 1 );
			//Input and Output Image;


			// Get the Perspective Transform Matrix i.e. lambda (2D warp transform)
			// Lambda Matrix
			CvMat lambda = Cv.GetPerspectiveTransform( inputQuad, outputQuad );
			// Apply this Perspective Transform to the src image
			// - get a "top-down" view of the supposedly box-y area
			Cv.WarpPerspective( black2, black2, lambda, Interpolation.Cubic, Const.ScalarBlack );
			// see nice explanation : http://www.pyimagesearch.com/2014/08/25/4-point-opencv-getperspective-transform-example/


			CvMat maskOV = new CvMat( inputMat.Rows, inputMat.Cols, MatrixType.U8C1, Const.ScalarBlack );
			using (CvMemStorage storage = new CvMemStorage())
			{
				CvSeq<CvPoint> updateContours = CvSeq<CvPoint>.FromArray( co_ordinates, SeqType.Contour, storage );
				imageDest = new CvMat( inputMat.Rows, inputMat.Cols, MatrixType.U8C1 );
				Cv.DrawContours( maskOV, updateContours, Const.ScalarWhite, 0, 100, 16 );
				//drawContours( maskOV, co_ordinates, 0, Scalar( 255 ), CV_FILLED, 8 );
			}

			double alpha = 0.8;
			double beta = (1.0 - alpha);
			Cv.AddWeighted( black2, alpha, inputMat, beta, 0.0, black2 );
			black2.Copy( inputMat, maskOV );

			return inputMat;
		}

		void fourPoints( CvLineSegmentPoint[] linesArray )
		{
			List<CvLineSegmentPoint> lines = new List<CvLineSegmentPoint>( linesArray );
			int i, j, k;

			List<double> angleV = new List<double>(lines.Count);
			for (i = lines.Count - 1; i >= 0; --i)
			{
				CvLineSegmentPoint lineSegm = lines[i];
				angleV.Add( Math.Atan2( lineSegm.P1.Y - lineSegm.P2.Y, lineSegm.P1.X - lineSegm.P2.X ) );
			}

			CvPoint p1, p2, p0, p0_;
			//Discard almost parallel lines and keep the largest
			// FIX : everything about this sucks
			for (i = 0; i < lines.Count; ++i)
			{
				CvLineSegmentPoint segi = lines[i];
				p0 = segi.P1;
				p0_ = segi.P2;
				double e2 = p0.DistanceTo( p0_ );

				for (j = 0; j < lines.Count; ++j)
				{
					if (i == j) // ugly?
						continue;

					if (Math.Abs( angleV[i] - angleV[j] ) > 0.1 || Math.Abs( angleV[i] - angleV[j] ) > Cv.PI / 2.0 - 0.1 && Math.Abs( angleV[i] - angleV[j] ) < Cv.PI / 2.0 + 0.1)
						continue;

					CvLineSegmentPoint segj = lines[j];
					p1 = segj.P1;
					p2 = segj.P2;

					if (PointOps.LineDistance( p1, p2, p0 ) < 15 && PointOps.LineDistance( p0, p0_, p1 ) < 15)
					{
						if (p1.DistanceTo( p2 ) > e2)
						{
							lines.RemoveAt( i );
							angleV.RemoveAt( i );
							--i;
							--j;
							break;
						}
						else
						{
							lines.RemoveAt( j );
							angleV.RemoveAt( j );
							--j;
						}
					}

				}
			}

			// instead of 3 lists, we could have one with custom struct containing all 3 required values
			List<CvPoint> allPointsV = new List<CvPoint>();
			List<int> fstln = new List<int>();
			List<int> secln = new List<int>();
			const int bound = 50;

			for (i = 0; i < lines.Count; ++i)
			{
				CvLineSegmentPoint segmI = lines[i];

				for (j = 0; j < lines.Count; ++j)
				{
					if (i == j)
						continue; // ugly?

					CvLineSegmentPoint segmJ = lines[j];
					if ( PointOps.LineIntersection( segmI.P1, segmI.P2, segmJ.P1, segmJ.P2, out p1 ) )
					{
						if (p1.X > -bound && p1.X < temp.Cols + bound && p1.Y > -bound && p1.Y < temp.Rows + bound)
						{
							bool foundSamePt = false;
							for (k = 0; k < allPointsV.Count; ++k)
							{
								if (p1 == allPointsV[k])
								{
									foundSamePt = true;
									break;
								}
							}
							if (!foundSamePt)
							{
								allPointsV.Add( p1 );
								fstln.Add( i );
								secln.Add( j );
							}
						}
					}
				}
			}

			if (allPointsV.Count == 0)
			{
				reset = true;
				return;
			}

			reset = false;

			// time to start doing our drawings
			if (imageDest3 == null)
				imageDest3 = new CvMat( height, width, MatrixType.U8C3 );

			// are we at start or just not found any points yet?
			if (oldPt.Count == 0 || numFrames < 20)
			{

				//************************draw intersections************************//
				for (i = 0; i < allPointsV.Count; ++i)
				{
					CvScalar circleColor;
					if (allPointsV[i].Y < height - 10)
						circleColor = Const.ScalarGreen;
					else
						circleColor = Const.ScalarWhite;

					Cv.Circle( imageDest3, allPointsV[i], 7, circleColor );
				}

				//mapping the detected corners with lines intersections
				List<int> tracker = new List<int>( final4P.Count );
				for (j = 0; j < final4P.Count; ++j)
				{
					double dist = PointOps.Norm( final4P[j] - allPointsV[0] );
					tracker.Add( 0 ); // tracker[j] = 0;
					for (i = 0; i < allPointsV.Count; ++i)
					{
						double distA = PointOps.Norm( final4P[j] - allPointsV[i] );
						if (distA < dist)
						{
							dist = distA;
							tracker[j] = i;
						}
					}
				}

				//*******  draw mapped corners  *****************//
				for (j = 0; j < final4P.Count; ++j)
				{
					Cv.Circle( imageDest3, allPointsV[tracker[j]], 8, Const.ScalarPurple );
				}

				//*******************************************************************************************//

				List<int> linesIds = new List<int>( final4P.Count );
				for (i = 0; i < final4P.Count; ++i)
				{
					int counterfstln = 0;
					for (j = 0; j < final4P.Count; ++j)
					{
						if (i == j || fstln[tracker[i]] == fstln[tracker[j]]/*this might be redundant after 1st check*/ || fstln[tracker[i]] == secln[tracker[j]])
						{
							++counterfstln;
						}
					}
					int countersecln = 0;
					for (j = 0; j < final4P.Count; ++j)
					{
						if (i == j || secln[tracker[i]] == fstln[tracker[j]] || secln[tracker[i]] == secln[tracker[j]])
						{
							++countersecln;
						}
					}

					if (counterfstln < countersecln)
					{
						linesIds.Add(fstln[tracker[i]]); // linesIds[i] = fstln[tracker[i]];
					}
					else
					{
						linesIds.Add( secln[tracker[i]] ); // linesIds[i] = secln[tracker[i]];
					}

				}

				List<int> maxdistpos1 = new List<int>(tracker.Count); // TODO : check if Count is always less than 3-4...
				for (j = 0; j < tracker.Count; j++)
				{
					maxdistpos1.Add( 0 ); // maxdistpos1.Add( -1 ); // "initialize" maxdistpos1[j], so that it can be re-assigned below
					// TODO : logic is wrong!!!! Not all [j]s are assigned. Proof : if "initialized" with "-1", it just crashes later!
					double dist = 0;
					for (i = 0; i < fstln.Count; i++)
					{
						if (linesIds[j] == fstln[i] || linesIds[j] == secln[i] && allPointsV[i].Y < height - 15)
						{
							double distA = PointOps.Norm( allPointsV[tracker[j]] - allPointsV[i] );
							if (distA > dist)
							{
								dist = distA;
								maxdistpos1[j] = i;
							}
						}
					}
				}

				oldPt.Clear();

				for (i = 0; i < final4P.Count; i++)
				{
					oldPt.Add( final4P[i] );
				}

				List<CvPoint> candidatePts = new List<CvPoint>();
				List<double> candist = new List<double>();

				for (i = 0; i < maxdistpos1.Count; i++)
				{
					Cv.Circle( imageDest3, allPointsV[maxdistpos1[i]], 7, Const.ScalarBlue );
					if (allPointsV[maxdistpos1[i]].Y > minMaskY && allPointsV[maxdistpos1[i]].Y < height - 10)
					{
						candidatePts.Add( allPointsV[maxdistpos1[i]] );
					}
				}


				for (i = 0; i < candidatePts.Count; i++)
				{
					double dist = 0;
					for (j = 0; j < oldPt.Count; j++)
					{
						dist += PointOps.Norm( candidatePts[i] - oldPt[j] );
					}
					candist.Add( dist );
				}

				while (oldPt.Count < 4)
				{
					if (candidatePts.Count != 0)
					{
						int p = candist.FindMaxIndex(); // Utils.FindMaxIndex( candist ); // p = max_element(candist.begin(),candist.end()) - candist.begin();
						oldPt.Add( candidatePts[p] );
						candist.RemoveAt( p );
						candidatePts.RemoveAt( p );
					}
					else
						break;
				}



				for (j = 0; j < oldPt.Count; j++)
				{
					Cv.Circle( imageDest3, oldPt[j], 7, Const.ScalarBlue );
				}

				//***************************************************** end of estimation **************************************************************//
			}
			else
			{
				for (i = 0; i < allPointsV.Count; ++i)
				{
					if (allPointsV[i].Y < height - 10)
					{
						Cv.Circle( imageDest3, allPointsV[i], 7, Const.ScalarGreen );
					}
					else
						Cv.Circle( imageDest3, allPointsV[i], 7, Const.ScalarWhite );
				}


				//mapping the detected corners with lines intersections //
				List<int> tracker = new List<int>( oldPt.Count );
				for (j = 0; j < oldPt.Count; ++j)
				{
					tracker.Add( -1 );
					double dist = 1000000;
					for (i = 0; i < allPointsV.Count; ++i)
					{
						double distA = PointOps.Norm( oldPt[j] - allPointsV[i] );
						if (distA < dist && allPointsV[i].Y < height - 10)
						{
							dist = distA;
							tracker[j] = i;
						}
					}
				}

				for (j = 0; j < oldPt.Count; ++j)
				{
					double distA = PointOps.Norm( oldPt[j] - allPointsV[tracker[j]] );
					//********************* threshold pou na orizei tin megisti perioxi gia anazitisi,alliws na krataei to proigoumeno simeio*******//
					if (distA < thresholdDist)
					{
						oldPt[j] = allPointsV[tracker[j]];
					}
				}
			}
		}

		private void extendLines( CvLineSegmentPoint[] lines, double ext )
		{
			// TODO : this is stupid way to extend a line, does 2 sqrts and is generally non-comprehensible
			// Better just make it parametric, like x = x0 + t * x1, y = y0 + t * y1 where for t=0, {x,y} = P1 & for t=1, {x,y} = P2
			// so we can increase in size by percent, like t = +-0.5 (+50%)

			//Transfer 2-point line segments to type "a*x=b" CvLine2D format
			//TODO : this should probably be redone manually without OpenCV's ultra-generic slow function. We only have 2 points after all.
			List<CvLine2D> fitLinesV = new List<CvLine2D>( lines.Length );
			CvPoint[] forFitline = new CvPoint[2];
			for (int i = 0; i < lines.Length; ++i)
			{
				forFitline[0] = lines[i].P1;
				forFitline[1] = lines[i].P2;
				CvLine2D fitLinef = Cv.FitLine2D( forFitline, DistanceType.L2, 0, 0.01, 0.01 );
				fitLinesV.Add( fitLinef );
			}

			CvPoint p1, p2, p3, p4;
			for (int i = 0; i < lines.Length; i++)
			{
				CvLineSegmentPoint lineSegm = lines[i];
				CvLine2D fitLine = fitLinesV[i];
				int fitLineVx = (int)(fitLine.Vx * ext);
				int fitLineVy = (int)(fitLine.Vy * ext);
				p1 = new CvPoint( lineSegm.P1.X + fitLineVx, lineSegm.P1.Y + fitLineVy );
				p2 = new CvPoint( lineSegm.P2.X - fitLineVx, lineSegm.P2.Y - fitLineVy );
				p3 = new CvPoint( lineSegm.P1.X - fitLineVx, lineSegm.P1.Y - fitLineVy );
				p4 = new CvPoint( lineSegm.P2.X + fitLineVx, lineSegm.P2.Y + fitLineVy );
				if (p1.DistanceTo(p2) > p3.DistanceTo(p4))
				{
					lineSegm.P1.X = p1.X;
					lineSegm.P1.Y = p1.Y;
					lineSegm.P2.X = p2.X;
					lineSegm.P2.Y = p2.Y;
				}
				else
				{
					lineSegm.P1.X = p3.X;
					lineSegm.P1.Y = p3.Y;
					lineSegm.P2.X = p4.X;
					lineSegm.P2.Y = p4.Y;
				}
			}
		}

		// calculates some threshold, and populates "final4P" List
		private void kawane(CvPoint[] newPtV)
		{
			//****************************//3-uppers//*************************//
			// what's that?
			int up1 = -1;
			int minYy = 100000;
			for (int k = 0; k < newPtV.Length; k++)
			{
				if (newPtV[k].Y < minYy)
				{
					minYy = newPtV[k].Y;
					up1 = k;
				}
				if (newPtV[k].Y == minYy)
				{
					if (newPtV[k].X > newPtV[up1].X)
					{
						minYy = newPtV[k].Y;
						up1 = k;

					}
				}
			}

			minMaskY = minYy; // this little variable is magically used in "FourPoints" function

			int up2 = -1;
			minYy = 100000;
			for (int k = 0; k < newPtV.Length; k++)
			{
				if (newPtV[k].Y < minYy && k != up1)
				{
					minYy = newPtV[k].Y;
					up2 = k;
				}
				if (newPtV[k].Y == minYy && k != up1)
				{
					if (newPtV[k].X > newPtV[up2].X)
					{
						minYy = newPtV[k].Y;
						up2 = k;
					}
				}
			}
			int up3 = -1;
			minYy = 100000;
			for (int k = 0; k < newPtV.Length; k++)
			{
				if (newPtV[k].Y < minYy && k != up1 && k != up2)
				{
					minYy = newPtV[k].Y;
					up3 = k;
				}
				if (newPtV[k].Y == minYy && k != up1 && k != up2)
				{
					if (newPtV[k].X > newPtV[up3].X)
					{
						minYy = newPtV[k].Y;
						up3 = k;
					}
				}
			}

			final4P.Clear(); // NOTE : treat array as empty. Not just make it local, cause it's used in "FourPoints" function.
			if (up1 > -1)
			{
				final4P.Add( newPtV[up1] );
				if (newPtV[up2].Y < height - 10)
				{
					final4P.Add( newPtV[up2] );
					thresholdDist = PointOps.Norm( final4P[0] - final4P[1] );
				}
				if (newPtV[up3].Y < height - 10)
				{
					final4P.Add( newPtV[up3] );
					thresholdDist += PointOps.Norm( final4P[1] - final4P[2] );
					thresholdDist = thresholdDist / 2;
				}
			}

			thresholdDist /= 9; // ...whatever
		}

		// getBoxPoints
		public float[] calcNextFrame( TextureData textureData, Texture2D output = null )
		{
			return calcNextFrame( UUtils.TextureToMat24( textureData ), output );
		}

		public float[] calcNextFrame( CvMat input, Texture2D output = null )
		{
			float[] points = new float[8]; // return this
			
			++numFrames;

			//input.Flip(input, FlipMode.Y); // TODO : to check if this is really needed. If yes, it SHOULD go inside "processFrame"
			// src_img == dest_img

			input = processFrame( input );

			if (numFrames < 20)
			{
				// SAY CHEEEESE
				//overlay.release();
				//putText( dest_img, "HOLD ", cvPoint( 30, 30 ),
				//		FONT_HERSHEY_COMPLEX_SMALL, 1, cvScalar( 0, 255, 0, 0 ), 1, CV_AA );
				for (int i = 0; i < 4; i++)
				{
					outputQuad[i] = new CvPoint2D32f( 0, 0 );
				}
			}


			if (reset)
			{
				// SAY RESEEEEET
				//overlay.release();
				//putText( dest_img, "RESTART tracker", cvPoint( 30, dest_img.rows / 2 ),
				//		FONT_HERSHEY_COMPLEX_SMALL, 1, cvScalar( 255, 0, 0, 0 ), 1, CV_AA );
			}

			//input.Flip(input, FlipMode.Y); // TODO : to check if this is really needed

			if (output != null)
				UUtils.CopyMatToTexture2D( input, output );

			if (oldPtValid)
			{
				int j = 0;
				for (int i = 0; i < 4; ++i)
				{
					points[j] = oldPt[i].X;
					points[j + 1] = oldPt[i].Y;
					j++;
				}
			}
			else
			{
				// this points[] is dumb as hell, return something more meaningful
				for (int i = 0; i < 8; i += 2)
				{
					points[i] = outputQuad[i >> 1].X;
					points[i + 1] = outputQuad[i >> 1].Y;
				}
			}

			return points;
		}
	}
}
