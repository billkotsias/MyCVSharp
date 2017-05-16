using Uk.Org.Adcock.Parallel;
using UnityEngine;
using OpenCvSharp;

namespace MyCVSharp
{
	static public class UUtils
	{
		// Convert Unity Texture to CvMat image 24bit; alpha value is discarded
		// NOTE : Unity Texture doesn't contain pixels getter (as WebCamTexture and Texture2D do), so they have to be passed in separately
		static public CvMat TextureToMat24( TextureData textureData )
		{
			byte[] outCopy;

			// Color32 array : r, g, b, a
			// convert Color32 object to Vec3b object
			// Vec3b is the representation of pixel for Mat
			// Note : <Parallel_For> loop was originally used (may come back to it later)
			int texWidth = textureData.width;
			int texHeight = textureData.height;
			outCopy = new byte[texWidth * texHeight * 3]; // reserve 3 bytes for each source texel

			// a bit faster linear loop
			if (true)
			{
				int srcOffset = textureData.pixels.GetLength( 0 ) - 1;
				int destOffset = outCopy.GetLength( 0 ) - 1;
				while (srcOffset >= 0)
				{
					Color32 col = textureData.pixels[srcOffset--];
					outCopy[destOffset--] = col.r;
					outCopy[destOffset--] = col.g;
					outCopy[destOffset--] = col.b;
				}
			}

			// slower
			if (false)
			{
				for (var j = 0; j < texHeight; ++j)
				{
					var rowOffset = j * texWidth;
					for (var i = 0; i < texWidth; ++i)
					{
						Color32 col = textureData.pixels[rowOffset + i];
						int destOffset = (rowOffset + i) * 3;
						outCopy[destOffset++] = col.b;
						outCopy[destOffset++] = col.g;
						outCopy[destOffset] = col.r;
					}
				}
			}

			// assign the byte array to Mat
			return new CvMat( texHeight, texWidth, MatrixType.U8C3, outCopy, false );
		}

		// converts to 8bit greyscale CvMat
		static public CvMat TextureToMat8( TextureData textureData )
		{
			byte[] outCopy;

			// Color32 array : r, g, b, a
			// convert Color32 object to Vec3b object
			// Vec3b is the representation of pixel for Mat
			// Note : <Parallel_For> loop was originally used (may come back to it later)
			int texWidth = textureData.width;
			int texHeight = textureData.height;
			outCopy = new byte[texWidth * texHeight]; // reserve 1 byte for each source texel

			{
				int srcOffset = textureData.pixels.GetLength( 0 ) - 1;
				int destOffset = outCopy.GetLength( 0 ) - 1;
				while (srcOffset >= 0)
				{
					Color32 col = textureData.pixels[srcOffset--];
					outCopy[destOffset--] = (byte)(col.r * 0.299 + col.g * 0.587 + col.b * 0.114);
				}
			}

			// assign the byte array to Mat
			return new CvMat( texHeight, texWidth, MatrixType.U8C1, outCopy, false );
		}

		static public void CopyMatToTexture2D( CvMat mat, Texture2D outTexture )
		{
			outTexture.SetPixels32( MatToPixels32( mat ) );
			// outTexture.UpdateExternalTexture(); // this could be useful for optimization
		}

		// Assign CvMat array to Unity Texture2D object; automatically distinguishes 8bit and 24bit CvMat types
		// NOTE : provide an array of equal or bigger size to avoid creating a new copy every time
		static public Color32[] MatToPixels32( CvMat mat, Color32[] outCopy = null )
		{
			int texWidth = mat.Width;
			int texHeight = mat.Height;
			int texArea = texHeight * texWidth;

			// create Color32 array that can be assigned to Texture2D directly
			if (outCopy == null || outCopy.GetLength(0) < texArea )
				outCopy = new Color32[texArea];

			int destOffset = texArea - 1;

			switch (mat.ElemType)
			{
				case MatrixType.U8C3: // rgb 24bit
					unsafe
					{
						byte* srcArray = mat.DataByte;
						int srcOffset = texArea * 3 - 1;
						while (srcOffset >= 0)
						{
							outCopy[destOffset--] =
								new Color32(
									srcArray[srcOffset--],
									srcArray[srcOffset--],
									srcArray[srcOffset--],
									1
								);
						}
					}
					break;

				case MatrixType.U8C1: // grey 8bit
					unsafe
					{
						byte* srcArray = mat.DataByte;
						int srcOffset = destOffset;
						while (srcOffset >= 0)
						{
							byte shade = srcArray[srcOffset--];
							outCopy[destOffset--] = new Color32( shade, shade, shade, 1 );
						}
					}
					break;
			}

			return outCopy;
		}

		static private int WindowCounter = 0;
		static public void NewWindowShow( CvArr imageToShow, string windowName = null)
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
