using System.Windows;
using AdminPanel.Views;

namespace AdminPanel
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Cấu hình ShutdownMode để app không tắt khi LoginWindow đóng
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginWindow = new AdminLoginWindow();
            bool? result = loginWindow.ShowDialog();

            if (result == true && loginWindow.IsAuthSuccessful)
            {
                // Đăng nhập thành công -> Mở MainWindow
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                this.MainWindow = new MainWindow();
                this.MainWindow.Show();
            }
            else
            {
                // Đăng nhập thất bại hoặc tắt cửa sổ -> Tắt app
                this.Shutdown();
            }
        }
    }
}

