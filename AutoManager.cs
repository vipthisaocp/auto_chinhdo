using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// Alias
using CvPoint = OpenCvSharp.Point;
using IOPath = System.IO.Path;
using DeviceData = AdvancedSharpAdbClient.Models.DeviceData;

// Import từ các module mới
using auto_chinhdo.Models;
using auto_chinhdo.Helpers;
using static auto_chinhdo.Helpers.OpenCvLogic;

namespace auto_chinhdo
{
    // Delegate cho hàm lấy template
    public delegate string[] GetTemplatesForHandler(DeviceItem it);


    public class AutoManager
    {
        // Khai báo lại các dependency (delegate)
        private readonly SemaphoreSlim _adbGate;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource> _running;
        private readonly Random _random = new Random();

        private readonly LogHandler _appendLog;
        private readonly TapHandler _performTap;
        private readonly CaptureScreenHandler _captureScreenAsync;
        private readonly AdbHealthCheckHandler _ensureAdbServerIsHealthy;
        private readonly GetTemplatesForHandler _getTemplatesFor;
        private readonly Func<string, string> _screenPathFor;

        private readonly Func<double> _getThreshold;
        private readonly Func<int> _getPollIntervalMs;
        private readonly Func<int> _getCooldownMs;

        // Hàm dựng nhận tất cả các dependency từ MainWindow
        public AutoManager(SemaphoreSlim adbGate, System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource> running,
                           LogHandler appendLog, TapHandler performTap,
                           CaptureScreenHandler captureScreenAsync, AdbHealthCheckHandler ensureAdbServerIsHealthy,
                           GetTemplatesForHandler getTemplatesFor, Func<string, string> screenPathFor,
                           Func<double> getThreshold, Func<int> getPollIntervalMs, Func<int> getCooldownMs)
        {
            _adbGate = adbGate;
            _running = running;
            _appendLog = appendLog;
            _performTap = performTap;
            _captureScreenAsync = captureScreenAsync;
            _ensureAdbServerIsHealthy = ensureAdbServerIsHealthy;
            _getTemplatesFor = getTemplatesFor;
            _screenPathFor = screenPathFor;
            _getThreshold = getThreshold;
            _getPollIntervalMs = getPollIntervalMs;
            _getCooldownMs = getCooldownMs;
        }

        // TÁC VỤ CHÍNH: AutoWorker (Logic Auto Thường)
        public async Task AutoWorker(DeviceItem it, CancellationToken ct)
        {
            if (it.Raw is not { } rawDevice) return;

            try
            {
                var screen = _screenPathFor(it.Serial);
                double currentThreshold = _getThreshold();

                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(_random.Next(0, 200), ct);
                    await _ensureAdbServerIsHealthy(false);

                    if (ct.IsCancellationRequested) break;

                    await _adbGate.WaitAsync(ct);

                    try
                    {
                        await _captureScreenAsync(it);

                        var tpls = _getTemplatesFor(it);
                        (string tpl, CvPoint center, double score)? hit = null;

                        // SỬ DỤNG OPEN CV LOGIC CHUNG
                        hit = await Task.Run(() => OpenCvLogic.MatchAny(screen, tpls, currentThreshold), ct);

                        if (hit is { } h)
                        {
                            _appendLog($"[{it.Title}] KHỚP THÀNH CÔNG: {IOPath.GetFileName(h.tpl)} (Score: {h.score:F3}) tại TapCenter({h.center.X},{h.center.Y}).");

                            if (it.WaitAfterAppearMs > 0)
                            {
                                await Task.Delay(it.WaitAfterAppearMs, ct);
                            }

                            if (await _performTap(rawDevice, h.center))
                            {
                                // Lệnh Tap đã được gửi và hoàn thành
                            }

                            if (_getCooldownMs() > 0)
                            {
                                await Task.Delay(_getCooldownMs(), ct);
                            }
                        }
                        else
                        {
                            _appendLog($"[{it.Title}] Không tìm thấy mẫu khớp đủ ngưỡng.");
                        }

                        // THU GOM RÁC ĐỊNH KỲ 
                        if (_random.Next(1, 50) == 1)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            _appendLog("✅ Global: Đã gọi Garbage Collector để giải phóng bộ nhớ.");
                        }


                    }
                    finally
                    {
                        _adbGate.Release();
                    }

                    // Poll chu kỳ
                    if (_getPollIntervalMs() > 0)
                    {
                        await Task.Delay(_getPollIntervalMs(), ct);
                    }
                }
            }
            catch (TaskCanceledException) { _appendLog($"AutoWorker cho {it.Title} đã bị HỦY."); }
            catch (Exception ex) { _appendLog($"LỖI NGHIÊM TRỌNG AUTO [{it.Title}]: {ex.Message}"); }
            finally { _running.TryRemove(it.Serial, out _); }
        }
    }
}