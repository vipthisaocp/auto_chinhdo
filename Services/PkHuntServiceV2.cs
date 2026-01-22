using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using auto_chinhdo.Helpers;
using auto_chinhdo.Models;
using AdvancedSharpAdbClient.Models;
using OpenCvSharp;

namespace auto_chinhdo.Services
{
    /// <summary>
    /// PK Hunt Service V2 - Logic đơn giản, tối ưu cho 960x540
    /// Sử dụng "Vital Signs" detection: Quét cả HP (đỏ) và Tên (vàng/tím)
    /// </summary>
    public class PkHuntServiceV2
    {
        #region Constants & ROI
        
        // === ROI MỞ RỘNG cho 960x540 ===
        // Bao gồm cả TÊN (phía trên) và THANH MÁU (phía dưới)
        private static readonly Rect VITAL_SIGNS_ROI = new Rect(12, 135, 100, 40);
        
        // Vị trí tap khi phát hiện mục tiêu
        private const int TARGET_TAP_X = 24;
        private const int TARGET_TAP_Y = 137;
        
        // === Ngưỡng pixel tối thiểu (0.5% của 4000 pixels = 20 pixels) ===
        private const int MIN_PIXELS_THRESHOLD = 20;
        
        // === HSV Ranges cho các màu ===
        // Màu ĐỎ (Thanh máu) - 2 dải vì đỏ nằm ở 2 đầu của Hue
        private static readonly Scalar RED_LOW_1 = new Scalar(0, 100, 100);
        private static readonly Scalar RED_HIGH_1 = new Scalar(10, 255, 255);
        private static readonly Scalar RED_LOW_2 = new Scalar(160, 100, 100);
        private static readonly Scalar RED_HIGH_2 = new Scalar(180, 255, 255);
        
        // Màu VÀNG (Tên phe/bang hội)
        private static readonly Scalar YELLOW_LOW = new Scalar(20, 100, 100);
        private static readonly Scalar YELLOW_HIGH = new Scalar(35, 255, 255);
        
        // Màu TÍM/HỒNG (Tên địch)
        private static readonly Scalar PURPLE_LOW = new Scalar(140, 50, 50);
        private static readonly Scalar PURPLE_HIGH = new Scalar(170, 255, 255);
        
        // === Templates ===
        private const string THEOSAU = "theosau.png";
        private const string TRIEUTAP = "trieutap_tienden.png";
        private const double TEMPLATE_THRESHOLD = 0.70;
        
        // Skill templates
        private static readonly string[] SKILLS = new[]
        {
            "skill1.png", "skill2.png", "skill3.png",
            "skill4.png", "skill5.png", "skill6.png"
        };
        
        // Thời gian không thấy mục tiêu trước khi "Theo sau" (ms)
        private const int NO_TARGET_TIMEOUT_MS = 5000;
        
        #endregion
        
        #region Dependencies
        
        private readonly string _sharedTemplateDir;
        private readonly Action<string> _log;
        private readonly Func<DeviceItem, Task> _captureScreen;
        private readonly Func<string> _getScreenPath;
        private readonly Action<DeviceData, int, int> _performTap;
        
        #endregion
        
        #region Constructor
        
        public PkHuntServiceV2(
            string sharedTemplateDir,
            string deviceTemplateDir, // Giữ cho tương thích, không dùng
            Action<string> log,
            Func<DeviceItem, Task> captureScreen,
            Func<string> getScreenPath,
            Action<DeviceData, int, int> performTap,
            Func<double> getThreshold = null // Giữ cho tương thích
        )
        {
            _sharedTemplateDir = sharedTemplateDir;
            _log = log;
            _captureScreen = captureScreen;
            _getScreenPath = getScreenPath;
            _performTap = performTap;
        }
        
        #endregion
        
        #region Main Loop
        
        /// <summary>
        /// Vòng lặp chính PK Hunt V2
        /// </summary>
        public async Task RunPkHuntLoopAsync(DeviceItem device, CancellationToken ct)
        {
            _log("⚔️ [V2] Bắt đầu PK Hunt V2 (Vital Signs Detection)...");
            _log($"📐 ROI: ({VITAL_SIGNS_ROI.X},{VITAL_SIGNS_ROI.Y},{VITAL_SIGNS_ROI.Width},{VITAL_SIGNS_ROI.Height})");
            
            // Lấy DeviceData một lần để tránh cast nhiều lần
            var deviceData = (DeviceData)device.Raw;
            
            DateTime lastSeenTarget = DateTime.Now;
            int loopCount = 0;
            
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    loopCount++;
                    
                    // 1. Chụp màn hình
                    await _captureScreen(device);
                    var screenPath = _getScreenPath();
                    
                    if (string.IsNullOrEmpty(screenPath) || !File.Exists(screenPath))
                    {
                        await Task.Delay(500, ct);
                        continue;
                    }
                    
                    // 2. Kiểm tra Vital Signs (mục tiêu còn sống?)
                    var vitalSigns = IsTargetAlive(screenPath);
                    
                    if (vitalSigns.IsAlive)
                    {
                        // Log chi tiết mỗi 10 lần
                        if (loopCount % 10 == 1)
                        {
                            _log($"🎯 [V2] Phát hiện mục tiêu! HP:{vitalSigns.HasHealthBar} Tên:{vitalSigns.HasNameTag}");
                        }
                        
                        // Thực hiện PK
                        await PerformPK(deviceData, screenPath);
                        lastSeenTarget = DateTime.Now;
                        
                        // Delay ngắn trước khi quét tiếp
                        await Task.Delay(200, ct);
                        continue;
                    }
                    
                    // 3. Không thấy mục tiêu - kiểm tra timeout
                    var noTargetDuration = DateTime.Now - lastSeenTarget;
                    
                    if (noTargetDuration.TotalMilliseconds >= NO_TARGET_TIMEOUT_MS)
                    {
                        _log($"👥 [V2] Không thấy mục tiêu {NO_TARGET_TIMEOUT_MS / 1000}s → Theo sau...");
                        
                        // Bấm "Theo sau"
                        await FollowLeader(screenPath, deviceData);
                        
                        // Chờ 2s
                        await Task.Delay(2000, ct);
                        
                        // Chụp lại và thử "Triệu tập"
                        await _captureScreen(device);
                        var newScreenPath = _getScreenPath();
                        await TrySummon(newScreenPath, deviceData);
                        
                        // Reset timer
                        lastSeenTarget = DateTime.Now;
                    }
                    
                    // Delay giữa các vòng quét
                    await Task.Delay(300, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _log($"❌ [V2] Lỗi: {ex.Message}");
                    await Task.Delay(1000, ct);
                }
            }
            
            _log("🛑 [V2] PK Hunt V2 đã dừng.");
        }
        
        #endregion
        
        #region Vital Signs Detection
        
        /// <summary>
        /// Kiểm tra mục tiêu còn sống không bằng cách quét Vital Signs
        /// Trả về true nếu: Có HP ĐỎ HOẶC Có Tên VÀNG/TÍM
        /// </summary>
        private (bool IsAlive, bool HasHealthBar, bool HasNameTag) IsTargetAlive(string screenPath)
        {
            try
            {
                using var img = Cv2.ImRead(screenPath, ImreadModes.Color);
                if (img.Empty()) return (false, false, false);
                
                // Crop ROI
                using var roi = new Mat(img, VITAL_SIGNS_ROI);
                
                // Chuyển sang HSV
                using var hsv = new Mat();
                Cv2.CvtColor(roi, hsv, ColorConversionCodes.BGR2HSV);
                
                // === Kiểm tra MÀU ĐỎ (Thanh máu) ===
                using var redMask1 = new Mat();
                using var redMask2 = new Mat();
                using var redMask = new Mat();
                
                Cv2.InRange(hsv, RED_LOW_1, RED_HIGH_1, redMask1);
                Cv2.InRange(hsv, RED_LOW_2, RED_HIGH_2, redMask2);
                Cv2.BitwiseOr(redMask1, redMask2, redMask);
                
                int redPixels = Cv2.CountNonZero(redMask);
                bool hasHealthBar = redPixels >= MIN_PIXELS_THRESHOLD;
                
                // === Kiểm tra MÀU VÀNG (Tên phe) ===
                using var yellowMask = new Mat();
                Cv2.InRange(hsv, YELLOW_LOW, YELLOW_HIGH, yellowMask);
                int yellowPixels = Cv2.CountNonZero(yellowMask);
                
                // === Kiểm tra MÀU TÍM/HỒNG (Tên địch) ===
                using var purpleMask = new Mat();
                Cv2.InRange(hsv, PURPLE_LOW, PURPLE_HIGH, purpleMask);
                int purplePixels = Cv2.CountNonZero(purpleMask);
                
                bool hasNameTag = (yellowPixels >= MIN_PIXELS_THRESHOLD) || 
                                  (purplePixels >= MIN_PIXELS_THRESHOLD);
                
                // Mục tiêu còn sống nếu có HP HOẶC có Tên
                bool isAlive = hasHealthBar || hasNameTag;
                
                return (isAlive, hasHealthBar, hasNameTag);
            }
            catch
            {
                return (false, false, false);
            }
        }
        
        #endregion
        
        #region PK Actions
        
        /// <summary>
        /// Thực hiện PK: Tap mục tiêu + Xả 6 skills
        /// </summary>
        private async Task PerformPK(DeviceData device, string screenPath)
        {
            // 1. Tap vào mục tiêu
            _performTap(device, TARGET_TAP_X, TARGET_TAP_Y);
            await Task.Delay(100);
            
            // 2. Xả skills
            foreach (var skill in SKILLS)
            {
                var skillPath = Path.Combine(_sharedTemplateDir, skill);
                
                if (!File.Exists(skillPath)) continue;
                
                var result = OpenCvLogic.MatchAny(screenPath, new[] { skillPath }, TEMPLATE_THRESHOLD);
                
                if (result.HasValue)
                {
                    _performTap(device, result.Value.center.X, result.Value.center.Y);
                    await Task.Delay(120); // Delay ngắn giữa các skill
                }
            }
        }
        
        #endregion
        
        #region Navigation Actions
        
        /// <summary>
        /// Bấm nút "Theo sau"
        /// </summary>
        private async Task FollowLeader(string screenPath, DeviceData device)
        {
            var templatePath = Path.Combine(_sharedTemplateDir, THEOSAU);
            
            if (!File.Exists(templatePath))
            {
                _log($"⚠️ [V2] Không tìm thấy template: {THEOSAU}");
                return;
            }
            
            var result = OpenCvLogic.MatchAny(screenPath, new[] { templatePath }, TEMPLATE_THRESHOLD);
            
            if (result.HasValue)
            {
                _performTap(device, result.Value.center.X, result.Value.center.Y);
                _log($"✅ [V2] Bấm 'Theo sau' tại ({result.Value.center.X},{result.Value.center.Y})");
            }
            else
            {
                _log("⚠️ [V2] Không tìm thấy nút 'Theo sau'");
            }
        }
        
        /// <summary>
        /// Thử bấm "Triệu tập" nếu có
        /// </summary>
        private async Task TrySummon(string screenPath, DeviceData device)
        {
            var templatePath = Path.Combine(_sharedTemplateDir, TRIEUTAP);
            
            if (!File.Exists(templatePath)) return;
            
            var result = OpenCvLogic.MatchAny(screenPath, new[] { templatePath }, 0.65); // Threshold thấp hơn
            
            if (result.HasValue)
            {
                _performTap(device, result.Value.center.X, result.Value.center.Y);
                _log($"✅ [V2] Bấm 'Triệu tập' tại ({result.Value.center.X},{result.Value.center.Y})");
            }
        }
        
        #endregion
    }
}
