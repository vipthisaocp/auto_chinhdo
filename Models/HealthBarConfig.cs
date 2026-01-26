using System;
using System.Text.Json.Serialization;

namespace auto_chinhdo.Models
{
    /// <summary>
    /// Cấu hình vùng nhận diện thanh máu (ROI) và màu chuẩn.
    /// </summary>
    public class HealthBarConfig
    {
        // Tọa độ vùng ROI (Region of Interest)
        public int X { get; set; }          // Tọa độ X góc trên bên trái
        public int Y { get; set; }          // Tọa độ Y góc trên bên trái
        public int Width { get; set; }      // Chiều rộng thanh máu
        public int Height { get; set; }     // Chiều cao thanh máu

        // Màu chuẩn (RGB) - Lấy mẫu tại 10% chiều dài thanh máu
        public int SampleR { get; set; }    // Giá trị Red chuẩn
        public int SampleG { get; set; }    // Giá trị Green chuẩn
        public int SampleB { get; set; }    // Giá trị Blue chuẩn

        // Ngưỡng sai số (Tolerance) cho phép
        public int ToleranceR { get; set; } = 50;  // Sai số Red ±50
        public int ToleranceG { get; set; } = 30;  // Sai số Green ±30
        public int ToleranceB { get; set; } = 30;  // Sai số Blue ±30

        // Tọa độ bấm vào mục tiêu khi phát hiện địch
        public int TapX { get; set; } = 24;
        public int TapY { get; set; } = 137;

        // Timeout chuyển sang theo sau (ms)
        public int NoEnemyTimeoutMs { get; set; } = 3000;

        // --- ROI cho Tab Navigation (v5.7) ---
        public int NavROI_NhiemVu_X { get; set; } = 0;
        public int NavROI_NhiemVu_Y { get; set; } = 130;
        public int NavROI_NhiemVu_W { get; set; } = 150;
        public int NavROI_NhiemVu_H { get; set; } = 80;

        public int NavROI_LanCan_X { get; set; } = 0;
        public int NavROI_LanCan_Y { get; set; } = 290;
        public int NavROI_LanCan_W { get; set; } = 150;
        public int NavROI_LanCan_H { get; set; } = 80;

        /// <summary>
        /// Kiểm tra xem cấu hình có hợp lệ không.
        /// </summary>
        [JsonIgnore]
        public bool IsValid => Width > 0 && Height > 0;

        /// <summary>
        /// Tạo cấu hình mặc định (dựa trên ảnh mẫu từ người dùng).
        /// </summary>
        public static HealthBarConfig CreateDefault()
        {
            return new HealthBarConfig
            {
                X = 20,
                Y = 25,
                Width = 120,
                Height = 12,
                SampleR = 200,
                SampleG = 30,
                SampleB = 30,
                ToleranceR = 50,
                ToleranceG = 30,
                ToleranceB = 30,
                TapX = 24,
                TapY = 137,
                NoEnemyTimeoutMs = 3000
            };
        }
    }
}
