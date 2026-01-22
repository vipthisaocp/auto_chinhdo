using System;
using System.Windows;
using auto_chinhdo.Services;

namespace auto_chinhdo.Views
{
    public partial class LoginWindow : Window
    {
        // Thời gian ghi nhớ đăng nhập: 3 ngày
        private const int REMEMBER_LOGIN_DAYS = 3;
        
        public bool IsLoginSuccessful { get; private set; }
        
        public LoginWindow()
        {
            InitializeComponent();
            LoadSavedCredentials();
        }
        
        private void LoadSavedCredentials()
        {
            try
            {
                // Kiểm tra xem có dữ liệu đăng nhập đã lưu không
                string savedEmail = Properties.Settings.Default.SavedEmail;
                string savedPassword = Properties.Settings.Default.SavedPassword;
                DateTime savedTime = Properties.Settings.Default.SavedLoginTime;
                
                // Kiểm tra thời gian hết hạn (3 ngày)
                bool isExpired = (DateTime.Now - savedTime).TotalDays > REMEMBER_LOGIN_DAYS;
                
                if (!string.IsNullOrEmpty(savedEmail) && !string.IsNullOrEmpty(savedPassword) && !isExpired)
                {
                    // Còn hiệu lực → điền sẵn thông tin
                    TxtEmail.Text = savedEmail;
                    TxtPassword.Password = savedPassword;
                    ChkRemember.IsChecked = true;
                }
                else if (!string.IsNullOrEmpty(savedEmail))
                {
                    // Chỉ còn email (mật khẩu đã hết hạn hoặc chưa lưu)
                    TxtEmail.Text = savedEmail;
                    ChkRemember.IsChecked = true;
                    
                    // Xóa mật khẩu đã hết hạn
                    if (isExpired)
                    {
                        Properties.Settings.Default.SavedPassword = "";
                        Properties.Settings.Default.Save();
                    }
                }
            }
            catch
            {
                // Bỏ qua lỗi đọc settings
            }
        }
        
        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = TxtEmail.Text.Trim();
            string password = TxtPassword.Password;
            
            // Validate
            if (string.IsNullOrEmpty(email))
            {
                ShowError("Vui lòng nhập email");
                return;
            }
            
            if (string.IsNullOrEmpty(password))
            {
                ShowError("Vui lòng nhập mật khẩu");
                return;
            }
            
            // Show loading
            BtnLogin.IsEnabled = false;
            TxtLoading.Visibility = Visibility.Visible;
            HideError();
            
            try
            {
                var (success, message) = await FirebaseService.Instance.LoginAsync(email, password);
                
                if (success)
                {
                    // Lưu thông tin đăng nhập nếu tick "Ghi nhớ"
                    if (ChkRemember.IsChecked == true)
                    {
                        Properties.Settings.Default.SavedEmail = email;
                        Properties.Settings.Default.SavedPassword = password;
                        Properties.Settings.Default.SavedLoginTime = DateTime.Now;
                        Properties.Settings.Default.RememberLogin = true;
                        Properties.Settings.Default.Save();
                    }
                    else
                    {
                        // Xóa thông tin đăng nhập đã lưu
                        Properties.Settings.Default.SavedEmail = "";
                        Properties.Settings.Default.SavedPassword = "";
                        Properties.Settings.Default.RememberLogin = false;
                        Properties.Settings.Default.Save();
                    }
                    
                    IsLoginSuccessful = true;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ShowError(message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi: {ex.Message}");
            }
            finally
            {
                BtnLogin.IsEnabled = true;
                TxtLoading.Visibility = Visibility.Collapsed;
            }
        }
        
        private void ShowError(string message)
        {
            TxtError.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }
        
        private void HideError()
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
        }
    }
}
