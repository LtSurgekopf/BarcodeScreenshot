using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AForge.Imaging.Filters;
using IronBarCode;
using BarcodeReader = IronBarCode.BarcodeReader;
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

    private static Bitmap ScreenShotBitmap => _screenShotBitmap;

    private static RubberbandAdorner _cropSelector;
    private static Bitmap _screenShotBitmap;

    public MainWindow()
    {
      CopyScreen(out _screenShotBitmap);
      _screenShotBitmap = new Bitmap("screen.png");
      var screenPath = Path.Combine(Environment.CurrentDirectory, "screen.png");
      ScreenShotSource = new BitmapImage(new Uri(screenPath));

      InitializeComponent();

      this.WindowState = WindowState.Maximized;
      this.Activate();
    }

    private static void CopyScreen(out Bitmap screenBitmap)
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
        //bmp.MakeTransparent(Color.Black);

        bmp.Save("screen.png", ImageFormat.Png);

        screenBitmap = bmp;
      }
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
      var adorner = new RubberbandAdorner(ImageViewScreenshot) { Window = this };
      await Task.Run(() => { DrawScreenShot(adorner); });

      BarcodeTypeComboBox.Focus();
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

      ReadBarcodeIron();
    }

    private void ReadBarcodeIron()
    {

      BarcodeEncoding possibleBarcodeEncoding = 0x0;
      switch ((BarcodeTypeComboBox.SelectedValue as ComboBoxItem)?.Content as string)
      {
        case "QR | ITF":
          possibleBarcodeEncoding = BarcodeEncoding.QRCode | BarcodeEncoding.ITF;
          break;
        case "QR":
          possibleBarcodeEncoding = BarcodeEncoding.QRCode;
          break;
        case "ITF":
          possibleBarcodeEncoding = BarcodeEncoding.ITF;
          break;
        case "1D":
          possibleBarcodeEncoding = BarcodeEncoding.AllOneDimensional;
          break;
        case "DataMatrix":
          possibleBarcodeEncoding = BarcodeEncoding.DataMatrix;
          break;
        case "2D":
          possibleBarcodeEncoding = BarcodeEncoding.DataMatrix |
                                    BarcodeEncoding.QRCode |
                                    BarcodeEncoding.PDF417 |
                                    BarcodeEncoding.Aztec;
          break;
        case "Alle":
          possibleBarcodeEncoding = BarcodeEncoding.All;
          break;
      }

      var ironResult = BarcodeReader.ReadASingleBarcode("crop.png",
        possibleBarcodeEncoding,
        BarcodeReader.BarcodeRotationCorrection.High,
        BarcodeReader.BarcodeImageCorrection.MediumCleanPixels);

      if (ironResult != null)
      {
        LabelBarcodeContent.Text = $"{ironResult.Text}";
        LabelBarcodeType.Text = ironResult.BarcodeType.ToString();
        //SystemSounds.Hand.Play();
        Console.Beep(3100, 80);
      }
    }
  }
}
