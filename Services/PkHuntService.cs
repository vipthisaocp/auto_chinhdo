using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using auto_chinhdo.Helpers;
using auto_chinhdo.Models;
using AdvancedSharpAdbClient.Models;

namespace auto_chinhdo.Services
{
    /// <summary>
    /// Service x�?lý logic PK t�?động - State Machine V36
    /// Logic rõ ràng: INIT �?SCAN_PLAYER �?PK/FOLLOW �?FIND_BOSS �?FIGHT_BOSS �?SCOUT_PK
    /// </summary>
    public class PkHuntService
    {
        // Enum State
        private enum BotState
        {
            INIT,           // Khởi tạo: Bấm Lan cản + Tab Người chơi
            SCAN_PLAYER,    // Quét Player
            PK,             // PK
            FOLLOW,         // Theo sau
            FIND_BOSS,      // Tìm Boss
            FIGHT_BOSS,     // Đánh Boss
            SCOUT_PK        // Thám thính PK (khi đánh Boss)
        }

        private readonly Action<string> _log;
        private readonly Func<DeviceItem, Task> _captureScreen;
        private readonly Func<string> _getScreenPath;
        private readonly Action<DeviceData, int, int> _performTap;
        private readonly Func<double> _getThreshold;
        private readonly string _sharedTemplateDir;
        private readonly string _deviceTemplateDir;
        
        // File logging
        private StreamWriter _logWriter;
        private readonly string _logFilePath;
        
        // HP Bar Config (ch�?dùng Player)
        private readonly HealthBarConfig _playerHpConfig;

        // Template names
        private const string LANCAN_TEMPLATE = "lancan.png";
        private const string NGUOICHOI_GOCTRAI_TEMPLATE = "Nguoichoigoctrai.png";
        private const string QUAIVAT_TAB_TEMPLATE = "quaivat.png";
        private const string THEOSAU_TEMPLATE = "theosau.png";
        private const string BOTHEOSAU_TEMPLATE = "botheosau.png";
        private const string TRIEUTAP_TIENDEN_TEMPLATE = "trieutap_tienden.png";
        private const string TANCONGBOSS_TEMPLATE = "tancongboss.png";
        private const string CONGKICH_BOSS_TEMPLATE = "congkich.png";
        private const string NUTTREOMAY_TEMPLATE = "nuttreomay.png";
        private const string DANGTREOMAY_TEMPLATE = "dangtreomay.png";
        private const string KIEMTRE_TEMPLATE = "kiemtre.png";
        
        // Skill templates (6 skills)
        private static readonly string[] SKILL_TEMPLATES = new[]
        {
            "skill1.png", "skill2.png", "skill3.png",
            "skill4.png", "skill5.png", "skill6.png"
        };

        // Threshold
        private const double NAV_THRESHOLD = 0.80;
        private const double SUMMON_THRESHOLD = 0.70;
        
        // Timers
        private DateTime _lastSeenPlayer = DateTime.Now;
        private DateTime _bossStartTime = DateTime.Now;
        private DateTime? _waitingSummonStartTime = null;
        private DateTime _lastGrindCheckTime = DateTime.MinValue;
        private int _initRetryCount = 0; // Đếm s�?lần th�?INIT

        public PkHuntService(
            string sharedTemplateDir,
            string deviceTemplateDir,
            Action<string> log,
            Func<DeviceItem, Task> captureScreen,
            Func<string> getScreenPath,
            Action<DeviceData, int, int> performTap,
            Func<double> getThreshold)
        {
            _sharedTemplateDir = sharedTemplateDir;
            _deviceTemplateDir = deviceTemplateDir;
            _log = log;
            _captureScreen = captureScreen;
            _getScreenPath = getScreenPath;
            _performTap = performTap;
            _getThreshold = getThreshold;
            
            // Khởi tạo file logging
            _logFilePath = Path.Combine(sharedTemplateDir, "pk_hunt_log.txt");
            try
            {
                _logWriter = new StreamWriter(_logFilePath, append: true);
                _logWriter.AutoFlush = true;
                LogToFile($"\r\n========== Bắt đầu phiên mới: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========");
            }
            catch (Exception ex)
            {
                LogBoth($"⚠️ Không th�?tạo file log: {ex.Message}");
            }

            // Load HP Bar Config (ch�?Player)
            var configService = new HealthBarConfigService(_sharedTemplateDir);
            _playerHpConfig = configService.LoadConfig("player");
            
            LogToFile($"📊 Player ROI: X={_playerHpConfig.X}, Y={_playerHpConfig.Y}");
            LogBoth($"📊 Player ROI: X={_playerHpConfig.X}, Y={_playerHpConfig.Y}");

            if (!Directory.Exists(_sharedTemplateDir)) Directory.CreateDirectory(_sharedTemplateDir);
            if (!Directory.Exists(_deviceTemplateDir)) Directory.CreateDirectory(_deviceTemplateDir);
        }

        public async Task RunPkHuntLoopAsync(DeviceItem device, CancellationToken ct)
        {
            if (device.Raw is not DeviceData rawDevice)
            {
                LogBoth("�?Device không hợp l�?);
                return;
            }

            LogBoth("⚔️ Bắt đầu ch�?đ�?PK Hunt (State Machine V36)...");

            BotState currentState = BotState.INIT;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _captureScreen(device);
                    var screenPath = _getScreenPath();
                    
                    if (!File.Exists(screenPath))
                    {
                        await Task.Delay(300, ct);
                        continue;
                    }

                    double threshold = _getThreshold();
                    
                    LogBoth($"🤖 State: {currentState}");

                    switch (currentState)
                    {
                        case BotState.INIT:
                            currentState = await HandleInit(screenPath, rawDevice, threshold, ct, device);
                            break;
                            
                        case BotState.SCAN_PLAYER:
                            currentState = await HandleScanPlayer(screenPath, rawDevice, threshold, ct);
                            break;
                            
                        case BotState.PK:
                            currentState = await HandlePK(screenPath, rawDevice, threshold, ct);
                            break;
                            
                        case BotState.FOLLOW:
                            currentState = await HandleFollow(screenPath, rawDevice, threshold, ct, device);
                            break;
                            
                        case BotState.FIND_BOSS:
                            currentState = await HandleFindBoss(screenPath, rawDevice, threshold, ct);
                            break;
                            
                        case BotState.FIGHT_BOSS:
                            currentState = await HandleFightBoss(screenPath, rawDevice, threshold, ct);
                            break;
                            
                        case BotState.SCOUT_PK:
                            currentState = await HandleScoutPK(screenPath, rawDevice, threshold, ct, device);
                            break;
                    }

                    await Task.Delay(300, ct);
                }
                catch (Exception ex)
                {
                    LogBoth($"�?Lỗi: {ex.Message}");
                    await Task.Delay(1000, ct);
                }
            }

            LogBoth("🛑 Đã dừng ch�?đ�?PK Hunt.");
        }

        // State 1: KHỞI TẠO
        private async Task<BotState> HandleInit(string screenPath, DeviceData rawDevice, double threshold, CancellationToken ct, DeviceItem device)
        {
            LogBoth($"🔧 Khởi tạo (Lần {_initRetryCount + 1}/3)...");
            
            // Bước 1: Th�?bấm Lan cản (không bắt buộc - có th�?không có popup)
            await TryMatchAndTap(screenPath, LANCAN_TEMPLATE, rawDevice, threshold, "Lan cản");
            await Task.Delay(300, ct);
            
            // Bước 2: Chụp lại màn hình
            await _captureScreen(device);
            screenPath = _getScreenPath();
            
            // Bước 3: Kiểm tra xem đã �?Tab Người chơi chưa
            // Nếu thấy nút "quaivat.png" (Tab Quái vật) �?Đang �?Tab Người chơi
            bool isInPlayerTab = await TryMatchOnly(screenPath, QUAIVAT_TAB_TEMPLATE, rawDevice, threshold, "Kiểm tra Tab");
            
            if (isInPlayerTab)
            {
                LogBoth("�?Đã �?Tab Người chơi �?B�?qua INIT, chuyển sang SCAN_PLAYER");
                _initRetryCount = 0;
                return BotState.SCAN_PLAYER;
            }
            
            // Bước 4: Chưa �?Tab Người chơi �?Th�?bấm Tab Người chơi
            LogBoth("⚠️ Chưa �?Tab Người chơi �?Th�?bấm Tab Người chơi...");
            bool switched = await TryMatchAndTap(screenPath, NGUOICHOI_GOCTRAI_TEMPLATE, rawDevice, threshold, "Tab Người chơi");
            if (switched)
            {
                LogBoth("�?Đã chuyển sang Tab Người chơi �?Chuyển sang SCAN_PLAYER");
                _initRetryCount = 0;
                await Task.Delay(500, ct);
                return BotState.SCAN_PLAYER;
            }
            
            // Bước 5: Không tìm thấy template �?Th�?lại hoặc b�?qua
            _initRetryCount++;
            if (_initRetryCount >= 3)
            {
                LogBoth("⚠️ Không tìm thấy template sau 3 lần th�?�?B�?qua INIT, chuyển sang SCAN_PLAYER");
                _initRetryCount = 0;
                return BotState.SCAN_PLAYER;
            }
            
            return BotState.INIT; // Th�?lại
        }

        // State 2: QUÉT PLAYER
        private async Task<BotState> HandleScanPlayer(string screenPath, DeviceData rawDevice, double threshold, CancellationToken ct)
        {
            // Quét ROI thanh máu Player
            double hpPercent = OpenCvLogic.ScanHealthBarWithConfig(screenPath, _playerHpConfig, isBoss: false);
            
            if (hpPercent > 0)
            {
                LogBoth($"🔍 Thấy Player: {hpPercent:F1}%");
                _lastSeenPlayer = DateTime.Now;
                return BotState.PK;
            }
            
            // Không thấy Player 5s �?Theo sau
            var noPlayerDuration = DateTime.Now - _lastSeenPlayer;
            if (noPlayerDuration.TotalSeconds >= 5)
            {
                LogBoth($"�?Không thấy Player {noPlayerDuration.TotalSeconds:F0}s �?Theo sau");
                return BotState.FOLLOW;
            }
            
            return BotState.SCAN_PLAYER; // Tiếp tục quét
        }

        // State 3: PK
        private async Task<BotState> HandlePK(string screenPath, DeviceData rawDevice, double threshold, CancellationToken ct)
        {
            // B�?Theo sau (nếu đang theo)
            await TryMatchAndTap(screenPath, BOTHEOSAU_TEMPLATE, rawDevice, threshold, "B�?Theo sau");
            
            // Tap vào thanh máu Player
            _performTap(rawDevice, _playerHpConfig.TapX, _playerHpConfig.TapY);
            LogBoth($"🎯 Tap Player ({_playerHpConfig.TapX}, {_playerHpConfig.TapY})");
            
            // X�?skill
            LogBoth("⚔️ X�?skill combo!");
            foreach (var skill in SKILL_TEMPLATES)
            {
                await TryMatchAndTap(screenPath, skill, rawDevice, threshold, skill.Replace(".png", ""));
                await Task.Delay(100, ct);
            }
            
            await Task.Delay(300, ct);
            return BotState.SCAN_PLAYER; // Quay lại quét
        }

        // State 4: THEO SAU
        private async Task<BotState> HandleFollow(string screenPath, DeviceData rawDevice, double threshold, CancellationToken ct, DeviceItem device)
        {
            // Bấm Theo sau
            bool followed = await TryMatchAndTap(screenPath, THEOSAU_TEMPLATE, rawDevice, threshold, "Theo sau");
            if (followed)
            {
                _waitingSummonStartTime = DateTime.Now;
                await Task.Delay(500, ct);
                
                // Kiểm tra Triệu tập
                await _captureScreen(device);
                screenPath = _getScreenPath();
                bool foundSummon = await TryMatchAndTap(screenPath, TRIEUTAP_TIENDEN_TEMPLATE, rawDevice, SUMMON_THRESHOLD, "Triệu tập");
                
                if (foundSummon)
                {
                    _waitingSummonStartTime = null;
                }
                
                return BotState.FIND_BOSS;
            }
            
            return BotState.FOLLOW; // Th�?lại
        }

        // State 5: TÌM BOSS
        private async Task<BotState> HandleFindBoss(string screenPath, DeviceData rawDevice, double threshold, CancellationToken ct)
        {
            // Tìm nút Tấn công Boss
            bool foundBossBtn = await TryMatchAndTap(screenPath, TANCONGBOSS_TEMPLATE, rawDevice, threshold, "Tấn công Boss");
            if (foundBossBtn)
            {
                LogBoth("👹 Bắt đầu đánh Boss");
                _bossStartTime = DateTime.Now;
                await Task.Delay(500, ct);
                return BotState.FIGHT_BOSS;
            }
            
            // Không thấy Boss �?Quay lại quét Player
            return BotState.SCAN_PLAYER;
        }

        // State 6: ĐÁNH BOSS
        private async Task<BotState> HandleFightBoss(string screenPath, DeviceData rawDevice, double threshold, CancellationToken ct)
        {
            // Bấm Công kích đ�?duy trì đánh Boss
            await TryMatchAndTap(screenPath, CONGKICH_BOSS_TEMPLATE, rawDevice, threshold, "Công kích Boss");
            
            // Kiểm tra 10s �?Thám thính PK
            var bossDuration = DateTime.Now - _bossStartTime;
            if (bossDuration.TotalSeconds >= 10)
            {
                LogBoth($"🛡�?Đánh Boss {bossDuration.TotalSeconds:F0}s �?Thám thính PK");
                return BotState.SCOUT_PK;
            }
            
            return BotState.FIGHT_BOSS; // Tiếp tục đánh
        }

        // State 7: THÁM THÍNH PK
        private async Task<BotState> HandleScoutPK(string screenPath, DeviceData rawDevice, double threshold, CancellationToken ct, DeviceItem device)
        {
            // Bấm Tab Người chơi
            bool backToPlayer = await TryMatchAndTap(screenPath, NGUOICHOI_GOCTRAI_TEMPLATE, rawDevice, threshold, "Thám thính PK");
            if (backToPlayer)
            {
                await Task.Delay(500, ct);
                
                // Quét Player
                await _captureScreen(device);
                screenPath = _getScreenPath();
                double pkHp = OpenCvLogic.ScanHealthBarWithConfig(screenPath, _playerHpConfig, isBoss: false);
                
                if (pkHp > 0)
                {
                    LogBoth($"⚠️ Phát hiện địch khi đánh Boss: {pkHp:F1}%");
                    _lastSeenPlayer = DateTime.Now;
                    return BotState.PK; // Ưu tiên PK
                }
                
                LogBoth("�?Không có địch �?Quay lại đánh Boss");
                // Bấm lại Tấn công Boss đ�?quay Tab Quái vật
                await TryMatchAndTap(screenPath, TANCONGBOSS_TEMPLATE, rawDevice, threshold, "Quay Boss");
                _bossStartTime = DateTime.Now;
                await Task.Delay(500, ct);
                return BotState.FIGHT_BOSS;
            }
            
            return BotState.SCOUT_PK; // Th�?lại
        }



        // Helper: TryMatchAndTap
        private async Task<bool> TryMatchAndTap(string screenPath, string templateName, DeviceData rawDevice, double threshold, string label)
        {
            var templatePath = Path.Combine(_sharedTemplateDir, templateName);
            
            // Debug: Kiểm tra file có tồn tại không
            if (!File.Exists(templatePath))
            {
                LogBoth($"�?[{label}] Template không tồn tại: {templatePath}");
                return false;
            }
            
            var templates = new string[] { templatePath };
            var result = OpenCvLogic.MatchAny(screenPath, templates, threshold);
            
            if (result.HasValue)
            {
                var (tpl, center, score) = result.Value;
                _performTap(rawDevice, center.X, center.Y);
                LogBoth($"�?[{label}] Tap ({center.X}, {center.Y}) - Score: {score:F2}");
                return true;
            }
            
            // Debug: Th�?với threshold thấp hơn đ�?xem có gần match không
            var debugResult = OpenCvLogic.MatchAny(screenPath, templates, 0.5);
            if (debugResult.HasValue)
            {
                var (_, _, debugScore) = debugResult.Value;
                LogBoth($"💡 [{label}] Near match: {debugScore:F2} (Cần >= {threshold:F2})");
            }
            
            return false;
        }
        
        // Helper: TryMatchOnly (ch�?match, không tap)
        private async Task<bool> TryMatchOnly(string screenPath, string templateName, DeviceData rawDevice, double threshold, string label)
        {
            var templatePath = Path.Combine(_sharedTemplateDir, templateName);
            
            if (!File.Exists(templatePath))
            {
                return false;
            }
            
            var templates = new string[] { templatePath };
            var result = OpenCvLogic.MatchAny(screenPath, templates, threshold);
            
            if (result.HasValue)
            {
                var (_, _, score) = result.Value;
                LogBoth($"🔍 [{label}] Tìm thấy - Score: {score:F2}");
                return true;
            }
            
            return false;
        }
        
        // Helper: Log vào file
        private void LogToFile(string message)
        {
            try
            {
                _logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            }
            catch
            {
                // Ignore logging errors
            }
        }
        
        // Helper: Log vào c�?console và file
        private void LogBoth(string message)
        {
            LogBoth(message);
            LogToFile(message);
        }
    }
}

