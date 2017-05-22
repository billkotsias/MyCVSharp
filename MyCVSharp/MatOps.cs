using OpenCvSharp;
using System;

namespace MyCVSharp
{
	public class MatOps
	{
		// Get area of similarly-colored pixels, starting from a point
		// Actually uses the FloodFill method but since this took me some time to figure out how it works, I have "libraried" the code
		// Note : uses 8-way filling, one may want to alter this...
		// =>	mask is input/output CvMat and has several restrictions; if you intend to use it on successive calls, pass it as null on first call
		//		and the function will create it for you appropriately.
		static public CvConnectedComp GetAreaOfSimilarPixels( CvMat input, CvPoint startPoint, CvScalar lower, CvScalar upper, ref CvMat mask, byte maskCol = 255)
		{
			CvConnectedComp filledAreaData;
			if (mask == null)
				mask = new CvMat( input.Rows + 2, input.Cols + 2, MatrixType.U8C1, new CvScalar( 0, 0, 0, 0 ) );

			input.FloodFill(
				startPoint, 0, lower, upper, out filledAreaData,
				(FloodFillFlag.Link8 | FloodFillFlag.MaskOnly | FloodFillFlag.FixedRange) + (maskCol << 8), mask );

			return filledAreaData;
		}

		// return empty CvMat with same size as source but with another type
		static public CvMat CopySize( CvMat src, MatrixType newType )
		{
			return new CvMat( src.Rows, src.Cols, newType );
		}

		// return empty CvMat with same size as source but with another type
		static public CvMat CopySize( CvMat src, MatrixType newType, CvScalar value )
		{
			return new CvMat( src.Rows, src.Cols, newType, value );
		}

		// wraps Cv.CvtColor, because it is much more restrictive than the C++ equivalent
		// NOTE : Seems to only work with Color-to-Gray conversion!!!
		static public CvMat ConvertChannels( CvMat src, MatrixType dstType = MatrixType.U8C1, ColorConversion convertMode = ColorConversion.BgrToGray )
		{
			CvMat dst = CopySize( src, dstType );
			Cv.CvtColor( src, dst, convertMode );
			return dst;
		}

		// ΝΟΤΕ : Convert to ParallelFor loop
		static public CvMat Convert8To24( CvMat src )
		{
			CvMat dst = CopySize( src, MatrixType.U8C3 );
			unsafe
			{
				int count = src.Rows * src.Cols;
				byte* srcBytes = src.DataByte;
				byte* dstBytes = dst.DataByte;
				for (int i = 0, k = 0; i < count; ++i)
				{
					byte srcByte = srcBytes[i];
					dstBytes[k++] = srcByte;
					dstBytes[k++] = srcByte;
					dstBytes[k++] = srcByte;
				}
			}
			return dst;
		}

		// converts the element type (32bit<=>8bit); CANNOT change channels number
		static public CvMat ConvertElements( CvMat src, MatrixType newType, double scale = 1, double shift = 0 )
		{
			CvMat dst = CopySize( src, newType );
			Cv.ConvertScale( src, dst, scale, shift );
			return dst;
		}

		// OpenCVSharp was so inconsequential with its own copy function that I had to wrap it up and make it into something sensible
		static public CvHistogram CopyHistogram(CvHistogram source)
		{
			int[] sizes = new int[source.Dim];
			for (int i = 0; i < source.Bins.Dims; ++i)
			{
				sizes[i] = source.Bins.GetDimSize( i );
			}
			CvHistogram newHisto = new CvHistogram( sizes, source.Type );
			source.Copy( newHisto ); // NOTE : source is copied INTO newHisto = "CopyTo"
			return newHisto;
		}

		static public CvMat BGRtoHueCV( CvMat input )
		{
			CvMat hsl = MatOps.ConvertChannels( input, MatrixType.U8C3, ColorConversion.BgrToHsv_Full );
			CvMat hue = MatOps.CopySize( input, MatrixType.U8C1 );
			//CvMat lum = hue.EmptyClone();
			//hsl.Split( hue, null, lum, null );
			hsl.Split( hue, null, null, null );
			return hue;
		}

		// => input = 24bit rgb
		// <= output = float 32bit, single channel (hue only)
		// NOTE : MUST be converted into multi-core loop (Convert to ParallelFor loop)!
		static public CvMat BGRtoHue(CvMat input)
		{
			CvMat output = CopySize( input, MatrixType.F32C1 );
			unsafe
			{
				int count = input.Rows * input.Cols;
				byte* dataIn = input.DataByte;
				float* dataOut = output.DataSingle;
				for (int i = 0, k = 0; i < count; ++i)
				{
					float r = dataIn[k++] / 255f;
					float g = dataIn[k++] / 255f;
					float b = dataIn[k++] / 255f;
					float max = Math.Max( Math.Max( r, g ), b );
					float min = Math.Min( Math.Min( r, g ), b );
					float delta = max - min;
					float hue;
					//float brightness = max;
					//float saturation = max == 0 ? 0 : (max - min) / max;
					if (delta != 0)
					{
						if (r == max)
							hue = (g - b) / delta;
						else
							if (g == max)
								hue = 2 + (b - r) / delta;
							else
								hue = 4 + (r - g) / delta;
						hue *= (1f / 6);
						//hue *= 60;
						//if (hue < 0) hue += 360;
						//hue /= 360;
						//hue = 1 - hue;
					}
					else
						hue = 0;
					dataOut[i] = hue;
				}
			}
			return output;
		}

		// => 24bit rgb
		// <= 24bit rgb, normalized of some sort
		// NOTE : MUST be converted into multi-core loop (Convert to ParallelFor loop)!
		static public CvMat MyNormalize( CvMat input )
		{
			CvMat output = input.Clone();
			unsafe
			{
				int count = input.Rows * input.Cols * 3;
				byte* dataIn = input.DataByte;
				byte* dataOut = output.DataByte;
				for (int i = 0; i < count; i+=3)
				{
					int r = dataIn[i];
					int g = dataIn[i + 1];
					int b = dataIn[i + 2];
					double invLength = 255.0 / Math.Sqrt( r * r + b * b + g * g); // because: channel / length => [0...1]
					if (!Double.IsInfinity(invLength))
					{
						dataOut[i] = (byte)(r * invLength);
						dataOut[i + 1] = (byte)(g * invLength);
						dataOut[i + 2] = (byte)(b * invLength);
					}
				}
			}
			return output;
		}

		// maybe this should go in another Class...
		static private int WindowCounter = 0;
		static public void NewWindowShow( CvArr imageToShow, string windowName = null )
		{
			if (windowName == null)
			{
				windowName = (WindowCounter++).ToString();
			}
			Cv.NamedWindow( windowName );
			Cv.ShowImage( windowName, imageToShow );
		}
	}
}
