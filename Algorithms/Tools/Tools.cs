using Emgu.CV;
using Emgu.CV.Structure;
using System.Windows.Forms;
using System.Windows;
using System.Drawing;
using System;
using Algorithms.Utilities;
using System.Linq;
using System.Threading.Tasks;
namespace Algorithms.Tools
{
    public class Tools
    {
        #region Copy
        public static Image<Gray, byte> Copy(Image<Gray, byte> inputImage)
        {
            Image<Gray, byte> result = inputImage.Clone();
            return result;
        }

        public static Image<Bgr, byte> Copy(Image<Bgr, byte> inputImage)
        {
            Image<Bgr, byte> result = inputImage.Clone();
            return result;
        }
        #endregion

        #region Invert
        public static Image<Gray, byte> Invert(Image<Gray, byte> inputImage)
        {
            Image<Gray, byte> result = new Image<Gray, byte>(inputImage.Size);
            for (int y = 0; y < inputImage.Height; y++)
            {
                for (int x = 0; x < inputImage.Width; x++)
                {
                    result.Data[y, x, 0] = (byte)(255 - inputImage.Data[y, x, 0]);
                }
            }
            return result;
        }

        public static Image<Bgr, byte> Invert(Image<Bgr, byte> inputImage)
        {
            Image<Bgr, byte> result = new Image<Bgr, byte>(inputImage.Size);
            for (int y = 0; y < inputImage.Height; y++)
            {
                for (int x = 0; x < inputImage.Width; x++)
                {
                    result.Data[y, x, 0] = (byte)(255 - inputImage.Data[y, x, 0]);
                    result.Data[y, x, 1] = (byte)(255 - inputImage.Data[y, x, 1]);
                    result.Data[y, x, 2] = (byte)(255 - inputImage.Data[y, x, 2]);
                }
            }
            return result;
        }
        #endregion

        #region Convert color image to grayscale image
        public static Image<Gray, byte> Convert(Image<Bgr, byte> inputImage)
        {
            Image<Gray, byte> result = inputImage.Convert<Gray, byte>();
            return result;
        }
        #endregion

        #region Binary
        public static Image<Gray, byte> Binary(Image<Gray, byte> inputImage, double threshold)
        {
            Image<Gray, byte> result = new Image<Gray, byte>(inputImage.Size);
            for (int y = 0; y < inputImage.Height; ++y)
            {
                for (int x = 0; x < inputImage.Width; ++x)
                {
                    if (inputImage.Data[y, x, 0] > threshold) result.Data[y, x, 0] = 255;
                }
            }
            return result;
        }
        #endregion

        #region Adaptive Binary
        public static Image<Gray, byte> AdaptiveBinary(Image<Gray, byte> inputImage, int windowDimension)
        {
            Image<Gray, byte> result = new Image<Gray, byte>(inputImage.Size);
            Image<Gray, double> integralImage = Utils.CalculateIntegralImage(inputImage);

            int halfDimension = windowDimension / 2;
            for (int y = 0; y < inputImage.Height; y++)
            {
                for (int x = 0; x < inputImage.Width; x++)
                {
                    int x0 = Math.Max(x - halfDimension - 1, 0);
                    int y0 = Math.Max(y - halfDimension - 1, 0);
                    int x1 = Math.Min(x + halfDimension, integralImage.Width - 1);
                    int y1 = Math.Min(y + halfDimension, integralImage.Height - 1);

                    double sum = 0;
                    if (x0 == 0 && y0 == 0)
                    {
                        sum = integralImage.Data[y1, x1, 0];
                    }
                    else if (y0 == 0)
                    {
                        sum = integralImage.Data[y1, x1, 0] - integralImage.Data[y1, x0 - 1, 0];
                    }
                    else if (x0 == 0)
                    {
                        sum = integralImage.Data[y1, x1, 0] - integralImage.Data[y0 - 1, x1, 0];
                    }
                    else
                    {
                        sum = integralImage.Data[y1, x1, 0] + integralImage.Data[y0 - 1, x0 - 1, 0] -
                            integralImage.Data[y1, x0 - 1, 0] - integralImage.Data[y0 - 1, x1, 0];
                    }

                    double mean = sum / (windowDimension * windowDimension);
                    double threshold = mean * Utils.B;
                    if (inputImage.Data[y, x, 0] >= threshold)
                        result.Data[y, x, 0] = 255;
                    else
                        result.Data[y, x, 0] = 0;
                }
            }

            return result;
        }
        #endregion

        #region GammaCorrection
        public static Image<TColor, TDepth> GammaCorrection<TColor, TDepth>(
        Image<TColor, TDepth> inputImage,
        double gammaValue)
        where TColor : struct, IColor
        where TDepth : struct
        {
            Image<TColor, TDepth> result = new Image<TColor, TDepth>(inputImage.Width, inputImage.Height);
            for (int i = 0; i < inputImage.Height; i++)
            {
                for (int j = 0; j < inputImage.Width; j++)
                {
                    if (typeof(TColor) == typeof(Rgb) || typeof(TColor) == typeof(Bgr))
                    {

                        for (int k = 0; k < 3; k++)
                        {
                            //result.Data[i, j, k] = (TDepth)(object)(byte)(Math.Pow((double)(dynamic)inputImage.Data[i, j, k] / 255.0, 1.0 / gammaValue) * 255);
                            // Get the original pixel value as a double
                            double pixelValue = (double)(dynamic)inputImage.Data[i, j, k];

                            // Apply gamma correction
                            double correctedValue = Math.Pow(pixelValue / 255.0, 1.0 / gammaValue) * 255;

                            // Clamp the value to ensure it remains in the valid range
                            correctedValue = Math.Max(0, Math.Min(255, correctedValue));

                            // Assign the corrected value, converting back to TDepth
                            result.Data[i, j, k] = (TDepth)(object)(byte)correctedValue;
                        }
                    }



                    else
                    { //grayscale
                        result.Data[i, j, 0] = (TDepth)(object)(byte)(Math.Pow((double)(dynamic)inputImage.Data[i, j, 0] / 255.0, 1.0 / gammaValue) * 255);
                    }
                }
            }
            return result;
        }
        #endregion

        #region Crop
        public static Image<TColor, TDepth> Crop<TColor, TDepth>(
    Image<TColor, TDepth> inputImage,
    Point firstMousePos,
    Point lastMousePos)
    where TColor : struct, IColor
    where TDepth : struct
        {
            // Determine leftTop and rightBottom points based on mouse positions
            Point leftTop = new Point(
                Math.Min(firstMousePos.X, lastMousePos.X),
                Math.Min(firstMousePos.Y, lastMousePos.Y)
            );
            Point rightBottom = new Point(
                Math.Max(firstMousePos.X, lastMousePos.X),
                Math.Max(firstMousePos.Y, lastMousePos.Y)
            );

            // Calculate crop width and height
            int cropWidth = rightBottom.X - leftTop.X;
            int cropHeight = rightBottom.Y - leftTop.Y;

            // Validate crop area dimensions
            if (cropWidth <= 0 || cropHeight <= 0 ||
                leftTop.X < 0 || leftTop.Y < 0 ||
                rightBottom.X > inputImage.Width || rightBottom.Y > inputImage.Height)
            {
                throw new ArgumentException("Invalid crop boundaries.");
            }

            // Set the ROI
            inputImage.ROI = new Rectangle(leftTop.X, leftTop.Y, cropWidth, cropHeight);



            // Create a new instance of the cropped image
            Image<TColor, TDepth> result = new Image<TColor, TDepth>(cropWidth, cropHeight);
            inputImage.CopyTo(result);  // Copy the cropped region to the result

            // Reset the ROI
            inputImage.ROI = Rectangle.Empty;

            //Display mean and rms
            (double mean, double rms) = CalculateMeanAndRMS(inputImage,
                new Rectangle(leftTop.X, leftTop.Y, cropWidth, cropHeight));

            MessageBox.Show($"Media: {mean}\nAbaterea Medie Pătratică: {rms}", "Rezultate");
            return result;
        }
        #endregion

        #region CalculateMeanAndRMS
        public static (double mean, double rms) CalculateMeanAndRMS<TColor, TDepth>(
       Image<TColor, TDepth> inputImage, Rectangle cropArea)
       where TColor : struct, IColor
       where TDepth : struct
        {
            // Set the ROI (Region of Interest) to the specified crop area
            inputImage.ROI = cropArea;

            double sum = 0;
            double sumSquared = 0;
            int pixelCount = cropArea.Width * cropArea.Height;

            // Iterate through the pixels in the cropped area
            for (int y = 0; y < cropArea.Height; y++)
            {
                for (int x = 0; x < cropArea.Width; x++)
                {
                    // Get pixel value
                    TColor pixelValue = inputImage[y, x];

                    // Calculate intensity based on color type
                    double intensity;
                    if (typeof(TColor) == typeof(Bgr))
                    {
                        var bgr = (Bgr)(object)pixelValue; // Cast to Bgr
                        intensity = 0.114 * bgr.Blue + 0.587 * bgr.Green + 0.299 * bgr.Red; // Calculate intensity
                    }
                    else if (typeof(TColor) == typeof(Rgb))
                    {
                        var rgb = (Rgb)(object)pixelValue; // Cast to Rgb
                        intensity = 0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue; // Calculate intensity
                    }
                    else if (typeof(TColor) == typeof(Gray))
                    {
                        var gray = (Gray)(object)pixelValue; // Cast to Gray
                        intensity = gray.Intensity; // Get intensity directly for Gray
                    }
                    else
                    {
                        throw new InvalidOperationException("Unsupported color type");
                    }

                    sum += intensity; // Accumulate the sum
                    sumSquared += intensity * intensity; // Accumulate the squared sum
                }
            }

            // Calculate the mean
            double mean = sum / pixelCount;

            // Calculate the RMS
            double meanSquare = sumSquared / pixelCount;
            double rms = Math.Sqrt(meanSquare - (mean * mean));

            // Reset ROI
            inputImage.ROI = Rectangle.Empty;

            return (mean, rms);
        }
        #endregion

        #region Median Filter

        public static Image<TColor, byte> MedianFilter<TColor>(
        Image<TColor, byte> inputImage, int windowSize, bool parallel)
        where TColor : struct, IColor
        {
            Image<TColor, byte> result = new Image<TColor, byte>(inputImage.Size);

            if (!parallel)
            {
                foreach (int y in Enumerable.Range(0, inputImage.Height - 1))
                {
                    for (int x = 0; x < inputImage.Width; x++)
                    {
                        Utils.ApplyMedianFilterAtPixel(inputImage, result, x, y, windowSize);
                    }
                }
            }
            else
            {
                Parallel.ForEach(Enumerable.Range(0, inputImage.Height - 1), y =>
                {
                    for (int x = 0; x < inputImage.Width; x++)
                    {
                        Utils.ApplyMedianFilterAtPixel(inputImage, result, x, y, windowSize);
                    }
                });
            }


            return result;

        }

        #endregion

        #region Detect Horizontal Edges (RGB)

        public static Image<Bgr, byte> DetectHorizontalEdgesRGB(Image<Bgr, byte> inputImage, double threshold)
        {
            // convert to double for safe computation
            var img = inputImage.Convert<Bgr, double>();

            int width = img.Width;
            int height = img.Height;

            Image<Bgr, byte> result = new Image<Bgr, byte>(width, height);

            // cimple sobel kernels for X and Y direction
            // sobel X
            double[,] sobelX = new double[,]
            {
                {-1, 0, 1},
                {-2, 0, 2},
                {-1, 0, 1}
            };
            // sobel Y
            double[,] sobelY = new double[,]
            {
                {-1, -2, -1},
                { 0,  0,  0},
                { 1,  2,  1}
            };

            // ignore boundary pixels for simplicity
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    // compute gradients for each channel
                    double GxR = 0, GyR = 0;
                    double GxG = 0, GyG = 0;
                    double GxB = 0, GyB = 0;

                    for (int j = -1; j <= 1; j++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            var pixel = img[y + j, x + i];
                            double R = pixel.Red;
                            double G = pixel.Green;
                            double B = pixel.Blue;

                            GxR += R * sobelX[j + 1, i + 1];
                            GyR += R * sobelY[j + 1, i + 1];

                            GxG += G * sobelX[j + 1, i + 1];
                            GyG += G * sobelY[j + 1, i + 1];

                            GxB += B * sobelX[j + 1, i + 1];
                            GyB += B * sobelY[j + 1, i + 1];
                        }
                    }

                    double f_xx = GxR * GxR + GxG * GxG + GxB * GxB;
                    double f_yy = GyR * GyR + GyG * GyG + GyB * GyB;
                    double f_xy = GxR * GyR + GxG * GyG + GxB * GyB;

                    double lambda1 = 0.5 * (f_xx + f_yy + 
                                            Math.Sqrt((f_xx - f_yy) * (f_xx - f_yy) + 4 * f_xy * f_xy));

                    double maxVariation = Math.Sqrt(lambda1);

                    bool isHorizontalEdge = maxVariation > threshold;

                    if (isHorizontalEdge)
                    {
                        // mark the edge as white
                        result.Data[y, x, 0] = 255;
                        result.Data[y, x, 1] = 255;
                        result.Data[y, x, 2] = 255; // Red channel to mark edges
                    }
                    else // if (!isHorizontalEdge)
                    {
                        // mark the edge as black
                        result.Data[y, x, 0] = 0;
                        result.Data[y, x, 1] = 0;
                        result.Data[y, x, 2] = 0;
                    }
                }
            }

            return result;
        }

        #endregion
    }

}