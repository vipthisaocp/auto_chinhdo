using System;
using System.Windows;
using AdminPanel.Services;

namespace AdminPanel.Views
{
    public partial class AdminLoginWindow : Window
    {
        public bool IsAuthSuccessful { get; private set; }
        
        public AdminLoginWindow()
        {
            InitializeComponent();
        }
        
        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text.Trim();
            string password = TxtPassword.Password;
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Vui lòng nhập tài khoản và mật khẩu");
                return;
            }
            
            // UI State
            BtnLogin.IsEnabled = false;
            TxtLoading.Visibility = Visibility.Visible;
            TxtError.Visibility = Visibility.Collapsed;
            
            try
            {
                var (success, message) = await FirebaseAdminService.Instance.AdminLoginAsync(username, password);
                
                if (success)
                {
                    IsAuthSuccessful = true;
                    DialogResult = true;
                    this.Close();
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
            TxtError.Text = "❌ " + message;
            TxtError.Visibility = Visibility.Visible;
        }
    }
}
