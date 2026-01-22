using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

// Alias
using IOPath = System.IO.Path;
using CvPoint = OpenCvSharp.Point;
using CvRect = OpenCvSharp.Rect;
using DeviceData = AdvancedSharpAdbClient.Models.DeviceData;

// Import từ các module mới
using auto_chinhdo.Models;
using auto_chinhdo.Helpers;
using static auto_chinhdo.Helpers.OpenCvLogic;

namespace auto_chinhdo
{
    // Delegates để tương tác với UI và ADB
    public delegate void LogHandler(string message);
    public delegate Task<bool> TapHandler(DeviceData rawDevice, CvPoint center);
    public delegate Task CaptureScreenHandler(DeviceItem it);
    public delegate Task AdbHealthCheckHandler(bool forceRestart);
    public delegate string TemplatePathForHandler(string serial, string templateName);


    public class PkManager
    {
        // ADB và Concurrency Control (được truyền từ MainWindow)
        private readonly AdbClient _adb;
        private readonly SemaphoreSlim _adbGate;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource> _running;
        private readonly Random _random = new Random();

        // Dependencies (Delegates/Handlers)
        private readonly LogHandler _appendLog;
        private readonly TapHandler _performTap;
        private readonly CaptureScreenHandler _captureScreenAsync;
        private readonly AdbHealthCheckHandler _ensureAdbServerIsHealthy;
        private readonly TemplatePathForHandler _templatePathFor;

        // Configuration Getters (Lấy giá trị từ UI Thread một cách an toàn)
        private readonly Func<double> _getThreshold;
        private readonly Func<int> _getPollIntervalMs;
        private readonly Func<int> _getCooldownMs;

        // Path (được truyền từ MainWindow)
        private readonly Func<string, string> _screenPathFor;

        // Hàm dựng nhận tất cả các dependency từ MainWindow
        public PkManager(AdbClient adb, SemaphoreSlim adbGate, System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource> running,
                         LogHandler appendLog, TapHandler performTap,
                         CaptureScreenHandler captureScreenAsync, AdbHealthCheckHandler ensureAdbServerIsHealthy,
                         Func<double> getThreshold, Func<int> getPollIntervalMs, Func<int> getCooldownMs,
                         Func<string, string> screenPathFor,
                         TemplatePathForHandler templatePathFor)
        {
            _adb = adb;
            _adbGate = adbGate;
            _running = running;
            _appendLog = appendLog;
            _performTap = performTap;
            _captureScreenAsync = captureScreenAsync;
            _ensureAdbServerIsHealthy = ensureAdbServerIsHealthy;
            _templatePathFor = templatePathFor;

            _getThreshold = getThreshold;
            _getPollIntervalMs = getPollIntervalMs;
            _getCooldownMs = getCooldownMs;
            _screenPathFor = screenPathFor;
        }

        // Helper function: TryMatchAndTap (Async an toàn luồng)
        private async Task<bool> TryMatchAndTap(DeviceData rawDevice, string[] templateNames)
        {
            var screen = _screenPathFor(rawDevice.Serial);
            double currentThreshold = _getThreshold();

            var templatesToMatch = new List<string>();

            // CHUYỂN TÊN TEMPLATE THÀNH ĐƯỜNG DẪN TUYỆT ĐỐI
            foreach (var tplName in templateNames)
            {
                var fullPath = _templatePathFor(rawDevice.Serial, tplName);

                if (File.Exists(fullPath))
                {
                    templatesToMatch.Add(fullPath);
                }
                else
                {
                    _appendLog($"[{rawDevice.Serial}] ❌ KHÔNG TÌM THẤY file template: {tplName}. Đường dẫn: {fullPath}");
                }
            }

            if (templatesToMatch.Count == 0) return false;

            // Dùng MatchAny với đường dẫn tuyệt đối
            var match = OpenCvLogic.MatchAny(screen, templatesToMatch.ToArray(), currentThreshold);

            if (match.HasValue)
            {
                var (tpl, center, score) = match.Value;
                _appendLog($"[{rawDevice.Serial}] KHỚP THÀNH CÔNG: {IOPath.GetFileName(tpl)} (Score: {score:F3}). Đang Tap...");

                if (await _performTap(rawDevice, center))
                {
                    return true;
                }
                else
                {
                    _appendLog($"[{rawDevice.Serial}] ❌ Tap thất bại (lỗi ADB hoặc không gửi được lệnh). Template: {IOPath.GetFileName(tpl)}");
                    return false;
                }
            }
            return false;
        }

        // TÁC VỤ CHÍNH: PkAutoWorker (State Machine V2)
        public async Task PkAutoWorker(DeviceItem it, CancellationToken ct)
        {
            if (it.Raw is not { } rawDevice) return;

            // Cấu hình tọa độ và tên template mặc định
            CvPoint fixedAttackCenter = new CvPoint(38, 135); // Khóa mục tiêu (Góc trên trái)
            const string HealthBarTemplate = "thanh_mau_muc_tieu.png";
            const string FollowTemplate = "pk_theosau.png"; 
            const string StopFollowTemplate = "pk_botheosau.png"; // Template nút Bỏ theo sau
            const string Skill1Template = "skill1.png";
            const string AttackTemplate = "skill2.png";

            DateTime lastFoundPurple = DateTime.Now;
            bool isFollowing = false;

            try
            {
                _appendLog($"[{it.Title}] ⚔️ Bắt đầu chế độ PK Tự động (Nhận diện tên Tím).");

                while (!ct.IsCancellationRequested)
                {
                    await _ensureAdbServerIsHealthy(false);
                    if (ct.IsCancellationRequested) break;

                    await _adbGate.WaitAsync(ct);
                    try
                    {
                        // 1. Chụp màn hình
                        await _captureScreenAsync(it);
                        var screenPath = _screenPathFor(rawDevice.Serial);
                        double currentThreshold = _getThreshold();

                        // 2. Kiểm tra Thanh Máu (Đang trong trận đấu?)
                        // [CẬP NHẬT]: Dùng màu sắc thay vì Template để bám mục tiêu khi HP tụt
                        bool isTargetLocked = OpenCvLogic.IsTargetHealthBarVisible(screenPath);

                        if (isTargetLocked)
                        {
                            // === ĐANG PK (Đã khóa mục tiêu) ===
                            lastFoundPurple = DateTime.Now; // Vẫn đang PK
                            isFollowing = false;
                            
                            _appendLog($"[{it.Title}] 🔥 Đang PK mục tiêu. Spam kỹ năng...");
                            
                            // Spam kỹ năng (Await để tránh dồn ADB)
                            if (await TryMatchAndTap(rawDevice, new[] { Skill1Template }))
                            {
                                await Task.Delay(_getCooldownMs() / 2, ct);
                            }
                            await TryMatchAndTap(rawDevice, new[] { AttackTemplate });

                            await Task.Delay(_getCooldownMs(), ct);
                        }
                        else
                        {
                            // === CHƯA KHÓA MỤC TIÊU - Tìm tên màu Tím ===
                            var purpleNames = OpenCvLogic.FindColorLocations(screenPath, TargetColor.Purple);

                            if (purpleNames.Count > 0)
                            {
                                // Phát hiện đối thủ!
                                lastFoundPurple = DateTime.Now;

                                // [LOGIC MỚI]: Nếu đang ở trạng thái Theo sau, phải bấm Hủy trước để có thể Click vào người chơi
                                if (isFollowing)
                                {
                                    _appendLog($"[{it.Title}] 🛑 Thấy địch khi đang Theo sau! Đang bấm Bỏ Theo sau...");
                                    // Ưu tiên tìm template Bỏ theo sau trước
                                    if (await TryMatchAndTap(rawDevice, new[] { StopFollowTemplate, FollowTemplate, "theosau.png" }))
                                    {
                                        isFollowing = false;
                                        await Task.Delay(300, ct); // Chờ game hủy trạng thái theo sau
                                    }
                                }

                                // Lấy đối thủ gần tâm hoặc đầu tiên tìm được
                                var target = purpleNames[0];
                                _appendLog($"[{it.Title}] 🎯 Phát hiện {purpleNames.Count} đối thủ tên Tím. Target tại ({target.X},{target.Y}).");

                                // Click vào tên Tím để khóa mục tiêu (Await an toàn)
                                if (await _performTap(rawDevice, target))
                                {
                                    await Task.Delay(500, ct); // Chờ game nhận diện target
                                    // Click nháy vào ô khóa mục tiêu để xác nhận chắc chắn
                                    await _performTap(rawDevice, fixedAttackCenter);
                                }
                            }
                            else
                            {
                                // KHÔNG thấy đối thủ màu tím
                                double secondsSinceLastSeen = (DateTime.Now - lastFoundPurple).TotalSeconds;

                                if (secondsSinceLastSeen > 3.0 && !isFollowing)
                                {
                                    // Đã hết đối thủ quá 3 giây => Chuyển sang Theo sau
                                    _appendLog($"[{it.Title}] 🔄 Hết đối thủ quá 3s. Đang tìm nút 'Theo sau'...");
                                    
                                    if (await TryMatchAndTap(rawDevice, new[] { FollowTemplate, "theosau.png" }))
                                    {
                                        _appendLog($"[{it.Title}] ✅ Đã bấm nút Theo sau.");
                                        isFollowing = true;
                                    }
                                    else
                                    {
                                        _appendLog($"[{it.Title}] ⏳ Không tìm thấy template nút Theo sau ({FollowTemplate}).");
                                    }
                                }
                                else if (!isFollowing)
                                {
                                    _appendLog($"[{it.Title}] 🔍 Đang quét tìm đối thủ tên Tím...");
                                }
                            }
                        }
                    }
                    finally
                    {
                        // Thu gom rác định kỳ
                        if (_random.Next(1, 40) == 1) GC.Collect();
                        _adbGate.Release();
                    }

                    // Nghỉ giữa các chu kỳ để tránh treo luồng
                    await Task.Delay(_getPollIntervalMs(), ct);
                }
            }
            catch (TaskCanceledException) { _appendLog($"[{it.Title}] PK AutoWorker đã dừng."); }
            catch (Exception ex) { _appendLog($"❌ LỖI PK [{it.Title}]: {ex.Message}"); }
            finally { _running.TryRemove(it.Serial, out _); }
        }
    }
}