using OpenCvSharp;
using System;

namespace MyCVSharp
{
	public partial class Filters
	{
		// This is a class that does floodfill inside a CvMat
		// I *thought* the CvMat.FloodFill function was broken, but it seems it's OK (check MatOps.GetAreaOfSimilarPixels)
		// so, I haven't used this or even checked if it works. Doesn't harm to leave it here, though.
		public abstract class AbstractFloodFiller
		{
			protected CvMat bitmap;
			protected byte[] tolerance = new byte[] { 25, 25, 25 };
			protected CvScalar fillColor = Const.ScalarMagenta;
			protected bool fillDiagonally = false;
			protected bool slow = false;

			//cached bitmap properties
			protected int bitmapWidth = 0;
			protected int bitmapHeight = 0;
			protected int bitmapStride = 0;
			protected int bitmapPixelFormatSize = 0;

			//internal, initialized per fill
			//protected BitArray pixelsChecked;
			protected bool[] pixelsChecked;
			protected byte[] byteFillColor;
			protected byte[] startColor;
			//protected int stride;

			public AbstractFloodFiller()
			{
			}

			public AbstractFloodFiller( AbstractFloodFiller configSource )
			{
				if (configSource != null)
				{
					this.Bitmap = configSource.Bitmap;
					this.FillColor = configSource.FillColor;
					this.FillDiagonally = configSource.FillDiagonally;
					this.Slow = configSource.Slow;
					this.Tolerance = configSource.Tolerance;
				}
			}

			public bool Slow
			{
				get { return slow; }
				set { slow = value; }
			}

			public CvScalar FillColor
			{
				get { return fillColor; }
				set { fillColor = value; }
			}

			public bool FillDiagonally
			{
				get { return fillDiagonally; }
				set { fillDiagonally = value; }
			}

			public byte[] Tolerance
			{
				get { return tolerance; }
				set { tolerance = value; }
			}

			public CvMat Bitmap
			{
				get { return bitmap; }
				set
				{
					bitmap = value;
				}
			}

			public abstract void FloodFill( CvPoint pt );

			protected void PrepareForFloodFill()
			{
				//cache data in member variables to decrease overhead of property calls
				//this is especially important with Width and Height, as they call
				//GdipGetImageWidth() and GdipGetImageHeight() respectively in gdiplus.dll - 
				//which means major overhead.
				byteFillColor = new byte[] { (byte)fillColor.Val0, (byte)fillColor.Val1, (byte)fillColor.Val2 };
				// PixelFormatSize == bytes / pixel
				switch (bitmap.ElemType)
				{
					case MatrixType.U8C1:
						bitmapPixelFormatSize = 1;
						break;
					case MatrixType.U8C2:
						bitmapPixelFormatSize = 2;
						break;
					case MatrixType.U8C3:
						bitmapPixelFormatSize = 3;
						break;
					case MatrixType.U8C4:
						bitmapPixelFormatSize = 4;
						break;
					default:
						bitmapPixelFormatSize = 0;
						break;
				}
				bitmapStride = bitmap.Cols * bitmapPixelFormatSize; // stride = width * pixelFormatSize;
				bitmapWidth = bitmap.Cols;
				bitmapHeight = bitmap.Rows;

				pixelsChecked = new bool[bitmapWidth * bitmapHeight];
			}
		}

		public class UnsafeQueueLinearFloodFiller : AbstractFloodFiller
		{
			protected unsafe byte* scan0;
			FloodFillRangeQueue ranges = new FloodFillRangeQueue();

			public UnsafeQueueLinearFloodFiller( AbstractFloodFiller configSource ) : base( configSource ) { }

			public override void FloodFill( CvPoint pt )
			{
				PrepareForFloodFill();

				unsafe
				{
					scan0 = (byte*)bitmap.DataByte;
					int x = pt.X; int y = pt.Y;
					int loc = CoordsToIndex( ref x, ref y );
					byte* colorPtr = ((byte*)(scan0 + loc));
					startColor = new byte[] { colorPtr[0], colorPtr[1], colorPtr[2] };
					LinearFloodFill4( ref x, ref y );

					bool[] pixelsChecked = this.pixelsChecked;

					while (ranges.Count > 0)
					{
						FloodFillRange range = ranges.Dequeue();

						//START THE LOOP UPWARDS AND DOWNWARDS
						int upY = range.Y - 1;//so we can pass the y coord by ref
						int downY = range.Y + 1;
						byte* upPtr = (byte*)(scan0 + CoordsToIndex( ref range.StartX, ref upY ));
						byte* downPtr = (byte*)(scan0 + CoordsToIndex( ref range.StartX, ref downY ));
						int downPxIdx = (bitmapWidth * (range.Y + 1)) + range.StartX;//CoordsToPixelIndex(range.StartX,range.Y+1);
						int upPxIdx = (bitmapWidth * (range.Y - 1)) + range.StartX;//CoordsToPixelIndex(range.StartX, range.Y - 1);
						for (int i = range.StartX; i <= range.EndX; i++)
						{
							//START LOOP UPWARDS
							//if we're not above the top of the bitmap and the pixel above this one is within the color tolerance
							if (range.Y > 0 && CheckPixel( ref upPtr ) && (!(pixelsChecked[upPxIdx])))
								LinearFloodFill4( ref i, ref upY );
							//START LOOP DOWNWARDS
							if (range.Y < (bitmapHeight - 1) && CheckPixel( ref downPtr ) && (!(pixelsChecked[downPxIdx])))
								LinearFloodFill4( ref i, ref downY );
							upPtr += bitmapPixelFormatSize;
							downPtr += bitmapPixelFormatSize;
							downPxIdx++;
							upPxIdx++;
						}
					}
				}
			}

			unsafe void LinearFloodFill4( ref int x, ref int y )
			{

				//offset the pointer to the point passed in
				byte* p = (byte*)(scan0 + (CoordsToIndex( ref x, ref y )));

				//cache some bitmap and fill info in local variables for a little extra speed
				bool[] pixelsChecked = this.pixelsChecked;
				byte[] byteFillColor = this.byteFillColor;
				int bitmapPixelFormatSize = this.bitmapPixelFormatSize;
				int bitmapWidth = this.bitmapWidth;

				//FIND LEFT EDGE OF COLOR AREA
				int lFillLoc = x; //the location to check/fill on the left
				byte* ptr = p; //the pointer to the current location
				int pxIdx = (bitmapWidth * y) + x;
				while (true)
				{
					ptr[0] = byteFillColor[0];   //fill with the color
					ptr[1] = byteFillColor[1];
					ptr[2] = byteFillColor[2];
					pixelsChecked[pxIdx] = true;
					lFillLoc--;              //de-increment counter
					ptr -= bitmapPixelFormatSize;                    //de-increment pointer
					pxIdx--;
					if (lFillLoc <= 0 || !CheckPixel( ref ptr ) || (pixelsChecked[pxIdx]))
						break;               //exit loop if we're at edge of bitmap or color area

				}
				lFillLoc++;

				//FIND RIGHT EDGE OF COLOR AREA
				int rFillLoc = x; //the location to check/fill on the left
				ptr = p;
				pxIdx = (bitmapWidth * y) + x;
				while (true)
				{
					ptr[0] = byteFillColor[0];   //fill with the color
					ptr[1] = byteFillColor[1];
					ptr[2] = byteFillColor[2];
					pixelsChecked[pxIdx] = true;
					rFillLoc++;          //increment counter
					ptr += bitmapPixelFormatSize;                //increment pointer
					pxIdx++;
					if (rFillLoc >= bitmapWidth || !CheckPixel( ref ptr ) || (pixelsChecked[pxIdx]))
						break;           //exit loop if we're at edge of bitmap or color area

				}
				rFillLoc--;

				FloodFillRange r = new FloodFillRange( lFillLoc, rFillLoc, y );
				ranges.Enqueue( ref r );

			}

			private unsafe bool CheckPixel( ref byte* px )
			{
				return
					px[0] >= (startColor[0] - tolerance[0]) && px[0] <= (startColor[0] + tolerance[0]) &&
					px[1] >= (startColor[1] - tolerance[1]) && px[1] <= (startColor[1] + tolerance[1]) &&
					px[2] >= (startColor[2] - tolerance[2]) && px[2] <= (startColor[2] + tolerance[2]);
			}

			private int CoordsToIndex( ref int x, ref int y )
			{
				return (bitmapStride * y) + (x * bitmapPixelFormatSize);
			}
		}

		//

		public struct FloodFillRange
		{
			public int StartX;
			public int EndX;
			public int Y;

			public FloodFillRange( int startX, int endX, int y )
			{
				StartX = startX;
				EndX = endX;
				Y = y;
			}
		}

		//

		public class FloodFillRangeQueue
		{
			FloodFillRange[] array;
			int size;
			int head;

			/// <summary>
			/// Returns the number of items currently in the queue.
			/// </summary>
			public int Count
			{
				get { return size; }
			}

			public FloodFillRangeQueue() : this( 10000 )
			{

			}

			public FloodFillRangeQueue( int initialSize )
			{
				array = new FloodFillRange[initialSize];
				head = 0;
				size = 0;
			}

			/// <summary>Gets the <see cref="FloodFillRange"/> at the beginning of the queue.</summary>
			public FloodFillRange First
			{
				get { return array[head]; }
			}

			/// <summary>Adds a <see cref="FloodFillRange"/> to the end of the queue.</summary>
			public void Enqueue( ref FloodFillRange r )
			{
				if (size + head == array.Length)
				{
					FloodFillRange[] newArray = new FloodFillRange[2 * array.Length];
					Array.Copy( array, head, newArray, 0, size );
					array = newArray;
					head = 0;
				}
				array[head + (size++)] = r;
			}

			/// <summary>Removes and returns the <see cref="FloodFillRange"/> at the beginning of the queue.</summary>
			public FloodFillRange Dequeue()
			{
				FloodFillRange range = new FloodFillRange();
				if (size > 0)
				{
					range = array[head];
					array[head] = new FloodFillRange();
					head++;//advance head position
					size--;//update size to exclude dequeued item
				}
				return range;
			}
		}
	}
}
