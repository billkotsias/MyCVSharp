using OpenCvSharp;
using System.Collections.Generic;
using System;

namespace MyCVSharp
{
	static public partial class Filters
	{
		// this may not be as super-accurate as the other one (although it might just be that as well), but it's 17 (!!!) times faster (measured!)
		// => frame = 8 bit greyscale CvMat
		static public void ContrastEnhancement2(CvMat frame)
		{
			unsafe
			{
				int dataSize = frame.Cols * frame.Rows;
				byte* data = frame.DataByte;
				// find min,max
				byte min = 255, max = 0;
				for (int i = 0; i < dataSize; ++i)
				{
					byte currentValue = data[i];
					if (min > currentValue) min = currentValue;
					if (max < currentValue) max = currentValue;
				}

				float gain = 255f / (max - min);
				if (gain == 1f)
				{
					Console.Out.WriteLine( "ContrastEnhancement2: cannot enhance" );
					return;
				} else
				{
					Console.Out.WriteLine( "ContrastEnhancement2: enhancing by : {0}", gain );
				}

				// apply auto-gain
				for (int i = 0; i < dataSize; ++i)
				{
					data[i] = (byte)((data[i] - min) * gain);
				}
			}
		}

		// NOTE : Also seems not well written and craves optimization at places. P.A.N.A.R.G.O.
		// => frame = 8 bit greyscale CvMat
		static public void ContrastEnhancement(CvMat frame)
		{
			//CvMat originalFrame = frame; // return this if cannot enhance
			//if (frame.ElemType != MatrixType.U8C1)
			//	frame = MatOps.Convert(frame, MatrixType.U8C1, 1 / 255.0 );

			/////original histogram
			const int HistBinSize = 256;
			int[] histSizes = new int[1];
			histSizes[0] = HistBinSize;
			CvHistogram hist = new CvHistogram( histSizes, HistogramFormat.Array );
			Cv.CalcArrHist( frame, hist, false ); // size = 256 implied

			CvHistogram newHist = MatOps.CopyHistogram( hist );
			CvArr newHistBin = newHist.Bins;

			//double[] origVals = new double[hist.Bins.GetDims( 0 )];
			List<double> origVals = new List<double>( HistBinSize );
			for (int i = 0; i < HistBinSize; i++)
			{
				double elem = newHistBin.GetReal1D( i );
				if (elem != 0)
					origVals.Add( elem );
			}

			// FIX : See no need for histL, since we have origVals
			//////histogram with only nonzero bins
			//CvMat histL = new CvMat( imageRows, imageCols, MatrixType.F32C1, new CvScalar( 0 ) );
			//for (i = 0; i < origVals.size(); i++)
			//	histL.at<float>( i, 0 ) = origVals.at( i );

			List<double> peakValues = new List<double>( HistBinSize ); //std::vector<int> peakValues;

			//////////3 bin search window
			for (int i = 1; i < origVals.Count - 2; ++i)
			{
				double elem = origVals[i];
				if (elem > origVals[i - 1] && elem > origVals[i + 1])
				{
					peakValues.Add( elem );
				}
			}

			if (peakValues.Count == 0)
			{
				//Console.Out.WriteLine( "Cannot enhance" );
				return; // cannot enhance?
			}

			//////Upper threshold
			double threshUP = 0;
			for (int i = 0; i < peakValues.Count; ++i)
			{
				threshUP += peakValues[i];
			}
			threshUP /= peakValues.Count;

			//////Lower threshold
			double threshDOWN = Math.Min( (frame.Cols * frame.Rows), threshUP * origVals.Count ) / 256.0;
			//Console.Out.WriteLine( "Enhance thresholds " + threshUP + "/" + threshDOWN );

			//////histogram reconstruction
			CvArr histBins = hist.Bins;
			for (int i = 0; i < HistBinSize; ++i)
			{
				double histElem = histBins.GetReal1D( i );
				if (histElem > threshUP)
				{
					histBins.SetReal1D( i, threshUP );
				}
				else if (histElem <= threshUP && histElem >= threshDOWN)
				{
					continue;
				}
				else if (histElem < threshDOWN && histElem > 0)
				{
					histBins.SetReal1D( i, threshDOWN );
				}
				else if (histElem == 0)
				{
					continue;
				}
			}
			// accumulated values(?)
			double[] accVals = new double[HistBinSize]; //std::vector<int> accVals;
			accVals[0] = ( histBins.GetReal1D( 0 ) );
			for (int i = 1; i < HistBinSize; ++i)
			{
				accVals[i] = (accVals[i - 1] + histBins[i]);
			}

			byte[] lookUpTable = new byte[HistBinSize]; //cv::Mat lookUpTable = cv::Mat::zeros( hist.size(), CV_8UC1 );
			for (int i = 0; i < HistBinSize; ++i)
			{
				lookUpTable[i] = (byte)(255.0 * accVals[i] / accVals[255]);
			}

			// assign computed values to input frame
			//Console.Out.Write( "Enhance-->" );
			for (int i = 0; i < frame.Cols; ++i)
			{
				for (int j = 0; j < frame.Rows; ++j)
				{
					// there is NO mask, thus no need to check for; was: "if (mask.data)..."
					byte oldValue = (byte)frame.Get2D( j, i );
					byte newValue = lookUpTable[oldValue];
					//if ((newValue <1 || newValue > 254) && (newValue != oldValue)) Console.Out.Write( oldValue + " " + newValue + "|");
					frame.Set2D( j, i, newValue );
					//frame.SetReal2D( j, i, lookUpTable[ (int)(255.0 * frame.GetReal2D( j, i )) ] / 255.0);
				}
			}
			//Console.Out.WriteLine();

			//frame = MatOps.Convert( frame, MatrixType.U8C1, 255.0 );
		}
	}
}
