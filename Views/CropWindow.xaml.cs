using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using OpenCvSharp;
using CvRect = OpenCvSharp.Rect;
using WPoint = System.Windows.Point;
using WRect = System.Windows.Rect;
using IOPath = System.IO.Path;

namespace auto_chinhdo.Views
{
    /// <summary>
    /// Cửa sổ cắt ảnh template từ screenshot
    /// </summary>
    public partial class CropWindow : System.Windows.Window
    {
        private readonly string _srcPath;
        private readonly string _saveDir;
        private WPoint _startPoint;
        private WRect _imageRect;
        
        /// <summary>
        /// Đường dẫn file đã lưu sau khi crop thành công
        /// </summary>
        public string SavedPath { get; private set; } = string.Empty;

        /// <summary>
        /// Vùng đã chọn (tọa độ gốc trong ảnh) - Dùng cho cấu hình HP Bar
        /// </summary>
        public WRect? SelectedRegion { get; private set; }

        /// <summary>
        /// Chế độ chỉ chọn vùng (không lưu file ảnh) - Dùng cho cấu hình HP Bar
        /// </summary>
        public bool SelectOnlyMode { get; set; } = false;

        public CropWindow(string srcPath, string saveDir)
        {
            InitializeComponent();
            
            _srcPath = srcPath;
            _saveDir = saveDir;
            
            // Load source image
            SourceImage.Source = LoadImageUnlocked(_srcPath);
            
            // Setup mouse events
            OverlayCanvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            OverlayCanvas.MouseMove += Canvas_MouseMove;
            OverlayCanvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
            
            // Setup keyboard shortcuts
            PreviewKeyDown += Window_PreviewKeyDown;
            
            // Update image rect on size change
            SizeChanged += (s, e) => UpdateImageRect();
            Loaded += (s, e) => UpdateImageRect();
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(OverlayCanvas);
            
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;
            Canvas.SetLeft(SelectionRect, _startPoint.X);
            Canvas.SetTop(SelectionRect, _startPoint.Y);
            SelectionRect.Visibility = Visibility.Visible;
            
            OverlayCanvas.CaptureMouse();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (SelectionRect.Visibility != Visibility.Visible) return;
            
            var pos = e.GetPosition(OverlayCanvas);
            var x = Math.Min(pos.X, _startPoint.X);
            var y = Math.Min(pos.Y, _startPoint.Y);
            var w = Math.Abs(pos.X - _startPoint.X);
            var h = Math.Abs(pos.Y - _startPoint.Y);
            
            Canvas.SetLeft(SelectionRect, x);
            Canvas.SetTop(SelectionRect, y);
            SelectionRect.Width = w;
            SelectionRect.Height = h;
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OverlayCanvas.ReleaseMouseCapture();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
            
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SaveCrop();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveCrop();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void UpdateImageRect()
        {
            if (SourceImage.Source is not BitmapSource bmp) return;
            
            var boxW = OverlayCanvas.ActualWidth;
            var boxH = OverlayCanvas.ActualHeight;
            
            if (boxW <= 1 || boxH <= 1)
            {
                _imageRect = WRect.Empty;
                return;
            }
            
            double scale = Math.Min(boxW / bmp.PixelWidth, boxH / bmp.PixelHeight);
            double dispW = bmp.PixelWidth * scale;
            double dispH = bmp.PixelHeight * scale;
            double offX = (boxW - dispW) / 2.0;
            double offY = (boxH - dispH) / 2.0;
            
            _imageRect = new WRect(offX, offY, dispW, dispH);
        }

        private void SaveCrop()
        {
            if (SourceImage.Source is not BitmapSource bmp) return;
            if (SelectionRect.Visibility != Visibility.Visible) return;
            if (SelectionRect.Width < 2 || SelectionRect.Height < 2) return;
            if (_imageRect.IsEmpty) return;

            // Calculate crop region in original image coordinates
            double rx = Canvas.GetLeft(SelectionRect);
            double ry = Canvas.GetTop(SelectionRect);
            double rw = SelectionRect.Width;
            double rh = SelectionRect.Height;
            
            double scale = _imageRect.Width / bmp.PixelWidth;
            double imgX = (rx - _imageRect.X) / scale;
            double imgY = (ry - _imageRect.Y) / scale;
            double imgW = rw / scale;
            double imgH = rh / scale;

            // Clamp to image bounds
            imgX = Math.Max(0, imgX);
            imgY = Math.Max(0, imgY);
            imgW = Math.Max(1, Math.Min(bmp.PixelWidth - imgX, imgW));
            imgH = Math.Max(1, Math.Min(bmp.PixelHeight - imgY, imgH));


            // Crop using OpenCV
            using var img = Cv2.ImRead(_srcPath, ImreadModes.Color);
            
            int x = (int)Math.Round(imgX);
            int y = (int)Math.Round(imgY);
            int w = (int)Math.Round(imgW);
            int h = (int)Math.Round(imgH);
            
            if (w <= 1 || h <= 1) return;

            // Lưu vùng đã chọn (dùng cho cấu hình HP Bar)
            SelectedRegion = new WRect(imgX, imgY, imgW, imgH);

            // Nếu là chế độ chỉ chọn vùng (HP Bar config), không cần lưu file ảnh
            if (SelectOnlyMode)
            {
                DialogResult = true;
                Close();
                return;
            }

            using var roi = new Mat(img, new CvRect(x, y, w, h));
            
            // Xử lý tách nền nếu checkbox được chọn
            Mat outputImage;
            if (ChkExtractText.IsChecked == true)
            {
                outputImage = ExtractTextWithTransparency(roi);
            }
            else
            {
                outputImage = roi.Clone();
            }

            // Show save dialog
            var defaultFileName = IOPath.Combine(_saveDir, $"template_{DateTime.Now:HHmmss}.png");
            var sfd = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = defaultFileName,
                InitialDirectory = _saveDir
            };

            if (sfd.ShowDialog() == true)
            {
                Cv2.ImWrite(sfd.FileName, outputImage);
                SavedPath = sfd.FileName;
                
                DialogResult = true;
                Close();
            }
            
            outputImage.Dispose();
        }

        /// <summary>
        /// Tách chữ/icon khỏi nền, trả về ảnh PNG với alpha channel (nền trong suốt)
        /// </summary>
        private Mat ExtractTextWithTransparency(Mat src)
        {
            // 1. Chuyển sang grayscale
            using var gray = new Mat();
            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

            // 2. Áp dụng Adaptive Threshold để tách chữ
            // Đảo ngược để chữ trắng trên nền đen
            using var binary = new Mat();
            Cv2.AdaptiveThreshold(gray, binary, 255, 
                AdaptiveThresholdTypes.GaussianC, 
                ThresholdTypes.BinaryInv, 
                blockSize: 11, c: 2);

            // 3. Làm mịn mask (loại bỏ nhiễu)
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
            using var cleanMask = new Mat();
            Cv2.MorphologyEx(binary, cleanMask, MorphTypes.Close, kernel);
            Cv2.MorphologyEx(cleanMask, cleanMask, MorphTypes.Open, kernel);

            // 4. Tạo ảnh BGRA (có alpha channel)
            using var bgra = new Mat();
            Cv2.CvtColor(src, bgra, ColorConversionCodes.BGR2BGRA);

            // 5. Áp dụng mask vào alpha channel
            // Chữ = giữ nguyên (alpha = 255), nền = trong suốt (alpha = 0)
            var result = new Mat(bgra.Size(), MatType.CV_8UC4);
            var channels = Cv2.Split(bgra);
            
            // channels[3] là alpha channel, gán = cleanMask
            channels[3] = cleanMask.Clone();
            
            Cv2.Merge(channels, result);
            
            // Cleanup
            foreach (var ch in channels) ch.Dispose();

            return result;
        }

        private static BitmapImage LoadImageUnlocked(string path)
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bi.UriSource = new Uri(path);
            bi.EndInit();
            bi.Freeze();
            return bi;
        }
    }
}
