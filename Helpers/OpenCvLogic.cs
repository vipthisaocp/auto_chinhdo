using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using OpenCvSharp; // CẦN THIẾT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using IOPath = System.IO.Path;

// THÊM: Định nghĩa alias cho OpenCvSharp.Point và OpenCvSharp.Rect
using CvPoint = OpenCvSharp.Point;
using CvRect = OpenCvSharp.Rect;

// Helpers chứa các hàm xử lý ảnh OpenCV

namespace auto_chinhdo.Helpers
{
    public enum TargetColor { Purple, Red }

    // Lớp tĩnh chứa tất cả các hàm xử lý ảnh (OpenCV)
    public static class OpenCvLogic
    {
        // =========================================================================
        // HÀM 1: MatchAny (Tìm điểm khớp tốt nhất trong nhiều template - Không lọc màu)
        // [Được sử dụng bởi AutoWorker (thường), TapSelectedAsync, MatchOnceSelectedAsync]
        // =========================================================================
        public static (string tpl, CvPoint center, double score)? MatchAny(string screenPath, string[] templates, double threshold)
        {
            return MatchAnyWithROI(screenPath, templates, threshold, null);
        }

        /// <summary>
        /// Phiên bản MatchAny hỗ trợ ROI (Region of Interest)
        /// </summary>
        public static (string tpl, CvPoint center, double score)? MatchAnyWithROI(string screenPath, string[] templates, double threshold, CvRect? roi = null)
        {
            using var imgColorFull = Cv2.ImRead(screenPath, ImreadModes.Color);
            if (imgColorFull.Empty()) return null;

            using var imgColor = roi.HasValue ? new Mat(imgColorFull, roi.Value) : imgColorFull;
            
            using var imgGray = new Mat();
            Cv2.CvtColor(imgColor, imgGray, ColorConversionCodes.BGR2GRAY);

            (string tpl, CvPoint c, double s)? best = null;

            foreach (var t in templates)
            {
                if (!File.Exists(t)) continue;

                using var tplFull = Cv2.ImRead(t, ImreadModes.Unchanged);
                if (tplFull.Empty()) continue;

                Mat tplMask = null;
                Mat tplGray = null;
                TemplateMatchModes mode;
                bool isSqDiff = false;

                try
                {
                    if (tplFull.Channels() < 4)
                    {
                        mode = TemplateMatchModes.CCoeffNormed; isSqDiff = false;
                        tplGray = new Mat(); Cv2.CvtColor(tplFull, tplGray, ColorConversionCodes.BGR2GRAY);
                    }
                    else
                    {
                        mode = TemplateMatchModes.SqDiffNormed; isSqDiff = true;
                        Mat[] planes = Cv2.Split(tplFull); tplMask = planes[3];
                        using var tplColor = new Mat(); Mat[] colorPlanes = new Mat[] { planes[0], planes[1], planes[2] };
                        Cv2.Merge(colorPlanes, tplColor);
                        tplGray = new Mat(); Cv2.CvtColor(tplColor, tplGray, ColorConversionCodes.BGR2GRAY);
                        for (int i = 0; i < 3; i++) planes[i].Dispose();
                    }

                    if (tplGray == null || tplGray.Width > imgGray.Width || tplGray.Height > imgGray.Height) continue;

                    using var res = new Mat(imgGray.Rows - tplGray.Rows + 1, imgGray.Cols - tplGray.Cols + 1, MatType.CV_32FC1);
                    if (tplMask != null) Cv2.MatchTemplate(imgGray, tplGray, res, mode, tplMask);
                    else Cv2.MatchTemplate(imgGray, tplGray, res, mode);

                    Cv2.MinMaxLoc(res, out double minVal, out double maxVal, out CvPoint minLoc, out CvPoint maxLoc);

                    double currentScore;
                    CvPoint currentLoc;
                    double comparisonValue;

                    if (isSqDiff)
                    {
                        currentScore = 1.0 - minVal; currentLoc = minLoc; comparisonValue = minVal;
                    }
                    else
                    {
                        currentScore = maxVal; currentLoc = maxLoc; comparisonValue = maxVal;
                    }

                    bool accepted = isSqDiff ? (comparisonValue <= (1.0 - threshold)) : (comparisonValue >= threshold);

                    if (accepted)
                    {
                        // Tính tọa độ trung tâm (X, Y)
                        int centerX = currentLoc.X + tplGray.Width / 2;
                        int centerY = currentLoc.Y + tplGray.Height / 2;

                        // Nếu dùng ROI, phải cộng thêm offset của ROI để ra tọa độ trên ảnh gốc
                        if (roi.HasValue)
                        {
                            centerX += roi.Value.X;
                            centerY += roi.Value.Y;
                        }

                        var c = new CvPoint(centerX, centerY);

                        if (best == null || (currentScore > best.Value.s))
                        {
                            best = (t, c, currentScore);
                        }
                    }
                }
                finally
                {
                    tplMask?.Dispose();
                    tplGray?.Dispose();
                }
            }
            // Dispose imgColor nếu nó là submat (crop)
            if (roi.HasValue) imgColor.Dispose();
            
            return best;
        }

        // =========================================================================
        // HÀM 2: PerformTargetLockMatch (Tìm điểm khớp có Lọc màu Tím ĐẬM)
        // [Được sử dụng bởi PkManager.PkAutoWorker]
        // =========================================================================
        public static (string tpl, CvPoint center, double score, CvPoint location)? PerformTargetLockMatch(string screenPath, string templatePath, double threshold)
        {
            // B1: Đọc ảnh và Áp dụng Lọc Màu Tím (HSV Filtering)
            using var imgColor = Cv2.ImRead(screenPath, ImreadModes.Color);
            if (imgColor.Empty()) return null;

            using var imgHsv = new Mat();
            Cv2.CvtColor(imgColor, imgHsv, ColorConversionCodes.BGR2HSV);

            using var colorMask = new Mat();

            // Dải Tím đậm đã điều chỉnh
            Scalar lowerPurple = new Scalar(120, 30, 30);
            Scalar upperPurple = new Scalar(170, 255, 255);
            Cv2.InRange(imgHsv, lowerPurple, upperPurple, colorMask);

            using var maskedImage = new Mat();
            imgColor.CopyTo(maskedImage, colorMask);

            using var imgGray = new Mat();
            Cv2.CvtColor(maskedImage, imgGray, ColorConversionCodes.BGR2GRAY);

            // --- B2: TÁI SỬ DỤNG LOGIC SO KHỚP CHO TEMPLATE ĐƠN ---

            using var tplFull = Cv2.ImRead(templatePath, ImreadModes.Unchanged);
            if (tplFull.Empty()) return null;

            Mat tplMask = null; Mat tplGray = null; TemplateMatchModes mode; bool isSqDiff = false;

            try
            {
                // Logic tách Template và xác định mode
                if (tplFull.Channels() < 4)
                {
                    mode = TemplateMatchModes.CCoeffNormed; isSqDiff = false;
                    tplGray = new Mat(); Cv2.CvtColor(tplFull, tplGray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    mode = TemplateMatchModes.SqDiffNormed; isSqDiff = true;
                    Mat[] planes = Cv2.Split(tplFull); tplMask = planes[3];
                    using var tplColor = new Mat(); Mat[] colorPlanes = new Mat[] { planes[0], planes[1], planes[2] };
                    Cv2.Merge(colorPlanes, tplColor);
                    tplGray = new Mat(); Cv2.CvtColor(tplColor, tplGray, ColorConversionCodes.BGR2GRAY);
                    for (int i = 0; i < 3; i++) planes[i].Dispose();
                }

                if (tplGray == null || tplGray.Width > imgGray.Width || tplGray.Height > imgGray.Height) return null;

                // B3: Thực hiện MatchTemplate trên ảnh đã lọc màu Tím (imgGray)
                using var res = new Mat(imgGray.Rows - tplGray.Rows + 1, imgGray.Cols - tplGray.Cols + 1, MatType.CV_32FC1);

                if (tplMask != null) Cv2.MatchTemplate(imgGray, tplGray, res, mode, tplMask);
                else Cv2.MatchTemplate(imgGray, tplGray, res, mode);

                Cv2.MinMaxLoc(res, out double minVal, out double maxVal, out CvPoint minLoc, out CvPoint maxLoc);

                double currentScore;
                CvPoint location;
                double comparisonValue;

                if (isSqDiff)
                {
                    currentScore = 1.0 - minVal; location = minLoc; comparisonValue = minVal;
                }
                else
                {
                    currentScore = maxVal; location = maxLoc; comparisonValue = maxVal;
                }

                bool accepted = isSqDiff ? (comparisonValue <= (1.0 - threshold)) : (comparisonValue >= threshold);

                if (accepted)
                {
                    CvPoint center = new CvPoint(location.X + tplGray.Width / 2, location.Y + tplGray.Height / 2);
                    return (templatePath, center, currentScore, location);
                }
                return null;
            }
            finally
            {
                tplMask?.Dispose();
                tplGray?.Dispose();
            }
        }

        // =========================================================================
        // HÀM 3: DrawMarkerToFile (Vẽ đánh dấu lên ảnh chụp)
        // =========================================================================
        // =========================================================================
        // HÀM 4: FindColorLocations (Tìm tọa độ các vùng có màu cụ thể)
        // [Sử dụng để tìm tên nhân vật màu Tím/Đỏ mà không cần template]
        // =========================================================================
        public static List<CvPoint> FindColorLocations(string screenPath, TargetColor colorType)
        {
            var results = new List<CvPoint>();
            using var imgColor = Cv2.ImRead(screenPath, ImreadModes.Color);
            if (imgColor.Empty()) return results;

            using var imgHsv = new Mat();
            Cv2.CvtColor(imgColor, imgHsv, ColorConversionCodes.BGR2HSV);

            using var mask = new Mat();
            Scalar lower, upper;

            if (colorType == TargetColor.Purple)
            {
                // Dải màu Tím (Magenta/Purple) của tên nhân vật
                lower = new Scalar(140, 100, 100);
                upper = new Scalar(170, 255, 255);
            }
            else // Red
            {
                // Dải màu Đỏ (Thường là tên quái hoặc đối địch khác)
                // Lưu ý: Màu đỏ trong HSV bị chia làm 2 dải (0-10 và 160-180)
                using var mask1 = new Mat();
                using var mask2 = new Mat();
                Cv2.InRange(imgHsv, new Scalar(0, 100, 100), new Scalar(10, 255, 255), mask1);
                Cv2.InRange(imgHsv, new Scalar(160, 100, 100), new Scalar(180, 255, 255), mask2);
                Cv2.BitwiseOr(mask1, mask2, mask);
                goto ProcessContours;
            }

            Cv2.InRange(imgHsv, lower, upper, mask);

        ProcessContours:
            // Khử nhiễu nhẹ
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
            Cv2.MorphologyEx(mask, mask, MorphTypes.Open, kernel);

            // Tìm các đường bao (Contours)
            Cv2.FindContours(mask, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                double area = Cv2.ContourArea(contour);
                // Chỉ lấy các vùng có kích thước phù hợp với tên nhân vật (đủ lớn để click, không quá nhỏ là nhiễu)
                if (area > 50 && area < 5000)
                {
                    var rect = Cv2.BoundingRect(contour);
                    results.Add(new CvPoint(rect.X + rect.Width / 2, rect.Y + rect.Height / 2));
                }
            }

            return results;
        }

        public static void DrawMarkerToFile(string imgPath, CvPoint p)
        {
            using var img = Cv2.ImRead(imgPath, ImreadModes.Color);
            Cv2.DrawMarker(img, p, Scalar.Lime, MarkerTypes.Cross, 24, 2);
            Cv2.ImWrite(imgPath, img);
        }

        // =========================================================================
        // HÀM 5: IsTargetHealthBarVisible (Kiểm tra thanh máu mục tiêu bằng màu sắc)
        // [Giúp bám mục tiêu ngay cả khi HP tụt làm thay đổi hình ảnh template]
        // =========================================================================
        public static bool IsTargetHealthBarVisible(string screenPath)
        {
            // Dùng logic đơn giản: Kiểm tra xem có màu Tím/Vàng hoặc Đỏ ở nửa trên màn hình không
            return HasEnemyOnScreen(screenPath);
        }

        /// <summary>
        /// Kiểm tra xem có địch trên màn hình không bằng cách so sánh với ảnh mẫu "trống".
        /// Nếu không có ảnh mẫu, sẽ dùng logic nhận diện màu sắc đơn giản.
        /// </summary>
        public static bool HasEnemyOnScreen(string screenPath, string? emptyScreenPath = null)
        {
            using var imgColor = Cv2.ImRead(screenPath, ImreadModes.Color);
            if (imgColor.Empty()) return false;

            // Vùng ROI: Góc trên bên trái - nơi hiển thị thông tin địch/nút chọn
            // Dựa trên ảnh người dùng: khoảng 0-150px theo X và 0-200px theo Y
            int roiW = Math.Min(180, imgColor.Cols);
            int roiH = Math.Min(250, imgColor.Rows);
            using var roi = imgColor[0, roiH, 0, roiW];

            // Nếu có ảnh mẫu trống, so sánh trực tiếp
            if (!string.IsNullOrEmpty(emptyScreenPath) && File.Exists(emptyScreenPath))
            {
                using var emptyImg = Cv2.ImRead(emptyScreenPath, ImreadModes.Color);
                if (!emptyImg.Empty())
                {
                    using var emptyRoi = emptyImg[0, Math.Min(roiH, emptyImg.Rows), 0, Math.Min(roiW, emptyImg.Cols)];
                    
                    // Resize nếu cần
                    using var resizedEmpty = new Mat();
                    if (roi.Size() != emptyRoi.Size())
                    {
                        Cv2.Resize(emptyRoi, resizedEmpty, roi.Size());
                    }
                    else
                    {
                        emptyRoi.CopyTo(resizedEmpty);
                    }

                    // So sánh bằng SSIM hoặc đơn giản là pixel diff
                    using var diff = new Mat();
                    Cv2.Absdiff(roi, resizedEmpty, diff);
                    Cv2.CvtColor(diff, diff, ColorConversionCodes.BGR2GRAY);
                    double meanDiff = Cv2.Mean(diff).Val0;

                    // Nếu khác biệt > 15 -> có địch
                    return meanDiff > 15;
                }
            }

            // Fallback: Dùng logic màu sắc đơn giản
            using var hsv = new Mat();
            Cv2.CvtColor(roi, hsv, ColorConversionCodes.BGR2HSV);

            // Kiểm tra màu Tím (tên địch PK)
            using var purpleMask = new Mat();
            Cv2.InRange(hsv, new Scalar(130, 50, 50), new Scalar(170, 255, 255), purpleMask);

            // Kiểm tra màu Vàng (tên quái/ký hiệu)
            using var yellowMask = new Mat();
            Cv2.InRange(hsv, new Scalar(15, 80, 80), new Scalar(40, 255, 255), yellowMask);

            // Kiểm tra màu Đỏ (thanh máu)
            using var redMask1 = new Mat();
            using var redMask2 = new Mat();
            using var redMask = new Mat();
            Cv2.InRange(hsv, new Scalar(0, 100, 80), new Scalar(10, 255, 255), redMask1);
            Cv2.InRange(hsv, new Scalar(170, 100, 80), new Scalar(180, 255, 255), redMask2);
            Cv2.BitwiseOr(redMask1, redMask2, redMask);

            int purplePixels = Cv2.CountNonZero(purpleMask);
            int yellowPixels = Cv2.CountNonZero(yellowMask);
            int redPixels = Cv2.CountNonZero(redMask);

            // Nếu thấy bất kỳ màu nào đặc trưng của địch
            return purplePixels > 10 || yellowPixels > 10 || redPixels > 50;
        }

        // =========================================================================
        // HÀM 6: ScanHealthBarWithConfig (Quét pixel để tính % máu với config động)
        // Thuật toán: Quét dọc theo đường ngang Y giữa thanh máu, đếm pixel đỏ
        // =========================================================================

        /// <summary>
        /// Quét dọc theo đường ngang Y của thanh máu để tính % HP.
        /// Sử dụng cấu hình từ HealthBarConfig (Model).
        /// </summary>
        /// <param name="screenPath">Đường dẫn ảnh chụp màn hình</param>
        /// <param name="config">Cấu hình vùng thanh máu (từ Models.HealthBarConfig)</param>
        /// <param name="isBoss">Cờ xác định mục tiêu có phải Boss không (dựa trên icon)</param>
        /// <returns>Phần trăm máu (0-100), hoặc -1 nếu không tìm thấy thanh máu</returns>
        public static double ScanHealthBarWithConfig(string screenPath, Models.HealthBarConfig config, bool isBoss = false)
        {
            using var imgColor = Cv2.ImRead(screenPath, ImreadModes.Color);
            if (imgColor.Empty()) return -1;

            using var imgHsv = new Mat();
            Cv2.CvtColor(imgColor, imgHsv, ColorConversionCodes.BGR2HSV);

            // Giới hạn vùng quét
            int startX = config.X;
            int endX = config.X + config.Width;
            int startY = config.Y;
            int endY = config.Y + config.Height;

            if (startY < 0 || endY > imgHsv.Rows || startX < 0 || endX > imgHsv.Cols) return -1;

            int firstRedX = -1;
            int realLastRedX = -1;
            int totalRedPixels = 0;
            int firstDarkX = -1; 
            int totalDarkPixels = 0;
            
            int continuousMissingRed = 0;
            bool foundGap = false;

            // Để tính độ liên tục của nền đen
            int maxContinuousDark = 0;
            int currentContinuousDark = 0;
            int darkColsCount = 0;

            for (int x = startX; x < endX; x++)
            {
                bool colHasRed = false;
                int colDarkPixels = 0;

                for (int y = startY; y < endY; y++)
                {
                    Vec3b hsv = imgHsv.At<Vec3b>(y, x);
                    int h = hsv[0];
                    int s = hsv[1];
                    int v = hsv[2];

                    // 🔴 Màu Đỏ (Chuẩn HSV)
                    // V12-V13: Value >= 40 để bắt máu thẫm
                    bool isRed = ((h >= 0 && h <= 10) || (h >= 160 && h <= 180)) && s >= 70 && v >= 40;
                    if (isRed)
                    {
                        colHasRed = true;
                        totalRedPixels++;
                    }

                    // ⚫ Màu Tối (Nền đen của thanh máu UI)
                    // V13: Lọc bỏ màu đen tuyệt đối v < 8 (thường là hang động map) 
                    // Thanh máu UI thường là xám đen (v khoảng 15-30)
                    if (v >= 8 && v < 35 && s < 15) colDarkPixels++;
                }

                if (colHasRed)
                {
                    if (firstRedX == -1) firstRedX = x;
                    if (!foundGap)
                    {
                        realLastRedX = x;
                        continuousMissingRed = 0;
                    }
                }
                else if (firstRedX != -1)
                {
                    continuousMissingRed++;
                    if (continuousMissingRed > 5) foundGap = true;
                }

                // Kiểm tra 'Cột tối đồng nhất'
                bool isUniformDarkCol = (double)colDarkPixels / config.Height > 0.65;
                if (isUniformDarkCol)
                {
                    if (firstDarkX == -1) firstDarkX = x;
                    totalDarkPixels += colDarkPixels;
                    darkColsCount++;
                    currentContinuousDark++;
                    if (currentContinuousDark > maxContinuousDark) maxContinuousDark = currentContinuousDark;
                }
                else
                {
                    currentContinuousDark = 0;
                }
            }

            // === LOGIC QUYẾT ĐỊNH V19 ===
            int effectiveFirstX = firstRedX != -1 ? firstRedX : firstDarkX;
            if (effectiveFirstX == -1) return -1;

            int startOffset = effectiveFirstX - config.X;
            if (startOffset > config.Width * 0.15) return -1;

            // V19: SIGNATURE VERIFICATION (Xác thực Player cực đoan)
            // Áp dụng cho cả máu đỏ và nền đen của Player để loại bỏ NPC
            if (!isBoss)
            {
                int nameRangeY_Start = Math.Max(0, config.Y - 28);
                int nameRangeY_End = Math.Max(0, config.Y - 3);
                int identityPixels = 0;

                for (int nx = config.X; nx < config.X + config.Width; nx += 2)
                {
                    for (int ny = nameRangeY_Start; ny < nameRangeY_End; ny += 2)
                    {
                        if (nx >= imgHsv.Cols || ny >= imgHsv.Rows) continue;
                        Vec3b hsv = imgHsv.At<Vec3b>(ny, nx);
                        int h = hsv[0]; int s = hsv[1]; int v = hsv[2];

                        // 🔴 Chữ Hồng/Tím Sen (Tên địch)
                        bool isPink = (h >= 140 && h <= 178) && s >= 60 && v >= 60;
                        // 🟡 Chữ Vàng (Thẻ [Ngô], [Hán]...)
                        bool isYellow = (h >= 20 && h <= 35) && s >= 100 && v >= 100;
                        if (isPink || isYellow) identityPixels++;

                        // ⚪ Lọc NPC tên trắng
                        if (s < 30 && v > 150) identityPixels -= 2;
                    }
                }
                // Nếu là Player mà không thấy dấu hiệu tên Hồng/Vàng (NPC tên Trắng sẽ bị điểm âm)
                if (identityPixels < 5) return -1;
            }

            // 1. Ưu tiên nhận diện theo màu đỏ (còn máu)
            if (realLastRedX != -1 && totalRedPixels > 10)
            {
                double hpPercent = (double)(realLastRedX - config.X) / config.Width * 100.0;
                return Math.Clamp(hpPercent, 0.5, 100);
            }

            // 2. Fallback nền đen (Dành cho địch đã bị khóa nhưng HP cạn kiệt)
            double darkDensity = (double)totalDarkPixels / (config.Width * config.Height);
            double darkContinuity = (double)maxContinuousDark / config.Width;

            // V14: BOUNDARY & CONTEXT CHECK - Chống báo giả hang động cực đoan
            if (firstDarkX != -1 && darkDensity > 0.70 && darkContinuity > 0.80)
            {
                // 1. Kiểm tra biên phải: Thanh máu UI phải kết thúc tại W. 
                // Nếu bên phải ROI (cách 5px) vẫn đen -> Đó là Map
                int rightCheckX = config.X + config.Width + 5;
                if (rightCheckX < imgHsv.Cols)
                {
                    Vec3b rightPixel = imgHsv.At<Vec3b>(startY + config.Height / 2, rightCheckX);
                    if (rightPixel[2] < 35 && rightPixel[1] < 15) return -1; // Đen tràn biên -> Map
                }

                // 2. Kiểm tra đa điểm biên dọc (Trái - Giữa - Phải)
                int[] checkX = { config.X + 5, config.X + config.Width / 2, config.X + config.Width - 5 };
                int topY = Math.Max(0, config.Y - 5);
                int botY = Math.Min(imgHsv.Rows - 1, config.Y + config.Height + 5);
                
                int darkBoundaryPoints = 0;
                foreach (int x in checkX)
                {
                    if (x >= imgHsv.Cols) continue;
                    if (imgHsv.At<Vec3b>(topY, x)[2] < 35) darkBoundaryPoints++;
                    if (imgHsv.At<Vec3b>(botY, x)[2] < 35) darkBoundaryPoints++;
                }

                // Nếu quá 3 điểm xung quanh cũng tối -> Đây là mảng đen Map
                if (darkBoundaryPoints >= 3) return -1;

                // 3. Phân tích độ phẳng bề mặt (Variance)
                int midY = startY + config.Height / 2;
                double sumV = 0; double sumSqV = 0; int count = 0;
                for (int x = firstDarkX; x < firstDarkX + maxContinuousDark; x += 5)
                {
                    if (x >= imgHsv.Cols) break;
                    int vVal = imgHsv.At<Vec3b>(midY, x)[2];
                    sumV += vVal; sumSqV += (double)vVal * vVal;
                    count++;
                }

                if (count > 5)
                {
                    double avg = sumV / count;
                    double variance = (sumSqV / count) - (avg * avg);
                    // UI thực sự phẳng tuyệt đối (v14: variance < 2.5)
                    if (variance > 2.5) return -1;
                }

                return 0.1;
            }

            return -1;
        }

        /// <summary>
        /// Kiểm tra xem có thanh máu địch trên màn hình không (dựa trên quét pixel với config).
        /// </summary>
        public static bool HasHealthBarWithConfig(string screenPath, Models.HealthBarConfig config)
        {
            double hp = ScanHealthBarWithConfig(screenPath, config);
            return hp > 0;  // Nếu HP > 0% -> có thanh máu -> có địch
        }
    }
}