using Emgu.CV;
using Emgu.CV.Structure;
using System.Windows;
using System.Drawing.Imaging;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Diagnostics;
using static System.Windows.MessageBox;


using Framework.View;
using static Framework.Utilities.DataProvider;
using static Framework.Utilities.FileHelper;
using static Framework.Utilities.DrawingHelper;
using static Framework.Converters.ImageConverter;

using Algorithms.Sections;
using Algorithms.Tools;
using Algorithms.Utilities;
using Framework.ViewModel;

using MatFileHandler;
using System.Windows.Forms;
using System.Media;
using System.Drawing;
using System;
using OpenTK.Graphics.ES11;
using Emgu.CV.CvEnum;
using Framework.Utilities;
using System.Collections.Generic;
using Emgu.CV.Cuda;

namespace Framework.ViewModel
{
    public class MenuCommands : BaseVM
    {
        private readonly MainVM _mainVM;

        public MenuCommands(MainVM mainVM)
        {
            _mainVM = mainVM;
        }

        private ImageSource InitialImage
        {
            get => _mainVM.InitialImage;
            set => _mainVM.InitialImage = value;
        }

        private ImageSource ProcessedImage
        {
            get => _mainVM.ProcessedImage;
            set => _mainVM.ProcessedImage = value;
        }

        private double ScaleValue
        {
            get => _mainVM.ScaleValue;
            set => _mainVM.ScaleValue = value;
        }

        #region File

        #region Load grayscale image
        private RelayCommand _loadGrayImageCommand;
        public RelayCommand LoadGrayImageCommand
        {
            get
            {
                if (_loadGrayImageCommand == null)
                    _loadGrayImageCommand = new RelayCommand(LoadGrayImage);
                return _loadGrayImageCommand;
            }
        }

        private void LoadGrayImage(object parameter)
        {
            Clear(parameter);

            string fileName = LoadFileDialog("Select a gray picture");
            if (fileName != null)
            {
                GrayInitialImage = new Image<Gray, byte>(fileName);
                InitialImage = Convert(GrayInitialImage);
            }
        }
        #endregion

        #region Load color image
        private ICommand _loadColorImageCommand;
        public ICommand LoadColorImageCommand
        {
            get
            {
                if (_loadColorImageCommand == null)
                    _loadColorImageCommand = new RelayCommand(LoadColorImage);
                return _loadColorImageCommand;
            }
        }

        private void LoadColorImage(object parameter)
        {
            Clear(parameter);

            string fileName = LoadFileDialog("Select a color picture");
            if (fileName != null)
            {
                ColorInitialImage = new Image<Bgr, byte>(fileName);
                InitialImage = Convert(ColorInitialImage);
            }
        }
        #endregion

        #region Save processed image
        private ICommand _saveProcessedImageCommand;
        public ICommand SaveProcessedImageCommand
        {
            get
            {
                if (_saveProcessedImageCommand == null)
                    _saveProcessedImageCommand = new RelayCommand(SaveProcessedImage);
                return _saveProcessedImageCommand;
            }
        }

        private void SaveProcessedImage(object parameter)
        {
            if (GrayProcessedImage == null && ColorProcessedImage == null)
            {
                System.Windows.MessageBox.Show("If you want to save your processed image, " +
                    "please load and process an image first!");
                return;
            }

            string imagePath = SaveFileDialog("image.jpg");
            if (imagePath != null)
            {
                GrayProcessedImage?.Bitmap.Save(imagePath, GetJpegCodec("image/jpeg"), GetEncoderParameter(Encoder.Quality, 100));
                ColorProcessedImage?.Bitmap.Save(imagePath, GetJpegCodec("image/jpeg"), GetEncoderParameter(Encoder.Quality, 100));
                OpenImage(imagePath);
            }
        }
        #endregion

        #region Load .mat image
        private ICommand _loadMatImageCommand;
        public ICommand LoadMatImageCommand
        {
            get
            {
                if (_loadMatImageCommand == null)
                    _loadMatImageCommand = new RelayCommand(LoadMatImage);
                return _loadMatImageCommand;
            }
        }

        private void LoadMatImage(object parameter)
        {
            Clear(parameter);

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Multispectral",
                Filter = "MAT-files (*.mat)|*.mat|All Files (*.*)|*.*",
                FilterIndex = 1,
                DefaultExt = "mat"
            };
            string fileName = null;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                fileName = openFileDialog.FileName;
            }
            else {
                return;
            }

            if (fileName != null)
            {
                IMatFile matFile;
                using (var fileStream = new System.IO.FileStream(fileName, System.IO.FileMode.Open))
                {
                    var reader = new MatFileReader(fileStream);
                    matFile = reader.Read();
                    var imageMatrix = matFile.Variables[0].Value.ConvertToDoubleArray();

                //return (z * xMax * yMax) + (y * xMax) + x;
               
                    int width = matFile.Variables[0].Value.Dimensions[0];
                    int height = matFile.Variables[0].Value.Dimensions[1];
                    int depth = matFile.Variables[0].Value.Dimensions[2];

                    double maxR = -999999999;
                    double minR = 9999999999;

                    double maxG = -999999999;
                    double minG = 9999999999;

                    double maxB = -999999999;
                    double minB = 9999999999;

                    var currentRGB = DataProvider.PaviaRGB;

                    for (int y = 0; y < height; y++) {
                        for (int x = 0; x < width; x++) {
                            if (imageMatrix[currentRGB.Item1 * width * height + y * width + x] > maxR)
                                maxR = imageMatrix[currentRGB.Item1 * width * height + y * width + x];
                            if (imageMatrix[currentRGB.Item1 * width * height + y * width + x] < minR)
                                minR = imageMatrix[currentRGB.Item1 * width * height + y * width + x];

                            if (imageMatrix[currentRGB.Item2 * width * height + y * width + x] > maxG)
                                maxG = imageMatrix[currentRGB.Item2 * width * height + y * width + x];
                            if (imageMatrix[currentRGB.Item2 * width * height + y * width + x] < minG)
                                minG = imageMatrix[currentRGB.Item2 * width * height + y * width + x];

                            if (imageMatrix[currentRGB.Item3 * width * height + y * width + x] > maxB)
                                maxB = imageMatrix[currentRGB.Item3 * width * height + y * width + x];
                            if (imageMatrix[currentRGB.Item3 * width * height + y * width + x] < minB)
                                minB = imageMatrix[currentRGB.Item3 * width * height + y * width + x];
                        }
                    }

                    //convert to bitmap
                    Bitmap bitmap = new Bitmap(width, height);
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int rawR = (int)(imageMatrix[currentRGB.Item1 * width * height + y * width + x] ); // Red channel
                            int rawG = (int)(imageMatrix[currentRGB.Item2 * width * height + y * width + x] ); // Green channel
                            int rawB = (int)(imageMatrix[currentRGB.Item3 * width * height + y * width + x] ); // Blue channel

                            // Clamp values to 0-255 range
                            int r = (int)(Math.Max(0, Math.Min(255, ((rawR - minR) / (maxR - minR)) * 255)));
                            int g = (int)(Math.Max(0, Math.Min(255, ((rawG - minG) / (maxG - minG)) * 255)));
                            int b = (int)(Math.Max(0, Math.Min(255, ((rawB - minB) / (maxB - minB)) * 255)));

                            // Set the pixel color in the bitmap
                            bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(r, g, b));
                        }
                    }
                    
                    ColorInitialImage = new Image<Bgr, byte>(bitmap);
                    InitialImage = Convert(bitmap);
                }
            }
        }
        #endregion

        #region Save both images
        private ICommand _saveImagesCommand;
        public ICommand SaveImagesCommand
        {
            get
            {
                if (_saveImagesCommand == null)
                    _saveImagesCommand = new RelayCommand(SaveImages);
                return _saveImagesCommand;
            }
        }

        private void SaveImages(object parameter)
        {
            if (GrayInitialImage == null && ColorInitialImage == null)
            {
                System.Windows.MessageBox.Show("If you want to save both images, " +
                    "please load and process an image first!");
                return;
            }

            if (GrayProcessedImage == null && ColorProcessedImage == null)
            {
                System.Windows.MessageBox.Show("If you want to save both images, " +
                    "please process your image first!");
                return;
            }

            string imagePath = SaveFileDialog("image.jpg");
            if (imagePath != null)
            {
                IImage processedImage = null;
                if (GrayInitialImage != null && GrayProcessedImage != null)
                    processedImage = Utils.Merge(GrayInitialImage, GrayProcessedImage);

                if (GrayInitialImage != null && ColorProcessedImage != null)
                    processedImage = Utils.Merge(GrayInitialImage, ColorProcessedImage);

                if (ColorInitialImage != null && GrayProcessedImage != null)
                    processedImage = Utils.Merge(ColorInitialImage, GrayProcessedImage);

                if (ColorInitialImage != null && ColorProcessedImage != null)
                    processedImage = Utils.Merge(ColorInitialImage, ColorProcessedImage);

                processedImage?.Bitmap.Save(imagePath, GetJpegCodec("image/jpeg"), GetEncoderParameter(Encoder.Quality, 100));
                OpenImage(imagePath);
            }
        }
        #endregion

        #region Exit
        private ICommand _exitCommand;
        public ICommand ExitCommand
        {
            get
            {
                if (_exitCommand == null)
                    _exitCommand = new RelayCommand(Exit);
                return _exitCommand;
            }
        }

        private void Exit(object parameter)
        {
            CloseWindows();
            System.Environment.Exit(0);
        }
        #endregion

        #endregion

        #region Edit

        #region Remove drawn shapes from initial canvas
        private ICommand _removeInitialDrawnShapesCommand;
        public ICommand RemoveInitialDrawnShapesCommand
        {
            get
            {
                if (_removeInitialDrawnShapesCommand == null)
                    _removeInitialDrawnShapesCommand = new RelayCommand(RemoveInitialDrawnShapes);
                return _removeInitialDrawnShapesCommand;
            }
        }

        private void RemoveInitialDrawnShapes(object parameter)
        {
            RemoveUiElements(parameter as Canvas);
        }
        #endregion

        #region Remove drawn shapes from processed canvas
        private ICommand _removeProcessedDrawnShapesCommand;
        public ICommand RemoveProcessedDrawnShapesCommand
        {
            get
            {
                if (_removeProcessedDrawnShapesCommand == null)
                    _removeProcessedDrawnShapesCommand = new RelayCommand(RemoveProcessedDrawnShapes);
                return _removeProcessedDrawnShapesCommand;
            }
        }

        private void RemoveProcessedDrawnShapes(object parameter)
        {
            RemoveUiElements(parameter as Canvas);
        }
        #endregion

        #region Remove drawn shapes from both canvases
        private ICommand _removeDrawnShapesCommand;
        public ICommand RemoveDrawnShapesCommand
        {
            get
            {
                if (_removeDrawnShapesCommand == null)
                    _removeDrawnShapesCommand = new RelayCommand(RemoveDrawnShapes);
                return _removeDrawnShapesCommand;
            }
        }

        private void RemoveDrawnShapes(object parameter)
        {
            var canvases = (object[])parameter;
            RemoveUiElements(canvases[0] as Canvas);
            RemoveUiElements(canvases[1] as Canvas);
        }
        #endregion

        #region Clear initial canvas
        private ICommand _clearInitialCanvasCommand;
        public ICommand ClearInitialCanvasCommand
        {
            get
            {
                if (_clearInitialCanvasCommand == null)
                    _clearInitialCanvasCommand = new RelayCommand(ClearInitialCanvas);
                return _clearInitialCanvasCommand;
            }
        }

        private void ClearInitialCanvas(object parameter)
        {
            RemoveUiElements(parameter as Canvas);

            GrayInitialImage = null;
            ColorInitialImage = null;
            InitialImage = null;
        }
        #endregion

        #region Clear processed canvas
        private ICommand _clearProcessedCanvasCommand;
        public ICommand ClearProcessedCanvasCommand
        {
            get
            {
                if (_clearProcessedCanvasCommand == null)
                    _clearProcessedCanvasCommand = new RelayCommand(ClearProcessedCanvas);
                return _clearProcessedCanvasCommand;
            }
        }

        private void ClearProcessedCanvas(object parameter)
        {
            RemoveUiElements(parameter as Canvas);

            GrayProcessedImage = null;
            ColorProcessedImage = null;
            ProcessedImage = null;
        }
        #endregion

        #region Closing all open windows and clear both canvases
        private ICommand _clearCommand;
        public ICommand ClearCommand
        {
            get
            {
                if (_clearCommand == null)
                    _clearCommand = new RelayCommand(Clear);
                return _clearCommand;
            }
        }

        private void Clear(object parameter)
        {
            CloseWindows();

            ScaleValue = 1;

            var canvases = (object[])parameter;
            ClearInitialCanvas(canvases[0] as Canvas);
            ClearProcessedCanvas(canvases[1] as Canvas);
        }
        #endregion

        #endregion

        #region Tools

        #region Magnifier
        private ICommand _magnifierCommand;
        public ICommand MagnifierCommand
        {
            get
            {
                if (_magnifierCommand == null)
                    _magnifierCommand = new RelayCommand(Magnifier);
                return _magnifierCommand;
            }
        }

        private void Magnifier(object parameter)
        {
            if (MagnifierOn == true) return;
            if (VectorOfMousePosition.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select an area first.");
                return;
            }

            MagnifierWindow magnifierWindow = new MagnifierWindow();
            magnifierWindow.Show();
        }
        #endregion

        #region Display Gray/Color levels

        #region On row
        private ICommand _displayLevelsOnRowCommand;
        public ICommand DisplayLevelsOnRowCommand
        {
            get
            {
                if (_displayLevelsOnRowCommand == null)
                    _displayLevelsOnRowCommand = new RelayCommand(DisplayLevelsOnRow);
                return _displayLevelsOnRowCommand;
            }
        }

        private void DisplayLevelsOnRow(object parameter)
        {
            if (RowColorLevelsOn == true) return;
            if (VectorOfMousePosition.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select an area first.");
                return;
            }

            ColorLevelsWindow window = new ColorLevelsWindow(_mainVM, CLevelsType.Row);
            window.Show();
        }
        #endregion

        #region On column
        private ICommand _displayLevelsOnColumnCommand;
        public ICommand DisplayLevelsOnColumnCommand
        {
            get
            {
                if (_displayLevelsOnColumnCommand == null)
                    _displayLevelsOnColumnCommand = new RelayCommand(DisplayLevelsOnColumn);
                return _displayLevelsOnColumnCommand;
            }
        }

        private void DisplayLevelsOnColumn(object parameter)
        {
            if (ColumnColorLevelsOn == true) return;
            if (VectorOfMousePosition.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select an area first.");
                return;
            }

            ColorLevelsWindow window = new ColorLevelsWindow(_mainVM, CLevelsType.Column);
            window.Show();
        }
        #endregion

        #endregion

        #region Visualize image histogram

        #region Initial image histogram
        private ICommand _histogramInitialImageCommand;
        public ICommand HistogramInitialImageCommand
        {
            get
            {
                if (_histogramInitialImageCommand == null)
                    _histogramInitialImageCommand = new RelayCommand(HistogramInitialImage);
                return _histogramInitialImageCommand;
            }
        }

        private void HistogramInitialImage(object parameter)
        {
            if (InitialHistogramOn == true) return;
            if (InitialImage == null)
            {
                System.Windows.MessageBox.Show("Please add an image !");
                return;
            }

            HistogramWindow window = null;

            if (ColorInitialImage != null)
            {
                window = new HistogramWindow(_mainVM, ImageType.InitialColor);
            }
            else if (GrayInitialImage != null)
            {
                window = new HistogramWindow(_mainVM, ImageType.InitialGray);
            }

            window.Show();
        }
        #endregion

        #region Processed image histogram
        private ICommand _histogramProcessedImageCommand;
        public ICommand HistogramProcessedImageCommand
        {
            get
            {
                if (_histogramProcessedImageCommand == null)
                    _histogramProcessedImageCommand = new RelayCommand(HistogramProcessedImage);
                return _histogramProcessedImageCommand;
            }
        }

        private void HistogramProcessedImage(object parameter)
        {
            if (ProcessedHistogramOn == true) return;
            if (ProcessedImage == null)
            {
                System.Windows.MessageBox.Show("Please process an image !");
                return;
            }

            HistogramWindow window = null;

            if (ColorProcessedImage != null)
            {
                window = new HistogramWindow(_mainVM, ImageType.ProcessedColor);
            }
            else if (GrayProcessedImage != null)
            {
                window = new HistogramWindow(_mainVM, ImageType.ProcessedGray);
            }

            window.Show();
        }
        #endregion

        #endregion

        #region Copy image
        private ICommand _copyImageCommand;
        public ICommand CopyImageCommand
        {
            get
            {
                if (_copyImageCommand == null)
                    _copyImageCommand = new RelayCommand(CopyImage);
                return _copyImageCommand;
            }
        }

        private void CopyImage(object parameter)
        {
            if (InitialImage == null)
            {
                System.Windows.MessageBox.Show("Please add an image !");
                return;
            }

            ClearProcessedCanvas(parameter);

            if (ColorInitialImage != null)
            {
                ColorProcessedImage = Tools.Copy(ColorInitialImage);
                ProcessedImage = Convert(ColorProcessedImage);
            }
            else if (GrayInitialImage != null)
            {
                GrayProcessedImage = Tools.Copy(GrayInitialImage);
                ProcessedImage = Convert(GrayProcessedImage);
            }
        }
        #endregion

        #region Invert image
        private ICommand _invertImageCommand;
        public ICommand InvertImageCommand
        {
            get
            {
                if (_invertImageCommand == null)
                    _invertImageCommand = new RelayCommand(InvertImage);
                return _invertImageCommand;
            }
        }

        private void InvertImage(object parameter)
        {
            if (InitialImage == null)
            {
                System.Windows.MessageBox.Show("Please add an image !");
                return;
            }

            ClearProcessedCanvas(parameter);

            if (GrayInitialImage != null)
            {
                GrayProcessedImage = Tools.Invert(GrayInitialImage);
                ProcessedImage = Convert(GrayProcessedImage);
            }
            else if (ColorInitialImage != null)
            {
                ColorProcessedImage = Tools.Invert(ColorInitialImage);
                ProcessedImage = Convert(ColorProcessedImage);
            }
        }
        #endregion

        #region Convert color image to grayscale image
        private ICommand _convertImageToGrayscaleCommand;
        public ICommand ConvertImageToGrayscaleCommand
        {
            get
            {
                if (_convertImageToGrayscaleCommand == null)
                    _convertImageToGrayscaleCommand = new RelayCommand(ConvertImageToGrayscale);
                return _convertImageToGrayscaleCommand;
            }
        }

        private void ConvertImageToGrayscale(object parameter)
        {
            if (InitialImage == null)
            {
                System.Windows.MessageBox.Show("Please add an image !");
                return;
            }

            ClearProcessedCanvas(parameter);

            if (ColorInitialImage != null)
            {
                GrayProcessedImage = Tools.Convert(ColorInitialImage);
                ProcessedImage = Convert(GrayProcessedImage);
            }
            else
            {
                System.Windows.MessageBox.Show("It is possible to convert only color images !");
            }
        }
        #endregion

        #region Mirror
        private ICommand _mirrorCommand;
        public ICommand MirrorCommand
        {
            get
            {
                if (_mirrorCommand == null)
                    _mirrorCommand = new RelayCommand(Mirror);
                return _mirrorCommand;
            }
        }
        private void Mirror(object parameter)
        {
            if (SliderOn == true) return;
            if (InitialImage == null)
            {
                System.Windows.MessageBox.Show("Please add an image !");
                return;
            }
            if (GrayInitialImage != null)
            {
                GrayProcessedImage = GrayInitialImage.Flip(Emgu.CV.CvEnum.FlipType.Horizontal);
                ProcessedImage = Convert(GrayProcessedImage);
            }
            else {
                ColorProcessedImage = ColorInitialImage.Flip(Emgu.CV.CvEnum.FlipType.Horizontal);
                ProcessedImage = Convert(ColorProcessedImage);
            }

        }
        #endregion

        #region Clockwise
        private ICommand _clockwiseCommand;
        public ICommand ClockwiseCommand
        {
            get
            {
                if (_clockwiseCommand == null)
                    _clockwiseCommand = new RelayCommand(Clockwise);
                return _clockwiseCommand;
            }
        }
        private void Clockwise(object parameter)
        {
            if (SliderOn == true) return;
            if (InitialImage == null)
            {
                System.Windows.MessageBox.Show("Please add an image !");
                return;
            }
            if (GrayInitialImage != null)
            {
                GrayProcessedImage = GrayInitialImage.Rotate(90, new Gray(), false);
                ProcessedImage = Convert(GrayProcessedImage);
            }
            else
            {
                ColorProcessedImage = ColorInitialImage.Rotate(90, new Bgr(), false);
                ProcessedImage = Convert(ColorProcessedImage);
            }

        }
        #endregion

        #region AntiClockwise
        private ICommand _antiClockwiseCommand;
        public ICommand AntiClockwiseCommand
        {
            get
            {
                if (_antiClockwiseCommand == null)
                    _antiClockwiseCommand = new RelayCommand(AntiClockwise);
                return _antiClockwiseCommand;
            }
        }
        private void AntiClockwise(object parameter)
        {
            if (SliderOn == true) return;
            if (InitialImage == null)
            {
                System.Windows.MessageBox.Show("Please add an image !");
                return;
            }
            if (GrayInitialImage != null)
            {
                GrayProcessedImage = GrayInitialImage.Rotate(-90, new Gray(), false);
                ProcessedImage = Convert(GrayProcessedImage);
            }
            else
            {
                ColorProcessedImage = ColorInitialImage.Rotate(-90, new Bgr(), false);
                ProcessedImage = Convert(ColorProcessedImage);
            }

        }
        #endregion

        #endregion

        #region Pointwise operations

        #region GammaCorrection
        private ICommand _gammaCorrectionCommand;
        public ICommand GammaCorrectionCommand
        {
            get
            {
                if (_gammaCorrectionCommand == null)
                    _gammaCorrectionCommand = new RelayCommand(GammaCorrection);
                return _gammaCorrectionCommand;
            }
        }
        private void GammaCorrection(object parameter)
        {
            if (SliderOn == true) return;
            if (InitialImage == null)
            {
                System.Windows.MessageBox.Show("Please add an image !");
                return;
            }
            SliderWindow window = new SliderWindow(_mainVM, "Gamma Value: ");
            window.ConfigureSlider(0, 5, 0.5, 0.5);
            if (GrayInitialImage != null)
            {
                window.SetWindowData(image: GrayInitialImage, algorithm: Tools.GammaCorrection);
            }
            else // if (ColorInitialImage != null)
            {
                window.SetWindowData(image: ColorInitialImage,
                algorithm: Tools.GammaCorrection);
            }
            window.Show();
        }
        #endregion

        #endregion

        #region Thresholding

        #region Adaptive Binary
        private ICommand _adaptiveBinaryCommand;
        public ICommand AdaptiveBinaryCommand
        {
            get
            {
                if (_adaptiveBinaryCommand == null)
                    _adaptiveBinaryCommand = new RelayCommand(AdaptiveBinary);
                return _adaptiveBinaryCommand;
            }
        }
        private void AdaptiveBinary(object parameter)
        {
            if (InitialImage == null) {
                System.Windows.MessageBox.Show("Please add an image !");
                return;
            }
            ClearProcessedCanvas(parameter);
            List<string> parameters = new List<string>() { "Dimension for mask: ", };
            DialogBox window = new DialogBox(_mainVM, parameters);
            window.ShowDialog();

            List<double> values = window.GetValues();
            int windowDimension = (int)values[0];
            
            if (GrayInitialImage != null) {
                GrayProcessedImage = Tools.AdaptiveBinary(GrayInitialImage, windowDimension);
                ProcessedImage = Convert(GrayProcessedImage);
            }
            else // if (ColorInitialImage != null)
            {   GrayProcessedImage = Tools.Convert(ColorInitialImage);
                GrayProcessedImage = Tools.Binary(GrayProcessedImage, windowDimension);
                ProcessedImage = Convert(GrayProcessedImage);
            } 
        }
        #endregion

        #endregion

        #region Filters

        #region Median Filter

        private ICommand _medianFilterCommand;
        public ICommand MedianFilterCommand
        {
            get
            {
                if (_medianFilterCommand == null)
                    _medianFilterCommand = new RelayCommand(parameter => ApplyMedianFilter(parameter, parallel: false));
                return _medianFilterCommand;
            }
        }

        private ICommand _medianFilterParallelCommand;
        public ICommand MedianFilterParallelCommand
        {
            get
            {
                if (_medianFilterParallelCommand == null)
                    _medianFilterParallelCommand = new RelayCommand(parameter => ApplyMedianFilter(parameter, parallel: true));
                return _medianFilterParallelCommand;
            }
        }

        private void ApplyMedianFilter(object parameter, bool parallel)
        {
            if (InitialImage == null)
            {
                System.Windows.MessageBox.Show("Please open an image first!", "No Image", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ClearProcessedCanvas(parameter);

            // Prepare parameters for the dialog
            var parameters = new List<string> { "Dimension for filter (between 3-15):" };
            var window = new DialogBox(_mainVM, parameters);
            window.ShowDialog();

            // Retrieve and validate user input
            var values = window.GetValues();
            if (values == null || values.Count == 0)
            {
                System.Windows.MessageBox.Show("No dimension provided.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(values[0].ToString(), out int windowDimension) ||
                windowDimension < 3 ||
                windowDimension > 15) 
            {
                System.Windows.MessageBox.Show("Please enter a valid positive integer for the dimension.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                if (GrayInitialImage != null)
                {
                    GrayProcessedImage = Tools.MedianFilter(GrayInitialImage, windowDimension, parallel);
                    ProcessedImage = Convert(GrayProcessedImage);
                }
                else if (ColorInitialImage != null)
                {
                    ColorProcessedImage = Tools.MedianFilter(ColorInitialImage, windowDimension, parallel);
                    ProcessedImage = Convert(ColorProcessedImage);
                }
                else
                {
                    System.Windows.MessageBox.Show("Unsupported image format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                stopwatch.Stop();
                double elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                string executionMode = parallel ? "Parallel" : "Sequential";
                System.Windows.MessageBox.Show($"Elapsed time ({executionMode}): {elapsedMilliseconds} ms", "Execution Time", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Detect Horizontal Edges

        private ICommand _horizontalEdgesRGBCommand;
        public ICommand HorizontalEdgesRGBCommand
        {
            get
            {
                if (_horizontalEdgesRGBCommand == null)
                    _horizontalEdgesRGBCommand = new RelayCommand(DetectHorizontalEdgesRGB);
                return _horizontalEdgesRGBCommand;
            }
        }

        // Implement the method
        private void DetectHorizontalEdgesRGB(object parameter)
        {
            if (InitialImage == null)
            {
                System.Windows.MessageBox.Show("Please add an image !");
                return;
            }

            // Ensure we have a color image, as the requirement is for RGB images
            if (ColorInitialImage == null)
            {
                System.Windows.MessageBox.Show("Please load a colour image first!");
                return;
            }

            ClearProcessedCanvas(parameter);

            // Prompt for threshold T
            List<string> parameters = new List<string>() { "Threshold (T): " };
            DialogBox window = new DialogBox(_mainVM, parameters);
            window.ShowDialog();
            List<double> values = window.GetValues();
            if (values == null || values.Count == 0)
            {
                System.Windows.MessageBox.Show("No threshold provided.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            double threshold = values[0];

            // Apply the edge detection
            var stopwatch = Stopwatch.StartNew();
            var edges = Tools.DetectHorizontalEdgesRGB(ColorInitialImage, threshold);
            stopwatch.Stop();

            ColorProcessedImage = edges;
            ProcessedImage = Convert(ColorProcessedImage);

            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            System.Windows.MessageBox.Show($"Elapsed time: {elapsedMs} ms", "Execution Time", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #endregion

        #region Morphological operations

        private ICommand _connectedComponentsCommand;
        public ICommand ConnectedComponentsCommand
        {
            get
            {
                if (_connectedComponentsCommand == null)
                    _connectedComponentsCommand = new RelayCommand(FindConnectedComponents);
                return _connectedComponentsCommand;
            }
        }

        private void FindConnectedComponents(object parameter)
        {
            if (InitialImage == null)
            {
                System.Windows.MessageBox.Show("Please add an image !");
                return;
            }

            // Ensure we have a color image, as the requirement is for RGB images
            if (GrayInitialImage == null)
            {
                System.Windows.MessageBox.Show("Please load a gray (binary) image first!");
                return;
            }

            ClearProcessedCanvas(parameter);

            // Convert to binary if needed
            //var binaryImg = Tools.ConvertToBinary(ColorProcessedImage);

            // Perform connected components analysis
            var result = Tools.DetermineConnectedComponents(GrayInitialImage);

            ColorProcessedImage = result;
            ProcessedImage = Convert(ColorProcessedImage);
        }



        #endregion

        #region Geometric transformations

        #region Scale Image Bilinear

        private ICommand _scaleImageBilinearCommand;
        public ICommand ScaleImageBilinearCommand
        {
            get
            {
                if (_scaleImageBilinearCommand == null)
                    _scaleImageBilinearCommand = new RelayCommand(ScaleImageBilinear);
                return _scaleImageBilinearCommand;
            }
        }

        // Method for Scaling
        private void ScaleImageBilinear(object parameter)
        {
            if (InitialImage == null)
            {
                System.Windows.MessageBox.Show("Please add an image!");
                return;
            }


            List<string> parameters = new List<string>() { "Scaling Factor (e.g., 0.5):" };
            DialogBox window = new DialogBox(_mainVM, parameters);
            window.ShowDialog();
            List<double> values = window.GetValues();

            if (values == null || values.Count == 0)
            {
                System.Windows.MessageBox.Show("No scaling factor provided.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            double scale = values[0];

            if (scale <= 0)
            {
                System.Windows.MessageBox.Show("Scaling factor must be greater than 0!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            Image<Bgr, byte> scaledImage;

            // Handle grayscale vs colour images
            if (ColorInitialImage != null)
            {
                // Colour image: scale in HSV
                scaledImage = Tools.ScaleImageBilinearHSV(ColorInitialImage, scale);
            }
            else
            {
                // Grayscale image: directly scale the intensity channel
                scaledImage = Tools.ScaleBilinearGrayscale(GrayInitialImage, scale);
            }

            stopwatch.Stop();

            // Update processed image
            ColorProcessedImage = scaledImage;
            ProcessedImage = Convert(ColorProcessedImage);

            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            System.Windows.MessageBox.Show($"Image scaled successfully! Elapsed time: {elapsedMs} ms", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #endregion

        #region Segmentation
        #endregion

        #region Save processed image as original image
        private ICommand _saveAsOriginalImageCommand;
        public ICommand SaveAsOriginalImageCommand
        {
            get
            {
                if (_saveAsOriginalImageCommand == null)
                    _saveAsOriginalImageCommand = new RelayCommand(SaveAsOriginalImage);
                return _saveAsOriginalImageCommand;
            }
        }

        private void SaveAsOriginalImage(object parameter)
        {
            if (ProcessedImage == null)
            {
                System.Windows.MessageBox.Show("Please process an image first.");
                return;
            }

            var canvases = (object[])parameter;

            ClearInitialCanvas(canvases[0] as Canvas);

            if (GrayProcessedImage != null)
            {
                GrayInitialImage = GrayProcessedImage;
                InitialImage = Convert(GrayInitialImage);
            }
            else if (ColorProcessedImage != null)
            {
                ColorInitialImage = ColorProcessedImage;
                InitialImage = Convert(ColorInitialImage);
            }

            ClearProcessedCanvas(canvases[1] as Canvas);
        }
        #endregion

        #region Binary
        private ICommand _binaryCommand;
        public ICommand BinaryCommand {
            get {
                if (_binaryCommand == null)
                    _binaryCommand = new RelayCommand(Binary);
                return _binaryCommand;
            }
        }
        private void Binary(object parameter)
        {
            if (SliderOn == true) return;
            if (InitialImage == null) {
                System.Windows.MessageBox.Show("Please add an image !");
                return;
            }
            SliderWindow window = new SliderWindow(_mainVM, "Threshold: ");
            window.ConfigureSlider(10, 245, 10, 5);
            if (GrayInitialImage != null) {
                window.SetWindowData(image: GrayInitialImage, algorithm: Tools.Binary);
            }
            else // if (ColorInitialImage != null)
                 { window.SetWindowData( image: Tools.Convert(ColorInitialImage),
                     algorithm: Tools.Binary);
            }
            window.Show();
        }
        #endregion

        #region Crop
        private ICommand _cropCommand;
        public ICommand CropCommand
        {
            get
            {
                if (_cropCommand == null)
                    _cropCommand = new RelayCommand(Crop);
                return _cropCommand;
            }
        }
        private void Crop(object parameter)
        {
            if (InitialImage == null)
            {
                System.Windows.MessageBox.Show("Please add an image !");
                return;
            }
            if (VectorOfMousePosition.Count % 2 != 0 || VectorOfMousePosition.Count == 0) {
                System.Windows.MessageBox.Show("Please select an area to crop !");
                return;
            }

            System.Drawing.Point firstPoint = new System.Drawing.Point((int)VectorOfMousePosition[VectorOfMousePosition.Count - 1].X,
                (int)VectorOfMousePosition[VectorOfMousePosition.Count - 1].Y) ;
            System.Drawing.Point secondPoint = new System.Drawing.Point((int)VectorOfMousePosition[VectorOfMousePosition.Count - 2].X,
                (int)VectorOfMousePosition[VectorOfMousePosition.Count - 2].Y);
            if (GrayInitialImage != null) {
                GrayProcessedImage = Tools.Crop(GrayInitialImage,
                    firstPoint,
                    secondPoint);
                ProcessedImage = Convert(GrayProcessedImage);
            }
            else // if (ColorInitialImage != null)
            {
                ColorProcessedImage = Tools.Crop(ColorInitialImage, firstPoint, secondPoint);
                ProcessedImage = Convert(ColorProcessedImage);
            }
        }
        #endregion

    }
}