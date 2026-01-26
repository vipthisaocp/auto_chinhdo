using Microsoft.Win32;
using OpenCvSharp;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;

// Alias cho các thư viện
using DeviceData = AdvancedSharpAdbClient.Models.DeviceData;
using SyncService = AdvancedSharpAdbClient.SyncService;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// Alias để tránh xung đột
using IOPath = System.IO.Path;
using WPoint = System.Windows.Point;
using WRect = System.Windows.Rect;
using CvPoint = OpenCvSharp.Point;
using CvRect = OpenCvSharp.Rect;

// Import từ các module đã tách
using auto_chinhdo.Models;
using auto_chinhdo.Helpers;
using auto_chinhdo.Views;
using auto_chinhdo.Services;                 // New
using auto_chinhdo.Models.Scripting;         // New
using static auto_chinhdo.Helpers.OpenCvLogic;

namespace auto_chinhdo
{
    // DeviceItem và AutoState đã được chuyển sang Models/DeviceItem.cs

    public partial class MainWindow : System.Windows.Window
    {
        // === HẰNG SỐ TỐI ƯU HIỆU NĂNG ===
        private const int MAX_LOG_LINES = 500;
        private const int LINES_TO_KEEP = 400;
        private const int ADB_RESET_INTERVAL_MS = 20 * 60 * 1000;

        // === BIẾN ĐA LUỒNG VÀ KẾT NỐI ===
        private const int ADB_PORT = 5037;
        private readonly AdbClient _adb = new AdbClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, ADB_PORT));

        private readonly SemaphoreSlim _adbGate = new SemaphoreSlim(4);
        private readonly Random _random = new Random();
        private CancellationTokenSource? _adbResetCts;

        // === KHAI BÁO BIẾN CẤP PHÉP (ĐÃ CHUYỂN SANG FIREBASE) ===
        private bool _isLicensed = true; // Luôn true sau khi qua LoginWindow
        private CancellationTokenSource? _licenseCheckCts;
        private CancellationTokenSource? _lastSeenUpdateCts;
        private const int LICENSE_CHECK_INTERVAL_HOURS = 18; // Kiểm tra license mỗi 18 tiếng
        private const int LAST_SEEN_UPDATE_MINUTES = 10; // Cập nhật lastSeen mỗi 10 phút (tăng độ realtime)

        // --- Các biến trạng thái UI/Tool (Consolidated) ---
        private readonly ObservableCollection<DeviceItem> _devices = new();
        private DeviceData? _device;
        private readonly string _adbPathFile = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "adb_path.txt");
        private readonly string _appDir = AppDomain.CurrentDomain.BaseDirectory;
        private string[] _templates = Array.Empty<string>();
        private string? _previewTemplatePath;
        private double _threshold = 0.95;
        private int _pollIntervalMs = 700;
        private int _cooldownMs = 800;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource> _running = new(); // Auto runner

        // THÊM: Quản lý logic PK và Auto thường
        private readonly PkManager _pkManager;
        private readonly AutoManager _autoManager;
        private readonly IScriptEngine _scriptEngine; // New Script Engine

        // =================================================================================
        // HÀM QUAN TRỌNG NHẤT: PerformTap (Đồng nhất, trả về bool, Async an toàn)
        // =================================================================================
        private async Task<bool> PerformTap(DeviceData rawDevice, CvPoint center)
        {
            if (rawDevice == null) return false;

            int tapX = (int)Math.Round((double)center.X);
            int tapY = (int)Math.Round((double)center.Y);
            string command = $"input tap {tapX} {tapY}";

            try
            {
                // Chạy bất đồng bộ nhưng await để kiểm soát luồng ADB
                await Task.Run(() =>
                {
                    _adb.ExecuteRemoteCommand(command, rawDevice, null, Encoding.UTF8);
                });
                
                // Trễ bắt buộc 50ms sau TAP (dùng Task.Delay thay vì Wait)
                await Task.Delay(50);
                
                System.Diagnostics.Debug.WriteLine($"[{rawDevice.Serial}] ADB TAP OK: {tapX},{tapY}");
                return true;
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi Tap ADB [{rawDevice.Serial}]: {ex.Message}");
                return false;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DevicesList.ItemsSource = _devices;

            AdbPathBox.Text = LoadAdbPath();

            this.Closing += MainWindow_Closing;

            if (string.IsNullOrEmpty(ThresholdBox.Text)) ThresholdBox.Text = _threshold.ToString("F2");
            if (string.IsNullOrEmpty(PollBox.Text)) PollBox.Text = _pollIntervalMs.ToString();
            if (string.IsNullOrEmpty(CooldownBox.Text)) CooldownBox.Text = _cooldownMs.ToString();

            // Lấy Getter configs an toàn
            Func<double> getThreshold = () => Dispatcher.Invoke(() => double.TryParse(ThresholdBox.Text, out var th) ? th : _threshold);
            Func<int> getPollIntervalMs = () => Dispatcher.Invoke(() => int.TryParse(PollBox.Text, out var pi) ? pi : _pollIntervalMs);
            Func<int> getCooldownMs = () => Dispatcher.Invoke(() => int.TryParse(CooldownBox.Text, out var cd) ? cd : _cooldownMs);


            // KHỞI TẠO PK MANAGER
            _pkManager = new PkManager(
                adb: _adb, adbGate: _adbGate, running: _running, appendLog: AppendLog, performTap: PerformTap,
                captureScreenAsync: CaptureScreenAsync, ensureAdbServerIsHealthy: EnsureAdbServerIsHealthy,
                getThreshold: getThreshold, getPollIntervalMs: getPollIntervalMs, getCooldownMs: getCooldownMs,
                screenPathFor: ScreenPathFor,
                templatePathFor: TemplatePathFor // <<< TRUYỀN HÀM PHÂN GIẢI ĐƯỜNG DẪN MỚI >>>
            );

            // KHỞI TẠO SCRIPT ENGINE (Dùng service tạm)
            var scriptAdbService = new AdbService();
            _scriptEngine = new ScriptEngine(scriptAdbService);
            
            // Kết nối log từ ScriptEngine vào UI
            _scriptEngine.OnLog += (msg) => Dispatcher.Invoke(() => AppendLog($"[Script] {msg}"));

            // KHỞI TẠO AUTO MANAGER (Auto Thường)
            _autoManager = new AutoManager(
                adbGate: _adbGate, running: _running, appendLog: AppendLog, performTap: PerformTap,
                captureScreenAsync: CaptureScreenAsync, ensureAdbServerIsHealthy: EnsureAdbServerIsHealthy,
                getTemplatesFor: GetTemplatesFor, screenPathFor: ScreenPathFor,
                getThreshold: getThreshold, getPollIntervalMs: getPollIntervalMs, getCooldownMs: getCooldownMs
            );


            // LOG INITIATION
            AppendLog($"Tool khởi động: {DateTime.Now:HH:mm:ss}");
            AppendLog("✅ Hệ thống xác thực Firebase: Đã đăng nhập.");

            StartAdbResetLoop();
            StartLicenseCheckLoop(); // Kiểm tra license định kỳ
            StartLastSeenUpdateLoop(); // Cập nhật lastSeen định kỳ
            _ = RefreshDevicesAsync();
        }

        private bool _isClosing = false;
        private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isClosing) return;
            
            e.Cancel = true; // Hủy đóng tạm thời để chờ unregister
            _isClosing = true;

            SaveAdbPath(AdbPathBox.Text);
            _adbResetCts?.Cancel();
            _licenseCheckCts?.Cancel();
            _lastSeenUpdateCts?.Cancel();
            
            // Hiển thị thông báo chờ nếu cần (tùy chọn)
            AppendLog("⏳ Đang gỡ đăng ký thiết bị...");
            
            try
            {
                // Sử dụng Task.Run để không làm treo UI khi chờ
                await Task.Run(async () => {
                    await Services.FirebaseService.Instance.UnregisterDeviceAsync();
                });
                AppendLog("👋 Đã gỡ đăng ký thành công.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error unregistering: {ex.Message}");
            }
            finally
            {
                // Thực hiện đóng cửa sổ thực sự
                Dispatcher.Invoke(() => {
                    Close();
                });
            }
        }

        // === KIỂM TRA LICENSE ĐỌNH KỲ (18 TIẾNG) ===
        private void StartLicenseCheckLoop()
        {
            if (_licenseCheckCts != null) return;
            _licenseCheckCts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                while (!_licenseCheckCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Chờ 18 tiếng
                        await Task.Delay(TimeSpan.FromHours(LICENSE_CHECK_INTERVAL_HOURS), _licenseCheckCts.Token);
                        
                        AppendLog("🔄 Bắt đầu kiểm tra license định kỳ...");
                        
                        // Lấy thông tin đăng nhập đã lưu
                        string savedEmail = Properties.Settings.Default.SavedEmail;
                        string savedPassword = Properties.Settings.Default.SavedPassword;
                        
                        if (string.IsNullOrEmpty(savedEmail) || string.IsNullOrEmpty(savedPassword))
                        {
                            AppendLog("⚠️ Không có thông tin đăng nhập được lưu. Bỏ qua kiểm tra.");
                            continue;
                        }
                        
                        // Kiểm tra lại license với Firebase
                        var (success, message) = await Services.FirebaseService.Instance.LoginAsync(savedEmail, savedPassword);
                        
                        if (!success)
                        {
                            // License hết hạn hoặc tài khoản bị khóa
                            AppendLog($"❌ LICENSE HẾT HẠN HOẶC TÀI KHOẢN BỊ KHÓA: {message}");
                            
                            Dispatcher.Invoke(() =>
                            {
                                // Dừng tất cả Auto
                                StopAllAuto();
                                
                                // Hiển thị thông báo
                                MessageBox.Show(
                                    $"License của bạn đã hết hạn hoặc tài khoản bị khóa.\n\nLý do: {message}\n\nỨng dụng sẽ đóng lại. Vui lòng liên hệ Admin để gia hạn.",
                                    "⚠️ License Hết Hạn",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                                
                                // Đóng ứng dụng
                                Application.Current.Shutdown();
                            });
                            
                            break; // Thoát khỏi vòng lặp
                        }
                        else
                        {
                            AppendLog("✅ License vẫn còn hiệu lực.");
                        }
                    }
                    catch (TaskCanceledException) { break; }
                    catch (Exception ex)
                    {
                        AppendLog($"⚠️ Lỗi kiểm tra license: {ex.Message}");
                        // Tiếp tục kiểm tra lần sau
                    }
                }
            }, _licenseCheckCts.Token);
            
            AppendLog($"🕒 Đã bật kiểm tra license định kỳ: mỗi {LICENSE_CHECK_INTERVAL_HOURS} tiếng.");
        }

        // === CẬP NHẬT LASTSEEN ĐỌNH KỲ (1 TIẾNG) ===
        private void StartLastSeenUpdateLoop()
        {
            if (_lastSeenUpdateCts != null) return;
            _lastSeenUpdateCts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                while (!_lastSeenUpdateCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Chờ 1 tiếng
                        await Task.Delay(TimeSpan.FromMinutes(LAST_SEEN_UPDATE_MINUTES), _lastSeenUpdateCts.Token);
                        
                        // Cập nhật lastSeen
                        await Services.FirebaseService.Instance.UpdateLastSeenAsync();
                        System.Diagnostics.Debug.WriteLine("Updated lastSeen for device.");
                    }
                    catch (TaskCanceledException) { break; }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error updating lastSeen: {ex.Message}");
                    }
                }
            }, _lastSeenUpdateCts.Token);
        }

        // === HÀM GHI LOG VÀO UI VÀ DEBUG CONSOLE ===
        private void AppendLog(string message)
        {
            string formattedMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n";
            System.Diagnostics.Debug.Write(formattedMessage);

            if (CheckAccess())
            {
                UpdateDebugLogBox(formattedMessage);
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() => UpdateDebugLogBox(formattedMessage)));
            }
        }

        private void UpdateDebugLogBox(string message)
        {
            if (DebugLogBox != null)
            {
                DebugLogBox.AppendText(message);
                if (DebugLogBox.LineCount > MAX_LOG_LINES)
                {
                    int linesToRemove = DebugLogBox.LineCount - LINES_TO_KEEP;
                    int charIndex = 0;
                    try
                    {
                        charIndex = DebugLogBox.GetCharacterIndexFromLineIndex(linesToRemove);
                    }
                    catch { /* Bỏ qua lỗi Index nếu xảy ra */ }

                    if (charIndex > 0)
                    {
                        DebugLogBox.Text = DebugLogBox.Text.Substring(charIndex);
                        DebugLogBox.Text = $"[--- LOG CŨ ĐÃ BỊ CẮT, ĐANG CHỨA {LINES_TO_KEEP} DÒNG ---]\n{DebugLogBox.Text}";
                    }
                }
                DebugLogBox.ScrollToEnd();
            }
        }

        // =========================
        // HÀM KIỂM TRA LICENSE CŨ (ĐÃ GỠ BỎ)
        // =========================

        // --- HÀM TẢI/LƯU ADB PATH ---
        private string LoadAdbPath()
        {
            try
            {
                if (File.Exists(_adbPathFile))
                {
                    return File.ReadAllText(_adbPathFile).Trim();
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Lỗi tải đường dẫn Adb: {ex.Message}");
            }
            return string.Empty;
        }

        private void SaveAdbPath(string path)
        {
            try
            {
                File.WriteAllText(_adbPathFile, path);
            }
            catch (Exception ex)
            {
                AppendLog($"Lỗi lưu đường dẫn Adb: {ex.Message}");
            }
        }

        // --- HÀM KIỂM TRA VÀ RESET ADB ĐỊNH KỲ (YÊU CẦU MỚI) ---
        private void StartAdbResetLoop()
        {
            if (_adbResetCts != null) return;
            _adbResetCts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                while (!_adbResetCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(ADB_RESET_INTERVAL_MS, _adbResetCts.Token);
                        AppendLog("--- BẮT ĐẦU RESET ADB ĐỊNH KỲ ---");
                        await EnsureAdbServerIsHealthy(forceRestart: true);
                        AppendLog("--- KẾT THÚC RESET ADB ---");
                    }
                    catch (TaskCanceledException) { break; }
                    catch (Exception ex)
                    {
                        AppendLog($"LỖI NẶNG TRONG ADB RESET LOOP: {ex.Message}");
                        await Task.Delay(60000);
                    }
                }
            }, _adbResetCts.Token);
        }

        // HÀM ENSURE ADB HEALTHY (ĐÃ SỬA ĐỂ PHỤC HỒI KẾT NỐI VÀ REFRESH DEVICE)
        internal async Task EnsureAdbServerIsHealthy(bool forceRestart = false)
        {
            var adbPath = Dispatcher.Invoke(() => AdbPathBox.Text);

            if (string.IsNullOrWhiteSpace(adbPath) || !File.Exists(adbPath))
            {
                AppendLog("LỖI: Không tìm thấy adb.exe. Không thể kiểm tra sức khỏe.");
                if (!forceRestart) return;
                throw new FileNotFoundException("Không tìm thấy adb.exe. Vui lòng chọn đường dẫn hợp lệ.");
            }

            var server = new AdbServer();

            if (!forceRestart && server.GetStatus().IsRunning) return;

            try
            {
                if (!server.GetStatus().IsRunning) AppendLog("⚠️ ADB Server chưa chạy hoặc bị lỗi. Đang khởi động lại...");
                else AppendLog("⚠️ Buộc Reset ADB Server theo chu kỳ...");

                server.StartServer(adbPath, restartServerIfNewer: false);
                await Task.Delay(500);

                if (server.GetStatus().IsRunning)
                {
                    AppendLog("✅ ADB Server đã khởi động lại thành công!");
                    await RefreshDevicesAsync();
                }
                else
                {
                    AppendLog("❌ LỖI: Không thể khởi động lại adb.exe. Tool có thể không ổn định.");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"LỖI NẶNG KHI KHỞI ĐỘNG LẠI ADB: {ex.Message}");
                throw;
            }
        }

        // --- CÁC HÀM XỬ LÝ TEMPLATE VÀ UI CƠ BẢN ---

        private string MakeSafe(string s)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            foreach (var ch in invalid) s = s.Replace(ch, '_');
            return s.Replace(":", "_").Replace("/", "_").Replace("\\", "_").Trim();
        }
        private string DeviceTemplateDir(DeviceItem it)
        {
            var name = string.IsNullOrWhiteSpace(it.Title) ? it.Serial : it.Title;
            name = MakeSafe(name);
            var dir = IOPath.Combine(_appDir, "templates", name);
            Directory.CreateDirectory(dir);
            return dir;
        }

        // ĐẶT LẠI THÀNH PUBLIC ĐỂ TRUYỀN QUA DELEGATE VÀO AutoManager
        public string[] GetTemplatesFor(DeviceItem it)
        {
            var dir = DeviceTemplateDir(it);
            if (!Directory.Exists(dir)) return Array.Empty<string>();
            var files = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
            return Array.FindAll(files, f =>
                f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));
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

        private string ScreenPathFor(string serial) => IOPath.Combine(_appDir, $"screen_{serial.Replace(':', '_')}.png");

        // ===== UI Event Handlers =====
        private void BrowseAdb_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "adb.exe|adb.exe|All files|*.*", FileName = AdbPathBox.Text };
            if (dlg.ShowDialog() == true) AdbPathBox.Text = dlg.FileName;
        }

        private async void RefreshDevices_Click(object sender, RoutedEventArgs e) => await RefreshDevicesAsync();
        private async void Capture_Click(object sender, RoutedEventArgs e) => await CaptureScreenForSelectedAsync();
        private async void Match_Click(object sender, RoutedEventArgs e) => await MatchOnceSelectedAsync();
        private async void Tap_Click(object sender, RoutedEventArgs e) => await TapSelectedAsync();
        private void Crop_Click(object sender, RoutedEventArgs e) => StartCropFromCapturedImage();
        private async void StartAuto_Click(object sender, RoutedEventArgs e) => await StartAutoAsync();
        private void StopAuto_Click(object sender, RoutedEventArgs e) => StopAllAuto();

        // === CẤU HÌNH HP BAR (Quét pixel thanh máu) ===
        private void BtnConfigHpBar_Click(object sender, RoutedEventArgs e)
        {
            var it = GetUiCurrentDevice();
            if (it == null) { SetStatus("Chưa chọn thiết bị. Vui lòng chọn thiết bị trước."); return; }

            var screenPath = ScreenPathFor(it.Serial);
            if (!File.Exists(screenPath))
            {
                SetStatus("Chưa có ảnh chụp màn hình. Hãy bấm Chụp trước.");
                return;
            }

            // Mở CropWindow với chế độ đặc biệt để chọn vùng HP Bar
            var cw = new CropWindow(screenPath, DeviceTemplateDir(it));
            cw.Title = "🩸 Chọn vùng Thanh máu (HP Bar)";
            cw.SelectOnlyMode = true;  // Chỉ chọn vùng, không lưu file ảnh
            
            // Khi người dùng đóng CropWindow, lấy tọa độ vùng đã chọn
            if (cw.ShowDialog() == true && cw.SelectedRegion.HasValue)
            {
                var region = cw.SelectedRegion.Value;
                
                // Lấy mẫu màu từ vùng vừa chọn
                var (r, g, b) = HealthBarConfigService.SampleColorFromImage(
                    screenPath, 
                    (int)region.X, 
                    (int)region.Y, 
                    (int)region.Width, 
                    (int)region.Height
                );

                // Tạo config mới
                var config = new HealthBarConfig
                {
                    X = (int)region.X,
                    Y = (int)region.Y,
                    Width = (int)region.Width,
                    Height = (int)region.Height,
                    SampleR = r,
                    SampleG = g,
                    SampleB = b,
                    ToleranceR = 50,
                    ToleranceG = 30,
                    ToleranceB = 30,
                    TapX = 24,
                    TapY = 137,
                    NoEnemyTimeoutMs = 3000
                };

                // V25: Đọc loại mục tiêu cần cấu hình
                string targetType = (TargetTypeConfigBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "player";
                string targetName = targetType == "boss" ? "BOSS" : "Người chơi";

                // V22: Lưu config vào thư mục SHARED
                var sharedPkDir = IOPath.Combine(_appDir, "templates", "pk_shared");
                if (!Directory.Exists(sharedPkDir)) Directory.CreateDirectory(sharedPkDir);

                var configService = new HealthBarConfigService(sharedPkDir);
                if (configService.SaveConfig(config, targetType))
                {
                    AppendLog($"✅ [SHARED] Đã lưu cấu hình HP Bar cho {targetName}: X={config.X}, Y={config.Y}, W={config.Width}, H={config.Height}");
                    
                    // V25: Cảnh báo nếu ROI có vẻ sai (X=0)
                    if (config.X < 10)
                    {
                        AppendLog("⚠️ CẢNH BÁO: Tọa độ X đang quá nhỏ (X=0). Bạn nên dùng nút 'Chọn vùng' để kéo lại thanh máu chính xác!");
                        AppendLog("   Nếu X=0, Bot có thể sẽ không nhìn thấy thanh máu.");
                    }

                    AppendLog($"   Màu mẫu RGB: ({r}, {g}, {b})");
                    SetStatus($"✅ Đã lưu cấu hình HP Bar cho {targetName}");
                }
                else
                {
                    SetStatus($"❌ Lỗi lưu cấu hình HP Bar {targetName}.");
                }
            }
        }

        private async void StartPk_Click(object sender, RoutedEventArgs e) => await StartPkAutoAsync();
        private void PickTemplate_Click(object sender, RoutedEventArgs e)
        {
            var it = GetUiCurrentDevice();
            if (it == null) { SetStatus("Chưa chọn thiết bị."); return; }

            var dlg = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg|All files|*.*", Multiselect = true };
            if (dlg.ShowDialog() == true)
            {
                var dstDir = DeviceTemplateDir(it);
                int copied = 0;
                foreach (var src in dlg.FileNames)
                {
                    try
                    {
                        var dst = IOPath.Combine(dstDir, IOPath.GetFileName(src));
                        File.Copy(src, dst, overwrite: true);
                        copied++;
                        _previewTemplatePath = dst;
                    }
                    catch { /* ignore single failures */ }
                }
                if (!string.IsNullOrEmpty(_previewTemplatePath) && File.Exists(_previewTemplatePath))
                    TemplateImage.Source = LoadImageUnlocked(_previewTemplatePath);

                SetStatus($"Đã thêm {copied} template vào: {dstDir}");
            }
        }


        // ===== Core Logic =====
        private void EnsureAdbServer()
        {
            var adbPath = AdbPathBox.Text;

            if (string.IsNullOrWhiteSpace(adbPath) || !File.Exists(adbPath))
            {
                throw new FileNotFoundException("Không tìm thấy adb.exe. Vui lòng chọn đường dẫn hợp lệ.");
            }

            var adbDirectory = Path.GetDirectoryName(adbPath);

            if (adbDirectory == null)
            {
                throw new Exception("Đường dẫn adb.exe không hợp lệ.");
            }

            var server = new AdbServer();
            server.StartServer(adbPath, restartServerIfNewer: false);
        }

        public class DeviceInfo
        {
            public string Serial { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public DeviceData? Raw { get; set; } = null;
        }
        private async Task RefreshDevicesAsync()
        {
            string adbPath;
            try
            {
                adbPath = (string)Dispatcher.Invoke(() => AdbPathBox.Text);

                Dispatcher.Invoke(() => SetStatus("Đang dò thiết bị…"));
                AppendLog("Bắt đầu dò tìm thiết bị ADB...");

                EnsureAdbServer();
                await Task.Delay(200);

                var (list, titles) = await Task.Run(() =>
                {
                    var adbList = _adb.GetDevices();
                    var ldTitles = QueryLdTitles(adbPath);
                    return (adbList, ldTitles);
                });

                Dispatcher.Invoke(() =>
                {
                    _devices.Clear();

                    foreach (var d in list)
                    {
                        var title = titles.TryGetValue(d.Serial, out var t) ? t : d.Serial;

                        _devices.Add(new DeviceItem
                        {
                            Serial = d.Serial,
                            Title = title,
                            Width = 0,
                            Height = 0,
                            Raw = d
                        });
                    }
                });

                var deviceUpdateTasks = new List<Task>();
                foreach (var item in _devices)
                {
                    if (item.Raw is { } rawDevice)
                    {
                        deviceUpdateTasks.Add(Task.Run(() =>
                        {
                            var size = GetSize(rawDevice);
                            Dispatcher.Invoke(() =>
                            {
                                item.Width = size.W;
                                item.Height = size.H;
                                item.OnChanged(nameof(item.Width));
                                item.OnChanged(nameof(item.Height));
                                item.OnChanged(nameof(item.SizeText));
                            });
                        }));
                    }
                }

                await Task.WhenAll(deviceUpdateTasks);

                Dispatcher.Invoke(() =>
                {
                    DevicesList.ItemsSource = _devices;
                    if (_devices.Count > 0)
                    {
                        DevicesList.SelectedIndex = 0;
                        _device = _devices[0].Raw;
                        SetStatus($"✅ Tìm thấy {_devices.Count} thiết bị. Đang chọn: {_devices[0].Title}");
                        AppendLog($"Đã kết nối thành công {_devices.Count} thiết bị.");
                    }
                    else
                    {
                        _device = null;
                        SetStatus("⚠️ Không thấy thiết bị nào. Bật USB debugging cho từng LD.");
                        AppendLog("Không tìm thấy thiết bị nào. Kiểm tra ADB path và USB debugging.");
                    }
                });

            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => SetStatus("❌ Lỗi dò thiết bị: " + ex.Message));
                AppendLog($"LỖI DÒ THIẾT BỊ: {ex.Message}");
            }
        }

        private (int W, int H) GetSize(DeviceData d)
        {
            var recv = new AdvancedSharpAdbClient.Receivers.ConsoleOutputReceiver();
            _adb.ExecuteRemoteCommand("wm size", d, recv, Encoding.UTF8);
            var m = Regex.Match(recv.ToString(), @"Physical size:\s*(\d+)x(\d+)");
            if (m.Success) return (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
            return (0, 0);
        }

        // Lấy thiết bị đang chọn (ưu tiên checkbox)
        private DeviceItem? GetUiCurrentDevice()
        {
            foreach (var it in _devices) if (it.IsSelected) return it;
            return DevicesList.SelectedItem as DeviceItem;
        }

        // ===== Capture / Match / Tap một thiết bị (Logic giữ nguyên) =====
        private async Task CaptureScreenForSelectedAsync()
        {
            var it = GetUiCurrentDevice();
            if (it == null) { Dispatcher.Invoke(() => SetStatus("Chưa chọn thiết bị.")); return; }
            await CaptureScreenAsync(it);
        }

        internal async Task CaptureScreenAsync(DeviceItem it)
        {
            if (it.Raw is not { } rawDevice) return;

            try
            {
                var screen = ScreenPathFor(it.Serial);
                Dispatcher.Invoke(() => SetStatus($"Chụp màn hình: {it.Title}"));
                AppendLog($"Bắt đầu chụp màn hình cho {it.Title}...");

                await Task.Run(() =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (ScreenImage.Source != null) ScreenImage.Source = null;
                    });

                    var tmp = screen + ".tmp";

                    _adb.ExecuteRemoteCommand("screencap -p /sdcard/__s.png", rawDevice, null, Encoding.UTF8);

                    using var sync = new SyncService(_adb, rawDevice);

                    using (var fs = File.Create(tmp))
                    {
                        sync.Pull("/sdcard/__s.png", fs, null, false);
                    }

                    File.Copy(tmp, screen, overwrite: true);
                    File.Delete(tmp);
                });

                Dispatcher.Invoke(() => ScreenImage.Source = LoadImageUnlocked(screen));
                Dispatcher.Invoke(() => SetStatus($"Đã chụp: {screen}"));
                AppendLog($"Chụp màn hình hoàn tất: {screen}");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => SetStatus("Lỗi chụp màn hình: " + ex.Message));
                AppendLog($"LỖI CHỤP MÀN HÌNH: {ex.Message}");
            }
        }

        private async Task MatchOnceSelectedAsync()
        {
            var it = GetUiCurrentDevice();
            if (it == null) { Dispatcher.Invoke(() => SetStatus("Chưa chọn thiết bị.")); return; }
            var screen = ScreenPathFor(it.Serial);
            if (!File.Exists(screen)) { Dispatcher.Invoke(() => SetStatus("Chưa có ảnh màn hình. Bấm Chụp trước.")); return; }

            _threshold = Dispatcher.Invoke(() => double.TryParse(ThresholdBox.Text, out var th) ? th : 0.95);

            var tplSet = GetTemplatesFor(it);
            if ((tplSet == null || tplSet.Length == 0) && _templates.Length > 0)
                tplSet = _templates;

            if (tplSet == null || tplSet.Length == 0) { Dispatcher.Invoke(() => SetStatus("Chưa có template cho thiết bị này.")); return; }

            AppendLog($"Bắt đầu so khớp 1 lần cho {it.Title} ({tplSet.Length} mẫu, ngưỡng {_threshold:F3})...");

            // SỬ DỤNG OPEN CV LOGIC
            var best = await Task.Run(() => OpenCvLogic.MatchAny(screen, tplSet, _threshold));
            if (best == null)
            {
                Dispatcher.Invoke(() => SetStatus("Không khớp đủ ngưỡng."));
                AppendLog("Kết quả: KHÔNG tìm thấy mẫu khớp đủ ngưỡng.");
                return;
            }
            var (tpl, c, s) = best.Value;

            _previewTemplatePath = tpl;
            Dispatcher.Invoke(() => TemplateImage.Source = new BitmapImage(new Uri(tpl)));
            // SỬ DỤNG OPEN CV LOGIC
            await Task.Run(() => OpenCvLogic.DrawMarkerToFile(screen, c));
            Dispatcher.Invoke(() => ScreenImage.Source = LoadImageUnlocked(screen));
            Dispatcher.Invoke(() => SetStatus($"Match {IOPath.GetFileName(tpl)} score={s:F3} tại ({c.X},{c.Y})"));
            AppendLog($"Kết quả: ĐÃ KHỚP {IOPath.GetFileName(tpl)} (Score: {s:F3}) tại ({c.X},{c.Y}).");
        }


        private async Task TapSelectedAsync()
        {
            var it = GetUiCurrentDevice();
            if (it == null) { Dispatcher.Invoke(() => SetStatus("Chưa chọn thiết bị.")); return; }
            var screen = ScreenPathFor(it.Serial);
            if (!File.Exists(screen)) { Dispatcher.Invoke(() => SetStatus("Chưa có ảnh màn hình.")); return; }

            if (it.Raw is not { } rawDevice) return;

            _threshold = Dispatcher.Invoke(() => double.TryParse(ThresholdBox.Text, out var th) ? th : 0.95);

            var tplSet = GetTemplatesFor(it);
            if ((tplSet == null || tplSet.Length == 0) && _templates.Length > 0)
                tplSet = _templates;

            if (tplSet == null || tplSet.Length == 0) { Dispatcher.Invoke(() => SetStatus("Chưa có template cho thiết bị này.")); return; }

            AppendLog($"Bắt đầu Tap 1 lần cho {it.Title}...");

            // SỬ DỤNG OPEN CV LOGIC
            var best = await Task.Run(() => OpenCvLogic.MatchAny(screen, tplSet, _threshold));
            if (best == null)
            {
                Dispatcher.Invoke(() => SetStatus("Không khớp đủ ngưỡng."));
                AppendLog("Tap thất bại: Không tìm thấy mẫu khớp đủ ngưỡng.");
                return;
            }
            var (_, c, s) = best.Value;

            if (await PerformTap(rawDevice, c))
            {
                Dispatcher.Invoke(() => SetStatus($"Tap @{c.X},{c.Y} (score={s:F3}) - SUCCESS"));
                AppendLog($"Tap thành công: @{c.X},{c.Y}");
            }
            else
            {
                Dispatcher.Invoke(() => SetStatus($"Tap @{c.X},{c.Y} - FAILED ADB"));
            }
        }


        // ===== Auto per-device - MỖI DEVICE CÓ CHẾ ĐỘ RIÊNG =====
        private async Task StartAutoAsync()
        {
            _pollIntervalMs = Dispatcher.Invoke(() => int.TryParse(PollBox.Text, out var pi) ? pi : 700);
            _cooldownMs = Dispatcher.Invoke(() => int.TryParse(CooldownBox.Text, out var cd) ? cd : 800);
            _threshold = Dispatcher.Invoke(() => double.TryParse(ThresholdBox.Text, out var th) ? th : 0.95);

            int started = 0;
            AppendLog($"Bắt đầu Auto cho các thiết bị đã chọn (Ngưỡng: {_threshold:F3})...");

            foreach (var it in _devices)
            {
                if (!it.IsSelected) continue;
                if (_running.ContainsKey(it.Serial)) continue;

                // ĐỌC CHẾ ĐỘ TỪ DEVICE (KHÔNG PHẢI GLOBAL CHECKBOX)
                var deviceMode = it.SelectedAutoMode;

                // V22: Phân cấp Template (Shared & Device)
                var devicePkDir = IOPath.Combine(DeviceTemplateDir(it), "pk");
                var sharedPkDir = IOPath.Combine(_appDir, "templates", "pk_shared");
                
                // Tạo thư mục nếu cần
                if (!Directory.Exists(sharedPkDir)) Directory.CreateDirectory(sharedPkDir);
                if ((deviceMode == AutoMode.PK || deviceMode == AutoMode.Hybrid) && !Directory.Exists(devicePkDir))
                {
                    Directory.CreateDirectory(devicePkDir);
                }

                var cts = new CancellationTokenSource();
                _running[it.Serial] = cts;

                switch (deviceMode)
                {
                    case AutoMode.Hybrid:
                        // CHẾ ĐỘ HYBRID: Dùng PkHuntServiceV2 với Vital Signs detection
                        var hybridService = new PkHuntServiceV2(
                            sharedTemplateDir: sharedPkDir,
                            deviceTemplateDir: devicePkDir,
                            log: AppendLog,
                            captureScreen: async (device) => await CaptureScreenAsync(device),
                            getScreenPath: () => ScreenPathFor(it.Serial),
                            performTap: async (device, x, y) => await PerformTap(device, new CvPoint(x, y)),
                            getThreshold: () => _threshold
                        );
                        _ = Task.Run(() => hybridService.RunPkHuntLoopAsync(it, cts.Token));
                        AppendLog($"-> 🔥 [{it.Title}] Hybrid Mode V2 (Vital Signs) đã khởi động");
                        started++;
                        break;

                    case AutoMode.PK:
                        // CHẾ ĐỘ PK: Dùng PkHuntServiceV2 với Vital Signs detection
                        var pkService = new PkHuntServiceV2(
                            sharedTemplateDir: sharedPkDir,
                            deviceTemplateDir: devicePkDir,
                            log: AppendLog,
                            captureScreen: async (device) => await CaptureScreenAsync(device),
                            getScreenPath: () => ScreenPathFor(it.Serial),
                            performTap: async (device, x, y) => await PerformTap(device, new CvPoint(x, y)),
                            getThreshold: () => _threshold
                        );
                        _ = Task.Run(() => pkService.RunPkHuntLoopAsync(it, cts.Token));
                        AppendLog($"-> ⚔️ [{it.Title}] PK Mode V2 (Vital Signs) đã khởi động");
                        started++;
                        break;

                    default:
                        // CHẾ ĐỘ AUTO THƯỜNG
                        var tpls = GetTemplatesFor(it);
                        if (tpls.Length == 0)
                        {
                            SetStatus($"⚠️ {it.Title}: chưa có template");
                            AppendLog($"Bỏ qua {it.Title}: Không tìm thấy template.");
                            continue;
                        }
                        _ = Task.Run(() => _autoManager.AutoWorker(it, cts.Token));
                        AppendLog($"-> 📋 [{it.Title}] Auto Mode đã khởi động");
                        started++;
                        break;
                }
            }

            await Task.Yield();

            Dispatcher.Invoke(() => SetStatus(started > 0 
                ? $"Auto đang chạy cho {started} thiết bị (Ngưỡng: {_threshold:F3})." 
                : "Không có thiết bị nào được chọn."));
        }

        // HÀM AutoWorker CŨ ĐÃ BỊ XÓA (Chuyển sang AutoManager.cs)


        // HÀM PK AUTO ĐÃ ĐƯỢC CHUYỂN LOGIC VÀO PKMANAGER
        private async Task StartPkAutoAsync()
        {
            _pollIntervalMs = Dispatcher.Invoke(() => int.TryParse(PollBox.Text, out var pi) ? pi : 700);
            _cooldownMs = Dispatcher.Invoke(() => int.TryParse(CooldownBox.Text, out var cd) ? cd : 800);
            _threshold = Dispatcher.Invoke(() => double.TryParse(ThresholdBox.Text, out var th) ? th : 0.95);

            int started = 0;
            AppendLog($"Bắt đầu chế độ PK Auto (Single-Target Lock)...");

            foreach (var it in _devices)
            {
                if (!it.IsSelected) continue;
                if (_running.ContainsKey(it.Serial)) continue;

                var cts = new CancellationTokenSource();
                _running[it.Serial] = cts;

                it.CurrentState = AutoState.ATTACKING_ENEMY;

                // CHỈ GỌI VÀ CHUYỂN VIỆC CHO PKMANAGER
                _ = Task.Run(() => _pkManager.PkAutoWorker(it, cts.Token));
                AppendLog($"-> PK AutoWorker đã khởi động cho: {it.Title}");
                started++;
            }

            await Task.Yield();
            Dispatcher.Invoke(() => SetStatus(started > 0 ? $"PK Auto đang chạy cho {started} thiết bị." : "Không có thiết bị nào được chọn."));
        }


        private void StopAllAuto()
        {
            AppendLog("Yêu cầu dừng tất cả AutoWorker...");
            foreach (var kv in _running) kv.Value.Cancel();
            _running.Clear();
            SetStatus("Đã dừng auto.");
            AppendLog("Tất cả AutoWorker đã dừng.");
        }

        private void SetStatus(string s) => StatusText.Text = s;
        private string TemplatePathFor(string serial, string templateName)
        {
            // Tìm DeviceItem bằng serial
            var item = _devices.FirstOrDefault(d => d.Serial == serial);
            if (item == null)
            {
                // Fallback: nếu không tìm thấy device, dùng thư mục mặc định hoặc trả về tên file
                var name = serial.Replace(':', '_');
                var dir = IOPath.Combine(_appDir, "templates", name);
                return IOPath.Combine(dir, templateName);
            }

            // Trả về đường dẫn tuyệt đối: [Thư mục thiết bị] \ [Tên template]
            return IOPath.Combine(DeviceTemplateDir(item), templateName);
        }
        // Query LD Titles
        private Dictionary<string, string> QueryLdTitles(string adbPath)
        {
            var dict = new Dictionary<string, string>();
            string consolePath = string.Empty;

            try
            {
                var adbDir = System.IO.Path.GetDirectoryName(adbPath) ?? string.Empty;

                var consoleFiles = new List<string> { "dnconsole.exe", "ldconsole.exe" };
                foreach (var file in consoleFiles)
                {
                    var fullPath = System.IO.Path.Combine(adbDir, file);
                    if (System.IO.File.Exists(fullPath))
                    {
                        consolePath = fullPath;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(consolePath)) return dict;

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = consolePath,
                    Arguments = "list2",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };
                using var p = System.Diagnostics.Process.Start(psi);
                if (p == null) return dict;

                var output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(3000);

                if (p.ExitCode != 0) return dict;

                AppendLog($"LD Console Output: \n{output}");

                foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = line.Split(',');

                    if (parts.Length < 6) continue;

                    var indexStr = parts[0].Trim();      // Index LDPlayer (0, 1, 2,...)
                    var name = parts[1].Trim();         // Tên hiển thị
                    var adbPortStr = parts[5].Trim();   // Cổng ADB (Ví dụ: 5555, 5557)

                    if (!string.IsNullOrWhiteSpace(name) && int.TryParse(adbPortStr, out var ldPort) && int.TryParse(indexStr, out var ldIndex))
                    {
                        int standardEmulatorPort = 5554 + (ldIndex * 2);
                        dict[$"emulator-{standardEmulatorPort}"] = name;

                        int adbPortEmulatorFormat = standardEmulatorPort + 1;
                        dict[$"127.0.0.1:{adbPortEmulatorFormat}"] = name;

                        dict[$"127.0.0.1:{ldPort}"] = name;
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Lỗi khi truy vấn LD titles: {ex.Message}");
            }
            return dict;
        }


        // ===== Cắt ảnh từ ảnh chụp (lưu vào thư mục tool) =====
        private void StartCropFromCapturedImage()
        {
            var it = GetUiCurrentDevice();
            if (it == null) { SetStatus("Chưa chọn thiết bị."); return; }
            var screen = ScreenPathFor(it.Serial);
            if (!File.Exists(screen)) { SetStatus("Chưa có ảnh màn hình. Bấm Chụp trước.."); return; }

            var targetDir = DeviceTemplateDir(it);
            var dlg = new CropWindow(screen, targetDir) { Owner = this }; // saveDir = per-device dir

            // ShowDialog() sẽ chặn cho đến khi cửa sổ CropWindow đóng lại
            if (dlg.ShowDialog() == true && File.Exists(dlg.SavedPath))
            {
                _previewTemplatePath = dlg.SavedPath;
                TemplateImage.Source = LoadImageUnlocked(_previewTemplatePath);
                SetStatus($"Đã lưu template: {IOPath.GetFileName(dlg.SavedPath)} vào {targetDir}");
            }
        }

        private async void BtnRunScript_Click(object sender, RoutedEventArgs e)
        {
            var targetDevices = _devices.Where(x => x.IsSelected).ToList();
            if (targetDevices.Count == 0)
            {
                var current = GetUiCurrentDevice();
                if (current != null) targetDevices.Add(current);
            }

            if (targetDevices.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất 1 thiết bị.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Chọn file script
            var ofd = new OpenFileDialog
            {
                Filter = "JSON Script|*.json",
                Title = "Chọn kịch bản Auto",
                InitialDirectory = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts")
            };

            // Tạo thư mục Scripts nếu chưa có
            if (!Directory.Exists(ofd.InitialDirectory)) Directory.CreateDirectory(ofd.InitialDirectory);

            if (ofd.ShowDialog() != true) return;

            var scriptProfile = _scriptEngine.LoadScript(ofd.FileName);
            if (scriptProfile == null)
            {
                MessageBox.Show("Không thể tải kịch bản (Lỗi định dạng hoặc file trống).", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Chạy script
            foreach (var device in targetDevices)
            {
                if (_running.ContainsKey(device.Serial))
                {
                    AppendLog($"[{device.Title}] Đang bận. Bỏ qua chạy script.");
                    continue;
                }

                var cts = new CancellationTokenSource();
                _running[device.Serial] = cts;
                
                AppendLog($"🚀 [{device.Title}] Bắt đầu chạy kịch bản: {scriptProfile.Name}");
                device.CurrentState = AutoState.IDLE_OR_PRIMARY_TASK; // Reset state text?

                // Run async fire-and-forget but keep track
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _scriptEngine.RunScriptAsync(device, scriptProfile, cts.Token);
                        Dispatcher.Invoke(() => AppendLog($"✅ [{device.Title}] Kịch bản kết thúc: {scriptProfile.Name}"));
                    }
                    catch (TaskCanceledException)
                    {
                        Dispatcher.Invoke(() => AppendLog($"🛑 [{device.Title}] Kịch bản đã bị hủy."));
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => AppendLog($"❌ [{device.Title}] Lỗi kịch bản: {ex.Message}"));
                    }
                    finally
                    {
                        _running.TryRemove(device.Serial, out _);
                    }
                }, cts.Token);
            }
        }
        private void BtnOpenScriptEditor_Click(object sender, RoutedEventArgs e)
        {
            // Lấy thư mục template của thiết bị đang chọn (hoặc default)
            var currentDevice = GetUiCurrentDevice();
            string templateDir = currentDevice != null 
                ? DeviceTemplateDir(currentDevice) 
                : IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates", "default");

            if (!Directory.Exists(templateDir))
                Directory.CreateDirectory(templateDir);


            var editor = new ScriptEditorWindow(templateDir) { Owner = this };
            
            // Subscribe to record event if needed (for future implementation)
            // editor.OnRecordStepRequested += (action) => { ... };
            
            editor.ShowDialog();
        }

        private async void BtnOcrDebug_Click(object sender, RoutedEventArgs e)
        {
            var it = GetUiCurrentDevice();
            if (it == null) { SetStatus("Chưa chọn thiết bị."); return; }

            var screen = ScreenPathFor(it.Serial);
            if (!File.Exists(screen))
            {
                SetStatus("Chưa có ảnh màn hình. Bấm Chụp trước.");
                return;
            }

            SetStatus("🔍 Đang đọc text bằng OCR...");

            try
            {
                var ocrService = new Services.OcrService();
                var results = await Task.Run(() => ocrService.ReadTextFromImage(screen));

                if (results.Count == 0)
                {
                    AppendLog("🔍 OCR Debug: Không tìm thấy text nào trên ảnh.");
                    MessageBox.Show("OCR không tìm thấy text nào trên ảnh.\n\nCó thể do:\n- Font game đặc biệt\n- Ảnh có hiệu ứng/gradient\n- tessdata không đúng", 
                        "OCR Debug", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    AppendLog($"🔍 OCR Debug: Tìm thấy {results.Count} đoạn text:");
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"Tìm thấy {results.Count} đoạn text:\n");
                    
                    foreach (var r in results)
                    {
                        var line = $"  • \"{r.Text}\" tại ({r.X}, {r.Y}) - {r.Confidence:P0}";
                        AppendLog(line);
                        sb.AppendLine($"• \"{r.Text}\"  [{r.X},{r.Y}] ({r.Confidence:P0})");
                    }

                    MessageBox.Show(sb.ToString(), "OCR Debug - Text tìm thấy", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                SetStatus($"OCR: {results.Count} text.");
            }
            catch (Exception ex)
            {
                AppendLog($"❌ OCR Error: {ex.Message}");
                MessageBox.Show($"Lỗi OCR: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Hủy tất cả các task đang chạy
            _adbResetCts?.Cancel();
            _licenseCheckCts?.Cancel();
            _lastSeenUpdateCts?.Cancel();
            
            foreach (var cts in _running.Values)
            {
                cts.Cancel();
            }
            
            base.OnClosing(e);
            
            // Buộc thoát process để không bị zombie trong Task Manager
            Environment.Exit(0);
        }
    }
}
