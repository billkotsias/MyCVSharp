using OpenCvSharp;

namespace MyCVSharp
{
	static public partial class Filters
	{
		// NOTE : "mask" is considered to be all white
		// => image must be 8bit greyscale
		// <= returns an int
		static public int NeighborhoodValleyEmphasis(CvMat image)
		{
			int i, r, c, g, t;
			double temp;

			double[] gray_valueV = new double[256]; // initiallized to 0
			double[] p0t = new double[256]; // initiallized to 0
			double[] p1t = new double[256]; // initiallized to 0
			double[] m0t = new double[256]; // initiallized to 0
			double[] m1t = new double[256]; // initiallized to 0

			// NOTE : removed loop with
			for (r = image.Rows - 1; r >= 0; --r) {
				for (c = image.Cols - 1; c >= 0; --c) {
					// mask is all white, removed check: "if (mask.at<uchar>( r, c ) == 255)"
					++gray_valueV[(int)image.GetReal2D( r, c )];
				}
			}
			int numOfPixels = image.Rows * image.Cols;
			for (r = gray_valueV.Length - 1; r >= 0 ; --r)
			{
				gray_valueV[ r ] /= numOfPixels; //The probability of occurrence of gray level i
			}

			// We will find p0(t) and p1(t) for each gray level probability of the 2 classes
			for (t = 0; t < 256; t++)
			{
				for (g = 0; g < t; g++)
					p0t[t] += gray_valueV[g];

				for (g = t; g < 256; g++)
					p1t[t] += gray_valueV[g];
			}

			for (t = 0; t < 256; t++)
			{
				for (g = 0; g < t; g++)
				{
					if (p0t[t] != 0)
						m0t[t] += g * gray_valueV[g] / p0t[t];
				}
				for (g = t; g < 256; g++)
				{
					if (p1t[t] != 0)
						m1t[t] += g * gray_valueV[g] / p1t[t];
				}
			}


			double[] sigma_b2 = new double[256];
			for (t = 0; t < 256; t++)
			{
				sigma_b2[t] = ( p0t[t] * m0t[t] * m0t[t] + p1t[t] * m1t[t] * m1t[t] );
			}


			double[] neighborhood_valV = new double[256];
			i = 11; // how's "11" chosen?
			for (t = 0; t < i; t++)
			{
				temp = 0;
				for (g = 1; g <= i; g++) //The i values greater than t
					temp += gray_valueV[g + t];
				for (g = 0; g <= i; g++) //The first i values
					temp += gray_valueV[g];

				neighborhood_valV[t] = temp;
			}
			for (t = i; t < 256 - i; t++)
			{
				temp = 0;
				for (g = 1; g < i; g++)
				{
					temp += gray_valueV[t + g];
					temp += gray_valueV[t - g];
				}
				temp += gray_valueV[t];

				neighborhood_valV[t] = temp;
			}
			for (t = 256 - i; t < 256; t++)
			{
				temp = 0;
				for (g = 1; g <= i; g++) //The i values less than t
					temp += gray_valueV[t - g];
				for (g = 0; g <= i; g++) //The last i values
					temp += gray_valueV[255 - g];

				neighborhood_valV[t] = temp;
			}

			// t is the OTSU threshold
			// No need to sort values, just keep index of max OTSU value, that's all! was: "std::map<double, int, std::greater<double>> threshM;"
			double maxValue = double.NegativeInfinity;
			int maxValueIndex = -1;
			for (t = 0; t < 256; ++t)
			{
				double newValue = (1 - neighborhood_valV[t]) * sigma_b2[t];
				if (newValue > maxValue)
				{
					maxValue = newValue;
					maxValueIndex = t;
				}
			}

			//t is the Neighborhood Valley-emphasis method threshold
			return maxValueIndex;
		}
	}
}
