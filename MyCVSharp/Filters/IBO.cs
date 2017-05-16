using System;
using OpenCvSharp;

namespace MyCVSharp
{
	// Cv-level filters
	static public partial class Filters
	{
		// affect a CvMat in an unspeakable way (maybe this: http://ieeexplore.ieee.org/document/4062288/?reload=true)
		// FIX : looks like there's lots of room for optimization
		// NOTE : It doesn't just look like it, IT'S P.A.N.A.R.G.O.
		// <= returns 32bit float greyscale
		static public CvMat IBO(CvMat image)
		{
			int imageRows = image.Rows;
			int imageCols = image.Cols;
			CvMat IBOsub = new CvMat( imageRows, imageCols, MatrixType.F32C1, new CvScalar(0) );

			const int kernelCols = 3;
			const int kernelRows = 3;

			int x, y, k, l;
			bool firstElement;
			int a1, b1, a2, b2;

			a1 = (kernelCols - 1) / 2;
			b1 = a1 + 1;
			a2 = (kernelRows - 1) / 2;
			b2 = a2 + 1;

			//convert g(x,y) = Z = Ð(f(x,y))  for 8 values surrounding the x,y value...
			for (x = a1; x < imageCols - a1; x++)
			{
				for (y = a1; y < imageRows - a1; y++)
				{
					firstElement = true;

					for (k = x - a1; k < x + b1; k++)
					{

						for (l = y - a2; l < y + b2; l++)
						{
							double val = image.GetReal2D( l, k );
							if (firstElement)
							{

								IBOsub.SetReal2D( y, x, val );// * image.at<float>(l,k);
								firstElement = false;
							}
							else
							{
								IBOsub.SetReal2D( y, x, IBOsub.GetReal2D(y,x) * val ); // originally there was multiplication not addition
								// TODO : I changed back to multiplication, because addition gave back white image. Great?
							}
						}
					}
				}
			}

			// TODO : I subtracted this addition because it seemed too much. Good?
			////////this is an addition by us
			if (false)
			{
				for (x = a1; x < imageCols - a1; x++)
				{
					for (y = a1; y < imageRows - a1; y++)
					{
						double sqr = IBOsub.GetReal2D( y, x );
						sqr *= sqr;
						IBOsub.SetReal2D( y, x, sqr );
					}
				}
			}

			for (x = 0; x < imageCols; x++)
			{
				for (k = 0; k < a1; k++)
				{
					IBOsub.SetReal2D( k, x, IBOsub.GetReal2D( a1, x ) );
					IBOsub.SetReal2D( imageRows - (k + 1), x, IBOsub.GetReal2D( imageRows - (a1 + 1), x ) );
				}
			}

			for (y = 0; y < imageRows; y++)
			{
				for (k = 0; k < a2; k++)
				{
					IBOsub.SetReal2D( y, k, IBOsub.GetReal2D( y, a2 ) );
					IBOsub.SetReal2D( y, imageCols - (k + 1), IBOsub.GetReal2D( y, imageCols - (a2 + 1) ) );
				}
			}

			// find the max value of the mat
			double minVal, maxVal;
			CvPoint minLoc, maxLoc;
			Cv.MinMaxLoc( IBOsub, out minVal, out maxVal, out minLoc, out maxLoc );
			image = (IBOsub * (1.0 / maxVal)); // by this function, image is now a new object

			IplConvKernel element = new IplConvKernel( 3, 3, 1, 1, ElementShape.Rect ); // simply returns a predefined shape-in-a-Mat
			Cv.Dilate( image, image, element );

			return image;
		}
	}
}
