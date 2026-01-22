using System.Threading.Tasks;
using auto_chinhdo.Models;
using AdvancedSharpAdbClient.Models;

namespace auto_chinhdo.Services
{
    /// <summary>
    /// Interface cho các thao tác ADB với thiết bị Android
    /// </summary>
    public interface IAdbService
    {
        /// <summary>
        /// Khởi động ADB server nếu chưa chạy
        /// </summary>
        void EnsureServerStarted();

        /// <summary>
        /// Kiểm tra và reset ADB server nếu cần
        /// </summary>
        Task EnsureServerIsHealthy(bool forceRestart = false);

        /// <summary>
        /// Lấy danh sách thiết bị đang kết nối
        /// </summary>
        Task<System.Collections.Generic.List<DeviceData>> GetDevicesAsync();

        /// <summary>
        /// Chụp màn hình thiết bị và lưu vào file
        /// </summary>
        Task CaptureScreenAsync(DeviceItem device, string outputPath);

        /// <summary>
        /// Thực hiện tap tại tọa độ chỉ định
        /// </summary>
        bool PerformTap(DeviceData device, int x, int y);

        /// <summary>
        /// Lấy kích thước màn hình thiết bị
        /// </summary>
        (int Width, int Height) GetScreenSize(DeviceData device);

        /// <summary>
        /// Thực thi lệnh ADB tùy chỉnh
        /// </summary>
        string ExecuteCommand(DeviceData device, string command);
    }
}
