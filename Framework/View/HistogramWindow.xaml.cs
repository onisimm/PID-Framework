using System.Windows;

using Framework.ViewModel;
using static Framework.Utilities.DataProvider;

namespace Framework.View
{
    public partial class HistogramWindow : Window
    {
        private readonly HistogramVM _histogramVM;
        private readonly ImageType _imageType = ImageType.None;

        public HistogramWindow(MainVM mainVM, ImageType type)
        {
            InitializeComponent();

            HistogramOn = true;

            _histogramVM = new HistogramVM();
            _histogramVM.Theme = mainVM.Theme;

            if (type != ImageType.None)
            {
                _histogramVM.CreateHistogram(_imageType = type);
            }

            DataContext = _histogramVM;
        }

        public HistogramWindow(MainVM mainVM, string title, dynamic values)
        {
            InitializeComponent();

            _histogramVM = new HistogramVM();
            _histogramVM.Theme = mainVM.Theme;
            _histogramVM.Title = title;

            if (values != null)
            {
                _histogramVM.CreateHistogram(values);
            }

            DataContext = _histogramVM;
        }

        private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize == e.NewSize)
                return;

            var w = SystemParameters.PrimaryScreenWidth;
            var h = SystemParameters.PrimaryScreenHeight;

            Left = (w - e.NewSize.Width) / 2;
            Top = (h - e.NewSize.Height) / 2;
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            switch (_imageType)
            {
                case ImageType.None:
                    HistogramOn = false;
                    break;

                case ImageType.InitialGray:
                    InitialHistogramOn = false;
                    break;

                case ImageType.InitialColor:
                    InitialHistogramOn = false;
                    break;

                case ImageType.ProcessedGray:
                    ProcessedHistogramOn = false;
                    break;

                case ImageType.ProcessedColor:
                    ProcessedHistogramOn = false;
                    break;
            }
        }
    }
}