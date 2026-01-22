using System;
using System.Linq;
using System.Windows;
using AdminPanel.Models;
using AdminPanel.Services;

namespace AdminPanel
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }
        
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Khởi tạo Firebase
            bool initialized = await FirebaseAdminService.Instance.InitializeAsync();
            if (!initialized)
            {
                MessageBox.Show("Không thể kết nối Firebase!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            // Load dữ liệu
            await LoadDashboardData();
        }
        
        private async System.Threading.Tasks.Task LoadDashboardData()
        {
            try
            {
                var users = await FirebaseAdminService.Instance.GetUsersAsync();
                var licenses = await FirebaseAdminService.Instance.GetLicensesAsync();
                
                // Update Dashboard
                TxtTotalUsers.Text = users.Count.ToString();
                TxtActiveLicenses.Text = licenses.Count(l => l.IsActive && !l.IsExpired).ToString();
                TxtExpiringSoon.Text = licenses.Count(l => l.IsActive && l.RemainingDays <= 7 && l.RemainingDays > 0).ToString();
                
                // Update Grids
                UsersGrid.ItemsSource = users;
                LicensesGrid.ItemsSource = licenses;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        #region Navigation
        
        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowView("dashboard");
        }
        
        private void BtnUsers_Click(object sender, RoutedEventArgs e)
        {
            ShowView("users");
        }
        
        private void BtnLicenses_Click(object sender, RoutedEventArgs e)
        {
            ShowView("licenses");
        }
        
        private async void BtnDevices_Click(object sender, RoutedEventArgs e)
        {
            ShowView("devices");
            await LoadDevicesData();
        }
        
        private void ShowView(string viewName)
        {
            DashboardView.Visibility = viewName == "dashboard" ? Visibility.Visible : Visibility.Collapsed;
            UsersView.Visibility = viewName == "users" ? Visibility.Visible : Visibility.Collapsed;
            LicensesView.Visibility = viewName == "licenses" ? Visibility.Visible : Visibility.Collapsed;
            DevicesView.Visibility = viewName == "devices" ? Visibility.Visible : Visibility.Collapsed;
            ReleaseView.Visibility = viewName == "release" ? Visibility.Visible : Visibility.Collapsed;
        }
        
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDashboardData();
            await LoadDevicesData();
            MessageBox.Show("Đã làm mới dữ liệu!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        #endregion
        
        #region Users CRUD
        
        private async void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            string email = TxtNewEmail.Text.Trim();
            string password = TxtNewPassword.Text.Trim();
            string displayName = TxtNewDisplayName.Text.Trim();
            
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Vui lòng nhập email và password!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var user = new UserModel
            {
                Email = email,
                Password = password,
                DisplayName = displayName
            };
            
            bool success = await FirebaseAdminService.Instance.AddUserAsync(user);
            if (success)
            {
                TxtNewEmail.Text = "";
                TxtNewPassword.Text = "";
                TxtNewDisplayName.Text = "";
                await LoadDashboardData();
                MessageBox.Show("Đã thêm user!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private async void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is UserModel user)
            {
                var result = MessageBox.Show($"Xóa user {user.Email}?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    await FirebaseAdminService.Instance.DeleteUserAsync(user.Id);
                    await LoadDashboardData();
                }
            }
        }
        
        #endregion
        
        #region Licenses CRUD
        
        private async void BtnAddLicense_Click(object sender, RoutedEventArgs e)
        {
            string email = TxtLicenseEmail.Text.Trim();
            if (!int.TryParse(TxtLicenseDays.Text, out int days))
            {
                days = 30;
            }
            
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Vui lòng nhập email!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var license = new LicenseModel
            {
                UserId = email,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(days),
                IsActive = true,
                MaxDevices = 1
            };
            
            bool success = await FirebaseAdminService.Instance.AddLicenseAsync(license);
            if (success)
            {
                TxtLicenseEmail.Text = "";
                TxtLicenseDays.Text = "30";
                await LoadDashboardData();
                MessageBox.Show($"Đã tạo license {days} ngày!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private async void BtnExtendLicense_Click(object sender, RoutedEventArgs e)
        {
            if (LicensesGrid.SelectedItem is LicenseModel license)
            {
                bool success = await FirebaseAdminService.Instance.ExtendLicenseAsync(license.Id, 30);
                if (success)
                {
                    await LoadDashboardData();
                    MessageBox.Show("Đã gia hạn thêm 30 ngày!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        
        private async void BtnDeactivateLicense_Click(object sender, RoutedEventArgs e)
        {
            if (LicensesGrid.SelectedItem is LicenseModel license)
            {
                var result = MessageBox.Show($"Vô hiệu license của {license.UserId}?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    await FirebaseAdminService.Instance.DeactivateLicenseAsync(license.Id);
                    await LoadDashboardData();
                }
            }
        }
        
        #endregion
        
        #region Devices Management
        
        private async System.Threading.Tasks.Task LoadDevicesData()
        {
            try
            {
                var devices = await FirebaseAdminService.Instance.GetAllActiveDevicesAsync();
                DevicesGrid.ItemsSource = devices;
                
                int onlineCount = devices.Count(d => d.IsOnline);
                TxtDeviceStats.Text = $"Tổng: {devices.Count} thiết bị ({onlineCount} online)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách thiết bị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void BtnRefreshDevices_Click(object sender, RoutedEventArgs e)
        {
            await LoadDevicesData();
        }
        
        private async void BtnCleanupDevices_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Xóa tất cả thiết bị không hoạt động trong 7 ngày qua?",
                "Xác nhận dọn dẹp",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                int cleaned = await FirebaseAdminService.Instance.CleanupInactiveDevicesAsync(7);
                await LoadDevicesData();
                MessageBox.Show($"Đã dọn dẹp {cleaned} thiết bị không hoạt động!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private async void BtnRemoveDevice_Click(object sender, RoutedEventArgs e)
        {
            if (DevicesGrid.SelectedItem is ActiveDeviceModel device)
            {
                var result = MessageBox.Show(
                    $"Xóa thiết bị '{device.DeviceName}' khỏi hệ thống?\n\nNgười dùng sẽ cần đăng nhập lại.",
                    "Xác nhận xóa thiết bị",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Cần tìm license ID thực sự từ device
                    // Tạm thời sử dụng logic tìm kiếm
                    var licenses = await FirebaseAdminService.Instance.GetLicensesAsync();
                    foreach (var license in licenses)
                    {
                        bool success = await FirebaseAdminService.Instance.ForceRemoveDeviceAsync(license.Id, device.Id);
                        if (success)
                        {
                            await LoadDevicesData();
                            MessageBox.Show("Đã xóa thiết bị!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                    }
                    
                    MessageBox.Show("Không tìm thấy thiết bị để xóa!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
        
        #endregion

        #region Update Release

        private async void BtnRelease_Click(object sender, RoutedEventArgs e)
        {
            ShowView("release");
            await LoadUpdateConfig();
        }

        private async System.Threading.Tasks.Task LoadUpdateConfig()
        {
            try
            {
                var config = await FirebaseAdminService.Instance.GetUpdateConfigAsync();
                if (config != null)
                {
                    TxtNewVersion.Text = config.ContainsKey("latestVersion") ? config["latestVersion"].ToString() : "";
                    TxtUpdateUrl.Text = config.ContainsKey("updateUrl") ? config["updateUrl"].ToString() : "";
                    TxtUpdateNotes.Text = config.ContainsKey("updateNotes") ? config["updateNotes"].ToString() : "";
                    ChkIsCritical.IsChecked = config.ContainsKey("isCritical") && (bool)config["isCritical"];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải thông tin update: {ex.Message}");
            }
        }

        private async void BtnPublish_Click(object sender, RoutedEventArgs e)
        {
            string version = TxtNewVersion.Text.Trim();
            string url = TxtUpdateUrl.Text.Trim();
            string notes = TxtUpdateNotes.Text.Trim();
            bool isCritical = ChkIsCritical.IsChecked ?? false;

            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Phiên bản và Link tải!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Xác nhận phát hành phiên bản {version}?\n\nKhách hàng sẽ thấy thông báo cập nhật ngay lập tức.",
                "Xác nhận phát hành",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                bool success = await FirebaseAdminService.Instance.PublishUpdateAsync(version, url, notes, isCritical);
                if (success)
                {
                    MessageBox.Show("Đã phát hành bản cập nhật thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Lỗi khi phát hành bản cập nhật!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}
