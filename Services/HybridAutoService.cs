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
    /// Service Hybrid kết hợp PK và đánh Boss
    /// Logic ưu tiên: PK người chơi > Đánh Boss > Theo sau PT
    /// </summary>
    public class HybridAutoService
    {
        private readonly Action<string> _log;
        private readonly Func<DeviceItem, Task> _captureScreen;
        private readonly Func<string> _getScreenPath;
        private readonly Action<DeviceData, int, int> _performTap;
        private readonly Func<double> _getThreshold;
        private readonly string _templateDir;

        // === TEMPLATES PK ===
        private const string LANCAN_TEMPLATE = "lancan.png";
        private const string NGUOICHOI_GOCTRAI_TEMPLATE = "Nguoichoigoctrai.png";
        private const string THEOSAU_TEMPLATE = "theosau.png";
        private const string BOTHEOSAU_TEMPLATE = "botheosau.png";
        private const string KIEMTRE_TEMPLATE = "kiemtre.png";

        // === TEMPLATES THANH MÁU NGƯỜI CHƠI (nhiều mức HP) ===
        // Hỗ trợ nhiều template để match thanh máu ở các mức HP khác nhau
        private static readonly string[] THANHMAU_TEMPLATES = new[]
        {
            "thanhmau.png",     // Thanh máu mặc định / đầy
            "thanhmau2.png",    // Thanh máu ~70%
            "thanhmau3.png",    // Thanh máu ~50%
            "thanhmau4.png",    // Thanh máu ~30%
            "thanhmau5.png"     // Thanh máu thấp
        };

        // === TEMPLATES BOSS ===
        private const string QUAIVAT_TEMPLATE = "quaivat.png";
        private const string THANHMAU_BOSS_TEMPLATE = "thanhmauboss.png";

        // === TEMPLATES HỒI SINH ===
        private const string HOISINHTAICHO_TEMPLATE = "hoisinhtaicho.png";  // Nút hồi sinh tại chỗ
        private const string HOISINHVETHANH_TEMPLATE = "hoisinhvethanh.png"; // Nút hồi sinh về thành

        // === TEMPLATES COMBAT DETECTION ===
        private const string DANGCHIENDAU_TEMPLATE = "dangchiendau.png";    // Icon đang trong trạng thái chiến đấu
        private const string THANHMAUMINH_TEMPLATE = "thanhmauminh.png";    // Thanh máu của mình (để biết đang bị đánh)

        // === SKILL TEMPLATES ===
        private static readonly string[] SKILL_TEMPLATES = new[]
        {
            "skill1.png", "skill2.png", "skill3.png",
            "skill4.png", "skill5.png", "skill6.png"
        };

        // === COMBAT TIMEOUTS ===
        // Timeout: 15 giây không thấy mục tiêu VÀ không trong combat → theo sau
        private const int NO_TARGET_TIMEOUT_MS = 15000;
        
        // Cooldown sau khi tap skill (để không bị timeout sớm khi đang combat)
        private const int SKILL_COMBAT_COOLDOWN_MS = 5000;
        
        // Thời gian tối thiểu giữ combat state (không timeout sớm)
        private const int COMBAT_MIN_DURATION_MS = 15000;
        
        // *** Thời gian tap skill liên tục sau khi lock mục tiêu (60s = 1 phút) ***
        private const int COMBAT_SKILL_LOOP_MS = 60000;
        
        // Số lần hồi sinh tại chỗ tối đa
        private const int MAX_RESPAWN_AT_SPOT = 3;
        
        // Thời gian đợi sau khi hồi sinh (ms)
        private const int RESPAWN_WAIT_MS = 3000;

        public HybridAutoService(
            string templateDir,
            Action<string> log,
            Func<DeviceItem, Task> captureScreen,
            Func<string> getScreenPath,
            Action<DeviceData, int, int> performTap,
            Func<double> getThreshold)
        {
            _templateDir = templateDir;
            _log = log;
            _captureScreen = captureScreen;
            _getScreenPath = getScreenPath;
            _performTap = performTap;
            _getThreshold = getThreshold;

            if (!Directory.Exists(_templateDir))
            {
                Directory.CreateDirectory(_templateDir);
                _log($"📁 Tạo thư mục Hybrid: {_templateDir}");
            }
        }

        public async Task RunHybridLoopAsync(DeviceItem device, CancellationToken ct)
        {
            if (device.Raw is not DeviceData rawDevice)
            {
                _log("❌ Device không hợp lệ");
                return;
            }

            _log("🔥 Bắt đầu chế độ Hybrid (PK + Boss)...");

            DateTime lastSeenTarget = DateTime.Now;
            DateTime lastSkillTap = DateTime.MinValue; // Track thời gian tap skill cuối
            DateTime combatStartTime = DateTime.MinValue; // Track khi nào bắt đầu combat
            bool isInCombat = false; // Trạng thái đang trong combat
            int respawnAtSpotCount = 0; // Đếm số lần hồi sinh tại chỗ

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

                    // === ƯU TIÊN CAO NHẤT: KIỂM TRA CHẾT (qua nút hồi sinh) ===
                    // Kiểm tra xem có thấy nút hồi sinh tại chỗ hoặc về thành không
                    bool foundRespawnButton = await TryMatchOnly(screenPath, HOISINHTAICHO_TEMPLATE, threshold)
                                           || await TryMatchOnly(screenPath, HOISINHVETHANH_TEMPLATE, threshold);
                    
                    if (foundRespawnButton)
                    {
                        _log("💀 Phát hiện màn hình hồi sinh!");
                        
                        if (respawnAtSpotCount < MAX_RESPAWN_AT_SPOT)
                        {
                            // Hồi sinh tại chỗ (còn dưới 3 lần)
                            bool tapped = await TryMatchAndTap(screenPath, HOISINHTAICHO_TEMPLATE, rawDevice, threshold, $"Hồi sinh tại chỗ ({respawnAtSpotCount + 1}/{MAX_RESPAWN_AT_SPOT})");
                            if (tapped)
                            {
                                respawnAtSpotCount++;
                                _log($"✨ Hồi sinh tại chỗ lần {respawnAtSpotCount}. Đợi {RESPAWN_WAIT_MS / 1000}s...");
                            }
                        }
                        else
                        {
                            // Lần thứ 4 trở đi: hồi sinh về thành
                            bool tapped = await TryMatchAndTap(screenPath, HOISINHVETHANH_TEMPLATE, rawDevice, threshold, "Hồi sinh về thành");
                            if (tapped)
                            {
                                _log($"🏠 Hồi sinh về thành (đã hết {MAX_RESPAWN_AT_SPOT} lần tại chỗ). Đợi {RESPAWN_WAIT_MS / 1000}s...");
                                respawnAtSpotCount = 0; // Reset counter sau khi về thành
                            }
                        }
                        
                        // Đợi sau khi hồi sinh
                        await Task.Delay(RESPAWN_WAIT_MS, ct);
                        lastSeenTarget = DateTime.Now;
                        continue;
                    }

                    // === ƯU TIÊN 1: PK NGƯỜI CHƠI (hỗ trợ màu sắc HSV) ===
                    // [CẬP NHẬT]: Dùng màu sắc thay vì Template để bám mục tiêu khi HP tụt
                    bool foundPlayerHealth = OpenCvLogic.IsTargetHealthBarVisible(screenPath);
                    if (foundPlayerHealth)
                    {
                        lastSeenTarget = DateTime.Now;
                        
                        // Bắt đầu combat state nếu chưa
                        if (!isInCombat)
                        {
                            isInCombat = true;
                            combatStartTime = DateTime.Now;
                            _log("⚔️ BẮT ĐẦU COMBAT - PK người chơi!");
                        }
                        
                        // Tap thanh máu để lock mục tiêu
                        await TryMatchAndTapAny(screenPath, THANHMAU_TEMPLATES, rawDevice, threshold, "Thanh máu người chơi");
                        
                        // *** VÒNG LẶP TAP SKILL 15s ***
                        // Sau khi lock mục tiêu, tap skill liên tục trong 15s
                        // Game sẽ tự PK theo, chỉ cần spam skill
                        _log($"⚔️ Bắt đầu vòng lặp skill 15 giây...");
                        var skillLoopEnd = DateTime.Now.AddMilliseconds(COMBAT_SKILL_LOOP_MS);
                        
                        while (DateTime.Now < skillLoopEnd && !ct.IsCancellationRequested)
                        {
                            // Kiểm tra hồi sinh (ưu tiên cao nhất)
                            await _captureScreen(device);
                            screenPath = _getScreenPath();
                            
                            bool needRespawn = await TryMatchOnly(screenPath, HOISINHTAICHO_TEMPLATE, threshold)
                                            || await TryMatchOnly(screenPath, HOISINHVETHANH_TEMPLATE, threshold);
                            if (needRespawn)
                            {
                                _log("💀 Phát hiện chết trong combat loop, thoát để hồi sinh...");
                                break; // Thoát khỏi skill loop để xử lý hồi sinh
                            }
                            
                            // Tap 6 skill liên tục
                            await TapAllSkills(screenPath, rawDevice, threshold);
                            lastSkillTap = DateTime.Now;
                            
                            await Task.Delay(500, ct); // Đợi 500ms giữa mỗi combo
                        }
                        
                        _log("⚔️ Hết 15s, kiểm tra lại thanh máu...");
                        continue;
                    }

                    // === ƯU TIÊN 2: ĐÁNH BOSS ===
                    bool foundBossHealth = await TryMatchOnly(screenPath, THANHMAU_BOSS_TEMPLATE, threshold);
                    if (foundBossHealth)
                    {
                        lastSeenTarget = DateTime.Now;
                        _log("👹 Thấy thanh máu Boss → Đánh Boss!");
                        
                        // Tap thanh máu boss
                        await TryMatchAndTap(screenPath, THANHMAU_BOSS_TEMPLATE, rawDevice, threshold, "Thanh máu Boss");
                        
                        // Tap 6 skill
                        await TapAllSkills(screenPath, rawDevice, threshold);
                        lastSkillTap = DateTime.Now; // Track thời gian tap skill
                        
                        await Task.Delay(300, ct);
                        continue;
                    }

                    // === KIỂM TRA TIMEOUT - THEO SAU ===
                    var noTargetDuration = DateTime.Now - lastSeenTarget;
                    var sinceLastSkill = DateTime.Now - lastSkillTap;
                    var combatDuration = DateTime.Now - combatStartTime;
                    
                    // Điều kiện timeout:
                    // 1. Quá 8s không thấy mục tiêu (thanh máu người chơi/boss)
                    // 2. VÀ đã hơn 5s từ lần tap skill cuối
                    // 3. VÀ (không trong combat HOẶC combat đã kéo dài hơn 8s)
                    bool combatExpired = !isInCombat || combatDuration.TotalMilliseconds >= COMBAT_MIN_DURATION_MS;
                    bool shouldTimeout = noTargetDuration.TotalMilliseconds >= NO_TARGET_TIMEOUT_MS
                                      && sinceLastSkill.TotalMilliseconds >= SKILL_COMBAT_COOLDOWN_MS
                                      && combatExpired;
                    
                    if (shouldTimeout)
                    {
                        // Reset combat state khi timeout
                        isInCombat = false;
                        _log("⏰ KẾT THÚC COMBAT - Timeout");
                        // Kiểm tra có đang theo sau không
                        bool isFollowing = await TryMatchOnly(screenPath, BOTHEOSAU_TEMPLATE, threshold);
                        
                        if (isFollowing)
                        {
                            _log("🚶 Đang theo sau PT...");
                            // Vẫn kiểm tra kiemtre
                            await TryMatchAndTap(screenPath, KIEMTRE_TEMPLATE, rawDevice, threshold, "Kiếm tre");
                        }
                        else
                        {
                            _log("⏰ 3s không thấy mục tiêu → Theo sau PT");
                            await TryMatchAndTap(screenPath, THEOSAU_TEMPLATE, rawDevice, threshold, "Theo sau");
                            
                            // Đợi và tap kiemtre
                            await Task.Delay(800, ct);
                            await _captureScreen(device);
                            var newScreen = _getScreenPath();
                            await TryMatchAndTap(newScreen, KIEMTRE_TEMPLATE, rawDevice, threshold, "Kiếm tre");
                        }
                        
                        lastSeenTarget = DateTime.Now;
                        await Task.Delay(500, ct);
                        continue;
                    }

                    // === TÌM MỤC TIÊU MỚI ===
                    // Thử tìm lan can
                    bool foundLanCan = await TryMatchAndTap(screenPath, LANCAN_TEMPLATE, rawDevice, threshold, "Lan cản");
                    if (foundLanCan)
                    {
                        await Task.Delay(500, ct);
                        
                        // Chụp lại và tìm người chơi/quái vật
                        await _captureScreen(device);
                        screenPath = _getScreenPath();
                        
                        // Ưu tiên tìm người chơi trước
                        bool foundPlayer = await TryMatchAndTap(screenPath, NGUOICHOI_GOCTRAI_TEMPLATE, rawDevice, threshold, "Người chơi");
                        if (!foundPlayer)
                        {
                            // Không thấy người chơi → tìm quái vật
                            await TryMatchAndTap(screenPath, QUAIVAT_TEMPLATE, rawDevice, threshold, "Quái vật");
                        }
                        
                        await Task.Delay(500, ct);
                        continue;
                    }

                    // Thử tap trực tiếp người chơi hoặc quái vật
                    bool foundNguoiChoi = await TryMatchAndTap(screenPath, NGUOICHOI_GOCTRAI_TEMPLATE, rawDevice, threshold, "Người chơi");
                    if (!foundNguoiChoi)
                    {
                        await TryMatchAndTap(screenPath, QUAIVAT_TEMPLATE, rawDevice, threshold, "Quái vật");
                    }

                    await Task.Delay(300, ct);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _log($"❌ Hybrid Error: {ex.Message}");
                    await Task.Delay(1000, ct);
                }
            }

            _log("🛑 Đã dừng chế độ Hybrid.");
        }

        private async Task TapAllSkills(string screenPath, DeviceData device, double threshold)
        {
            foreach (var skill in SKILL_TEMPLATES)
            {
                await TryMatchAndTap(screenPath, skill, device, threshold, skill.Replace(".png", ""));
                await Task.Delay(100);
            }
        }

        private async Task<bool> TryMatchAndTap(string screenPath, string templateName, DeviceData device, double threshold, string stepName)
        {
            string templatePath = Path.Combine(_templateDir, templateName);

            if (!File.Exists(templatePath))
            {
                return false;
            }

            var match = await Task.Run(() => 
                OpenCvLogic.MatchAny(screenPath, new[] { templatePath }, threshold));

            if (match != null)
            {
                int x = (int)match.Value.center.X;
                int y = (int)match.Value.center.Y;
                _performTap(device, x, y);
                _log($"✅ [{stepName}] Tap ({x}, {y})");
                return true;
            }

            return false;
        }

        private async Task<bool> TryMatchOnly(string screenPath, string templateName, double threshold)
        {
            string templatePath = Path.Combine(_templateDir, templateName);

            if (!File.Exists(templatePath))
            {
                return false;
            }

            var match = await Task.Run(() => 
                OpenCvLogic.MatchAny(screenPath, new[] { templatePath }, threshold));

            return match != null;
        }

        /// <summary>
        /// Match bất kỳ template nào trong array. Trả về true nếu tìm thấy ít nhất 1 template.
        /// </summary>
        private async Task<bool> TryMatchAnyTemplates(string screenPath, string[] templateNames, double threshold)
        {
            foreach (var templateName in templateNames)
            {
                string templatePath = Path.Combine(_templateDir, templateName);
                if (!File.Exists(templatePath))
                {
                    continue; // Bỏ qua nếu template không tồn tại
                }

                var match = await Task.Run(() => 
                    OpenCvLogic.MatchAny(screenPath, new[] { templatePath }, threshold));

                if (match != null)
                {
                    return true; // Tìm thấy 1 template → return ngay
                }
            }
            return false;
        }

        /// <summary>
        /// Match và tap bất kỳ template nào trong array. Trả về true nếu tap thành công.
        /// </summary>
        private async Task<bool> TryMatchAndTapAny(string screenPath, string[] templateNames, DeviceData device, double threshold, string stepName)
        {
            foreach (var templateName in templateNames)
            {
                string templatePath = Path.Combine(_templateDir, templateName);
                if (!File.Exists(templatePath))
                {
                    continue;
                }

                var match = await Task.Run(() => 
                    OpenCvLogic.MatchAny(screenPath, new[] { templatePath }, threshold));

                if (match != null)
                {
                    int x = (int)match.Value.center.X;
                    int y = (int)match.Value.center.Y;
                    _performTap(device, x, y);
                    _log($"✅ [{stepName}] Tap ({x}, {y}) - {templateName}");
                    return true;
                }
            }
            return false;
        }
    }
}
