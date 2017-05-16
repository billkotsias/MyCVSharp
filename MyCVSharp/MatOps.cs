using OpenCvSharp;

namespace MyCVSharp
{
	public class MatOps
	{
		// wraps Cv.CvtColor, because it is much more restrictive than the C++ equivalent
		// NOTE : Seems to only work with Color-to-Gray conversion!!!
		static public CvMat ConvertChannels( CvMat src, ColorConversion convertMode = ColorConversion.BgrToGray )
		{
			CvMat dst = new CvMat( src.Rows, src.Cols, MatrixType.U8C1 );
			Cv.CvtColor( src, dst, convertMode );
			return dst;
		}

		static public CvMat Convert8To24( CvMat src )
		{
			CvMat dst = new CvMat( src.Rows, src.Cols, MatrixType.U8C3 );
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
			CvMat dst = new CvMat( src.Rows, src.Cols, newType );
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
	}
}
