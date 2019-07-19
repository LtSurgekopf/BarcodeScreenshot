using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AForge.Imaging.Filters;
using ZXing;
using ZXing.Common;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;
using Path = System.IO.Path;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Rectangle = System.Drawing.Rectangle;

namespace BarcodeScreenshot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private BitmapSource ScreenShotSource { get; set; }

        private Bitmap ScreenShotBitmap => _screenShotBitmap;

        private RubberbandAdorner _cropSelector;
        private readonly Bitmap _screenShotBitmap;

        public MainWindow()
        {
            CopyScreen2(out _screenShotBitmap);
            _screenShotBitmap = new Bitmap("screen.png");
            var screenPath = Path.Combine(Environment.CurrentDirectory, "screen.png");
            ScreenShotSource = new BitmapImage(new Uri(screenPath));

            InitializeComponent();

            this.WindowState = WindowState.Maximized;
            this.Activate();
        }

        private static void CopyScreen2(out Bitmap screenBitmap)
        {
            int screenCount = Screen.AllScreens.Length;

            int screenTop = SystemInformation.VirtualScreen.Top;
            int screenLeft = SystemInformation.VirtualScreen.Left;
            int screenWidth = Screen.AllScreens.Max(m => m.Bounds.Width);
            int screenHeight = Screen.AllScreens.Max(m => m.Bounds.Height);

            bool isVertical = (SystemInformation.VirtualScreen.Height < SystemInformation.VirtualScreen.Width);

            if (isVertical)
                screenWidth *= screenCount;
            else
                screenHeight *= screenCount;

            // Create a bitmap of the appropriate size to receive the screenshot.
            using (Bitmap bmp = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppPArgb))
            {
                // Draw the screenshot into our bitmap.
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(screenLeft, screenTop, 0, 0, bmp.Size);
                }

                // Make black color transparent
                bmp.MakeTransparent(Color.Black);

                bmp.Save("screen.png", ImageFormat.Png);

                screenBitmap = bmp;
            }
        }

        private static BitmapSource CopyScreen()
        {

            using (var screenBmp = new Bitmap(
              (int)SystemParameters.VirtualScreenWidth,
              (int)SystemParameters.VirtualScreenHeight, PixelFormat.Format32bppPArgb))
            {
                using (var bmpGraphics = Graphics.FromImage(screenBmp))
                {
                    bmpGraphics.CopyFromScreen(0, 0, 0, 0, screenBmp.Size);
                    var screen = Imaging.CreateBitmapSourceFromHBitmap(
                      screenBmp.GetHbitmap(),
                      IntPtr.Zero,
                      Int32Rect.Empty,
                      BitmapSizeOptions.FromEmptyOptions());
                    screenBmp.Save("screen.png", ImageFormat.Png);
                    return screen;
                }
            }
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            var adorner = new RubberbandAdorner(ImageViewScreenshot) {Window = this};
            await Task.Run(() => { DrawScreenShot(adorner); });
        }
            

        public void AddAdornerToImageView(Adorner adorner)
        {
            AdornerLayer.GetAdornerLayer(ImageViewScreenshot)?.Add(adorner);
        }

        public delegate void GetAdornerLayerDelegate(Adorner adorner);

        private void DrawScreenShot(RubberbandAdorner adorner)
        {
            var getAdornerLayerDelegate = new GetAdornerLayerDelegate(AddAdornerToImageView);
            //var layer = AdornerLayer.GetAdornerLayer(ImageViewScreenshot);
            //_cropSelector = new RubberbandAdorner(ImageViewScreenshot) {Window = this};
            _cropSelector = adorner;
            //layer.Add(_cropSelector);
            Dispatcher.Invoke(getAdornerLayerDelegate, _cropSelector);


            //var bmp = new Bitmap("testImage-1920x1080.png");
            //ScreenShotSource = bmp.BitmapToImageSource();
            //ImageViewScreenshot.Source = ScreenShotSource;
            var setImageViewSourceDelegate = new SetImageViewSourceDelegate(SetImageViewSource);
            Dispatcher.Invoke(setImageViewSourceDelegate, ScreenShotSource);
        }

        public void SetImageViewSource(BitmapSource source)
        {
            ImageViewScreenshot.Source = source;
        }

        public delegate void SetImageViewSourceDelegate(BitmapSource source);

        private void ImageViewScreenshot_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var anchor = e.GetPosition(ImageViewScreenshot);
            _cropSelector.CaptureMouse();
            _cropSelector.StartSelection(anchor);
        }

        protected internal void ImageViewScreenshot_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            //var img = BitmapFrame.Create(ScreenShotSource);
            var cropRect = new Rectangle
            {
                X = (int)(_cropSelector.SelectRect.X * ScreenShotBitmap.Width / ImageViewScreenshot.ActualWidth),
                Y = (int)(_cropSelector.SelectRect.Y * ScreenShotBitmap.Height / ImageViewScreenshot.ActualHeight),
                Width = (int)(_cropSelector.SelectRect.Width * ScreenShotBitmap.Width / ImageViewScreenshot.ActualWidth),
                Height = (int)(_cropSelector.SelectRect.Height * ScreenShotBitmap.Height / ImageViewScreenshot.ActualHeight)
            };

            var croppedBitmap = new Crop(cropRect).Apply(ScreenShotBitmap);
            croppedBitmap.Save("crop.png");
            

            //var cropped = ScreenShotSource.cropAtRect(cropRect);

            //Bitmap cropped = new Bitmap(rect.Width, rect.Height);
            //using (Graphics g = Graphics.FromImage(img))
            //{
            //  g.DrawImage(img, new Rectangle(0, 0, cropped.Width, cropped.Height),
            //    rect, GraphicsUnit.Pixel);
            //}
            //var cropped = new CroppedBitmap(ScreenShotSource, rect);
            var cropURI = new Uri(Path.Combine(Environment.CurrentDirectory, "crop.png"));
            ImageViewScreenshot.Source = new BitmapImage(cropURI);
            AdornerLayer.GetAdornerLayer(ImageViewScreenshot)?.Remove(_cropSelector);
            //croppedBmp.Save("crop.bmp", ImageFormat.Bmp);

            //ReadBarcode(cropped);
            ReadBarcodeZXing(croppedBitmap);
        }

        private void ReadBarcodeZXing(Bitmap cropped)
        {
            IList<BarcodeFormat> possibleFormats = new List<BarcodeFormat>();
            switch ((BarcodeTypeComboBox.SelectedValue as ComboBoxItem)?.Content as string)
            {
                case "QR | ITF":
                    possibleFormats.Add(BarcodeFormat.QR_CODE);
                    possibleFormats.Add(BarcodeFormat.ITF);
                    break;
                case "QR":
                    possibleFormats.Add(BarcodeFormat.QR_CODE);
                    break;
                case "ITF":
                    possibleFormats.Add(BarcodeFormat.ITF);
                    break;
                case "1D":
                    possibleFormats.Add(BarcodeFormat.All_1D);
                    break;
                case "DataMatrix":
                    possibleFormats.Add(BarcodeFormat.DATA_MATRIX);
                    break;
                case "2D":
                    possibleFormats.Add(BarcodeFormat.DATA_MATRIX);
                    possibleFormats.Add(BarcodeFormat.QR_CODE);
                    possibleFormats.Add(BarcodeFormat.PDF_417);
                    possibleFormats.Add(BarcodeFormat.AZTEC);
                    break;
                case "Alle":
                    possibleFormats.Add(BarcodeFormat.All_1D);
                    possibleFormats.Add(BarcodeFormat.DATA_MATRIX);
                    possibleFormats.Add(BarcodeFormat.QR_CODE);
                    possibleFormats.Add(BarcodeFormat.PDF_417);
                    possibleFormats.Add(BarcodeFormat.AZTEC);
                    break;
            }


            IBarcodeReader reader = new BarcodeReader()
            {
                AutoRotate = true,
                Options = new DecodingOptions()
                {
                    TryHarder = true,
                    PossibleFormats = possibleFormats
                }
            };
            // load a bitmap
            // detect and decode the barcode inside the bitmap
            var result = reader.Decode(cropped);
            // do something with the result
            if (result != null)
            {
                LabelBarcodeContent.Text = $"({result.BarcodeFormat.ToString()}) {result.Text}";
                //SystemSounds.Hand.Play();
                Console.Beep(3100, 80);
            }
        }
    }

    static class ExtensionMethods
    {
        

        public static CroppedBitmap cropAtRect(this BitmapSource b, Rectangle r)
        {
            //Bitmap nb = new Bitmap(r.Width, r.Height);
            //using (var g = Graphics.FromImage(nb))
            //{
            //    g.DrawImage(b, -r.X, -r.Y);
            //} 

            var croppedBitmap = new CroppedBitmap(b, new Int32Rect
            {
                Height = r.Height,
                Width = r.Width,
                X = r.X,
                Y = r.Y
            });

            return croppedBitmap; //.ToBitmapFromSource();
            //return nb;
        }

        public static BitmapSource BitmapToImageSource(this Bitmap bmp)
        {
            BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bmp.GetHbitmap(),
                IntPtr.Zero,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(bmp.Width, bmp.Height));
            return bs;
        }
        
    }
}
