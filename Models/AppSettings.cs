using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace auto_chinhdo.Models
{
    /// <summary>
    /// Cấu hình ứng dụng - được lưu trữ dưới dạng JSON
    /// </summary>
    public class AppSettings
    {
        // Singleton Instance
        public static AppSettings Instance { get; private set; } = Load();

        // === ADB Settings ===
        /// <summary>
        /// Đường dẫn đến adb.exe
        /// </summary>
        public string AdbPath { get; set; } = @"C:\LDPlayer\LDPlayer9\adb.exe";

        /// <summary>
        /// Cổng ADB mặc định
        /// </summary>
        public int AdbPort { get; set; } = 5037;

        /// <summary>
        /// Số kết nối ADB đồng thời tối đa
        /// </summary>
        public int MaxConcurrentAdbConnections { get; set; } = 4;

        /// <summary>
        /// Thời gian reset ADB server định kỳ (ms)
        /// </summary>
        public int AdbResetIntervalMs { get; set; } = 20 * 60 * 1000; // 20 phút

        // === Auto Settings ===
        /// <summary>
        /// Ngưỡng khớp template (0.0 - 1.0)
        /// </summary>
        public double Threshold { get; set; } = 0.95;

        /// <summary>
        /// Khoảng thời gian giữa các lần poll (ms)
        /// </summary>
        public int PollIntervalMs { get; set; } = 700;

        /// <summary>
        /// Thời gian cooldown sau mỗi lần tap (ms)
        /// </summary>
        public int CooldownMs { get; set; } = 800;

        // === PK Settings ===
        /// <summary>
        /// Tọa độ X của nút tấn công cố định
        /// </summary>
        public int PkAttackCenterX { get; set; } = 38;

        /// <summary>
        /// Tọa độ Y của nút tấn công cố định
        /// </summary>
        public int PkAttackCenterY { get; set; } = 135;

        /// <summary>
        /// Tọa độ X của vị trí căn cứ
        /// </summary>
        public int PkBaseCenterX { get; set; } = 420;

        /// <summary>
        /// Tọa độ Y của vị trí căn cứ
        /// </summary>
        public int PkBaseCenterY { get; set; } = 257;

        // === UI Settings ===
        /// <summary>
        /// Số dòng log tối đa hiển thị
        /// </summary>
        public int MaxLogLines { get; set; } = 500;

        /// <summary>
        /// Số dòng log giữ lại khi cắt
        /// </summary>
        public int LinesToKeep { get; set; } = 400;

        /// <summary>
        /// Bật chế độ Dark Mode
        /// </summary>
        public bool IsDarkMode { get; set; } = false;

        // === File Paths ===
        private static readonly string SettingsFileName = "appsettings.json";
        
        [JsonIgnore]
        public static string SettingsFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);

        [JsonIgnore]
        public static string AppDirectory => AppDomain.CurrentDomain.BaseDirectory;

        [JsonIgnore]
        public static string TemplatesDirectory => Path.Combine(AppDirectory, "templates");

        /// <summary>
        /// Tải settings từ file JSON
        /// </summary>
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tải settings: {ex.Message}");
            }
            
            return new AppSettings();
        }

        /// <summary>
        /// Lưu settings vào file JSON
        /// </summary>
        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi lưu settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy đường dẫn thư mục template cho một thiết bị
        /// </summary>
        public static string GetDeviceTemplateDir(string deviceName)
        {
            var safeName = MakeSafeFileName(deviceName);
            var dir = Path.Combine(TemplatesDirectory, safeName);
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Lấy đường dẫn file screenshot cho một thiết bị
        /// </summary>
        public static string GetScreenPath(string serial)
        {
            return Path.Combine(AppDirectory, $"screen_{serial.Replace(':', '_')}.png");
        }

        /// <summary>
        /// Chuyển tên thành tên file an toàn
        /// </summary>
        private static string MakeSafeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var ch in invalid) 
                name = name.Replace(ch, '_');
            return name.Replace(":", "_").Replace("/", "_").Replace("\\", "_").Trim();
        }
    }
}
