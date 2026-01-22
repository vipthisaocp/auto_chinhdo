using OpenCvSharp;

namespace auto_chinhdo.Services
{
    /// <summary>
    /// Kết quả so khớp template
    /// </summary>
    public class MatchResult
    {
        public string TemplatePath { get; set; } = string.Empty;
        public Point Center { get; set; }
        public double Score { get; set; }
        public Point Location { get; set; }
    }

    /// <summary>
    /// Interface cho dịch vụ xử lý ảnh và so khớp template
    /// </summary>
    public interface IImageMatchingService
    {
        /// <summary>
        /// Tìm template khớp nhất trong danh sách
        /// </summary>
        MatchResult? MatchAny(string screenPath, string[] templates, double threshold);

        /// <summary>
        /// So khớp template với bộ lọc màu (cho PK mode)
        /// </summary>
        MatchResult? MatchWithColorFilter(string screenPath, string templatePath, double threshold);

        /// <summary>
        /// Vẽ marker lên ảnh tại vị trí chỉ định
        /// </summary>
        void DrawMarker(string imagePath, Point position);
    }
}
