using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;
using System.Linq;
using System.Windows;
using PointCollection = System.Windows.Media.PointCollection;

namespace Framework.Utilities
{
    class DataProvider
    {
        public static Image<Gray, byte> GrayInitialImage { get; set; }
        public static Image<Gray, byte> GrayProcessedImage { get; set; }
        public static Image<Bgr, byte> ColorInitialImage { get; set; }
        public static Image<Bgr, byte> ColorProcessedImage { get; set; }
        public static System.Windows.Point MousePosition { get; set; }
        public static System.Windows.Point LastPosition { get; set; }
        public static PointCollection VectorOfMousePosition { get; set; }

        public static bool MagnifierOn { get; set; }
        public static bool RowColorLevelsOn { get; set; }
        public static bool ColumnColorLevelsOn { get; set; }
        public static bool InitialHistogramOn { get; set; }
        public static bool ProcessedHistogramOn { get; set; }
        public static bool HistogramOn { get; set; }
        public static bool SliderOn { get; set; }
        public static readonly (int, int, int) PaviaRGB = ( 63, 38, 13 );
        static DataProvider()
        {
            MousePosition = new System.Windows.Point(0, 0);
            LastPosition = MousePosition;
            VectorOfMousePosition = new PointCollection();
        }

        public static void CloseWindow<TWindow>()
            where TWindow : Window
        {
            var window = Application.Current.Windows.OfType<TWindow>().SingleOrDefault(w => w.IsActive);
            window?.Close();
        }

        public static void CloseWindows()
        {
            for (int intCounter = Application.Current.Windows.Count - 1; intCounter >= 1; --intCounter)
                Application.Current.Windows[intCounter].Close();
        }
    }
}