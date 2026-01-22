using System;
using System.IO;
using System.Text.Json;
using auto_chinhdo.Models;

namespace auto_chinhdo.Services
{
    /// <summary>
    /// Service quản lý cấu hình thanh máu - Load/Save từ file JSON.
    /// </summary>
    public class HealthBarConfigService
    {
        private const string CONFIG_FILENAME = "hp_bar_config.json";
        private readonly string _configPath;
        private HealthBarConfig? _cachedConfig;

        public HealthBarConfigService(string baseDir)
        {
            _configPath = baseDir; // Bây giờ là thư mục base
        }

        private string GetFilePath(string targetType)
        {
            string fileName = targetType.ToLower() == "boss" ? "boss_hp_bar_config.json" : "hp_bar_config.json";
            return Path.Combine(_configPath, fileName);
        }

        /// <summary>
        /// Load cấu hình từ file. Nếu file không tồn tại, trả về cấu hình mặc định.
        /// </summary>
        public HealthBarConfig LoadConfig(string targetType = "player")
        {
            string filePath = GetFilePath(targetType);
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var config = JsonSerializer.Deserialize<HealthBarConfig>(json);
                    if (config != null && config.IsValid)
                    {
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi load HP config ({targetType}): {ex.Message}");
            }

            // Trả về cấu hình mặc định nếu không load được
            return HealthBarConfig.CreateDefault();
        }

        /// <summary>
        /// Lưu cấu hình vào file JSON.
        /// </summary>
        public bool SaveConfig(HealthBarConfig config, string targetType = "player")
        {
            string filePath = GetFilePath(targetType);
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi save HP config ({targetType}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lấy cấu hình đã cache (load từ lần gọi LoadConfig trước đó).
        /// </summary>
        public HealthBarConfig GetCachedConfig()
        {
            return _cachedConfig ?? LoadConfig();
        }

        /// <summary>
        /// Lấy mẫu màu từ ảnh tại vị trí 10% chiều dài thanh máu.
        /// </summary>
        public static (int R, int G, int B) SampleColorFromImage(string imagePath, int x, int y, int width, int height)
        {
            try
            {
                using var img = OpenCvSharp.Cv2.ImRead(imagePath, OpenCvSharp.ImreadModes.Color);
                if (img.Empty()) return (200, 30, 30); // Mặc định nếu lỗi

                // Lấy mẫu tại 10% chiều dài thanh máu
                int sampleX = x + (int)(width * 0.1);
                int sampleY = y + height / 2;

                // Kiểm tra giới hạn
                if (sampleX >= img.Cols || sampleY >= img.Rows)
                    return (200, 30, 30);

                // Lấy màu BGR
                var pixel = img.At<OpenCvSharp.Vec3b>(sampleY, sampleX);
                int b = pixel[0];
                int g = pixel[1];
                int r = pixel[2];

                return (r, g, b);
            }
            catch
            {
                return (200, 30, 30); // Mặc định nếu lỗi
            }
        }
    }
}
