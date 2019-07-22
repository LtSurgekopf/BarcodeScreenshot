using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
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
using static BarcodeScreenshot.App;
using Application = System.Windows.Application;
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

    private RubberbandAdorner _cropSelector;

    public MainWindow()
    {
      DoScreenShot();

      InitializeComponent();

      this.WindowState = WindowState.Maximized;
      this.Activate();
    }

    private void DoScreenShot()
    {
      CopyScreen();
    }

    private static void CopyScreen()
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

        bmp.Save("screen.png", ImageFormat.Png);
        bmp.Dispose();
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

      var setImageViewSourceDelegate = new SetImageViewSourceDelegate(SetImageViewSource);

      var screenPath = Path.Combine(Environment.CurrentDirectory, "screen.png");
      Dispatcher.Invoke(setImageViewSourceDelegate);
    }

    public void SetImageViewSource()
    {
      var screenPath = Path.Combine(Environment.CurrentDirectory, "screen.png");
      MemoryStream ms = new MemoryStream();
      using (var screen = (Bitmap) Image.FromFile(screenPath))
      {
        screen.Save(ms, ImageFormat.Png);
      }

      BitmapImage bitmapImage = new BitmapImage();
      bitmapImage.BeginInit();
      bitmapImage.StreamSource = ms;
      bitmapImage.EndInit();

      ImageViewScreenshot.Source = bitmapImage;
    }

    public delegate void SetImageViewSourceDelegate();

    private void ImageViewScreenshot_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
      var anchor = e.GetPosition(ImageViewScreenshot);
      _cropSelector.CaptureMouse();
      _cropSelector.StartSelection(anchor);
    }

    protected internal void ImageViewScreenshot_OnMouseUp(object sender, MouseButtonEventArgs e)
    {
      Bitmap croppedBitmap;
      using (var screenShotBitmap = (Bitmap) Image.FromFile("screen.png"))
      {
        var cropRect = new Rectangle
        {
          X = (int)(_cropSelector.SelectRect.X * screenShotBitmap.Width / ImageViewScreenshot.ActualWidth),
          Y = (int)(_cropSelector.SelectRect.Y * screenShotBitmap.Height / ImageViewScreenshot.ActualHeight),
          Width = (int)(_cropSelector.SelectRect.Width * screenShotBitmap.Width / ImageViewScreenshot.ActualWidth),
          Height = (int)(_cropSelector.SelectRect.Height * screenShotBitmap.Height / ImageViewScreenshot.ActualHeight)
        };

        croppedBitmap = new Crop(cropRect).Apply(screenShotBitmap);
        screenShotBitmap.Dispose();
        croppedBitmap.Save("crop.png");
        ReadBarcodeZXing(croppedBitmap);
      }
      croppedBitmap.Dispose();


      //var cropped = ScreenShotSource.cropAtRect(cropRect);

      //Bitmap cropped = new Bitmap(rect.Width, rect.Height);
      //using (Graphics g = Graphics.FromImage(img))
      //{
      //  g.DrawImage(img, new Rectangle(0, 0, cropped.Width, cropped.Height),
      //    rect, GraphicsUnit.Pixel);
      //}
      //var cropped = new CroppedBitmap(ScreenShotSource, rect);
      //var cropURI = new Uri(Path.Combine(Environment.CurrentDirectory, "crop.png"));
      //ImageViewScreenshot.Source = new BitmapImage(cropURI);
      //AdornerLayer.GetAdornerLayer(ImageViewScreenshot)?.Remove(_cropSelector);
      //croppedBmp.Save("crop.bmp", ImageFormat.Bmp);

      //ReadBarcode(cropped);
      
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
          possibleFormats.Add(BarcodeFormat.CODE_128);
          break;
        case "Alle":
          possibleFormats.Add(BarcodeFormat.All_1D);
          possibleFormats.Add(BarcodeFormat.DATA_MATRIX);
          possibleFormats.Add(BarcodeFormat.QR_CODE);
          possibleFormats.Add(BarcodeFormat.PDF_417);
          possibleFormats.Add(BarcodeFormat.AZTEC);
          possibleFormats.Add(BarcodeFormat.CODE_128);
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
        LabelBarcodeContent.Text = result.Text;
        LabelBarcodeType.Text = result.BarcodeFormat.ToString();
        //SystemSounds.Hand.Play();
        Console.Beep(3100, 80);
      }
    }

    private void ButtonNewScreenShot_OnClick(object sender, RoutedEventArgs e)
    {
      if (Application.Current.MainWindow != null) Application.Current.MainWindow.WindowState = WindowState.Minimized;
      _cropSelector.HideSelection();
      //Thread.Sleep(50);
      ImageViewScreenshot.Source = null;
      DoScreenShot();
      var adorner = new RubberbandAdorner(ImageViewScreenshot) { Window = this };
      DrawScreenShot(adorner);
      //Thread.Sleep(50);
      this.Activate();
      this.Focus();
    }
  }
}
