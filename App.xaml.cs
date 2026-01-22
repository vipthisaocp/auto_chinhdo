using System.Configuration;
using System.Data;
using System.Windows;
using auto_chinhdo.Views;
using auto_chinhdo.Services;

namespace auto_chinhdo
{
    public partial class App : Application
    {
        public const string CurrentVersion = "1.0.2";
        
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Đặt ShutdownMode để app không tự tắt khi đóng LoginWindow
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            try
            {
                // Khởi tạo Firebase
                await FirebaseService.Instance.InitializeAsync();
                
                // KIỂM TRA CẬP NHẬT
                var updateConfig = await FirebaseService.Instance.CheckForUpdateAsync();
                if (updateConfig != null && updateConfig.HasUpdate(CurrentVersion))
                {
                    string msg = $"Đã có phiên bản mới: {updateConfig.LatestVersion}\n\n{updateConfig.UpdateNotes}\n\nBạn có muốn cập nhật ngay bây giờ không?";
                    var resultUpdate = MessageBox.Show(msg, "Phát hiện bản cập nhật mới", 
                        MessageBoxButton.YesNo, MessageBoxImage.Information);
                        
                    if (resultUpdate == MessageBoxResult.Yes)
                    {
                        // Hiển thị UI chờ hoặc đóng app để chạy updater
                        await UpdateService.Instance.ProcessUpdateAsync(updateConfig);
                        return; // Kết thúc app sau khi gọi Updater
                    }
                    else if (updateConfig.IsCritical)
                    {
                        MessageBox.Show("Đây là bản cập nhật bắt buộc. Ứng dụng sẽ thoát.", "Yêu cầu cập nhật", MessageBoxButton.OK, MessageBoxImage.Warning);
                        Shutdown();
                        return;
                    }
                }
                
                // Hiển thị LoginWindow
                var loginWindow = new LoginWindow();
                bool? result = loginWindow.ShowDialog();
                
                if (result == true && loginWindow.IsLoginSuccessful)
                {
                    // Đăng nhập thành công → mở MainWindow
                    ShutdownMode = ShutdownMode.OnMainWindowClose;
                    
                    var mainWindow = new MainWindow();
                    MainWindow = mainWindow;
                    mainWindow.Show();
                }
                else
                {
                    // Đăng nhập thất bại → thoát ứng dụng
                    Shutdown();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khởi động: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
