using OpenCvSharp;
using UnityEngine;

namespace MyCVSharp
{
	// Unity-friendly filters
	// NOTE : If we need speed and lots of different processing, we shouldn't use these functions sequencially, but instead:
	// 1) Utils.TextureToMat
	// 2) Do all required stuff to CvMat [...]
	// [...]
	// 3) Utils.CopyMatToTexture2D
	// 4) Texture2D.Apply()
	// so from- and to- Unity conversions are only done once
	static public class UFilters
	{
		static public void Canny( TextureData input, Texture2D output, double threshold1 = 50.0, double threshold2 = 200.0 )
		{
			CvMat cvMat;

			cvMat = UUtils.TextureToMat8(input);
			//Utils.NewWindowShow( cvMat );

			Cv.Canny( cvMat, cvMat, threshold1, threshold2 );
			//Utils.NewWindowShow( cvMat );

			UUtils.CopyMatToTexture2D( cvMat, output );
			//CvMat cvMater = Utils.TextureToMat8( new TextureData(output) );
			//Utils.NewWindowShow( cvMater );
		}
	}
}
