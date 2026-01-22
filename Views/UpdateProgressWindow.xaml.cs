using System.Windows;

namespace auto_chinhdo.Views
{
    public partial class UpdateProgressWindow : Window
    {
        public UpdateProgressWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Cập nhật trạng thái hiển thị
        /// </summary>
        public void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                TxtStatus.Text = status;
            });
        }

        /// <summary>
        /// Cập nhật tiến trình tải xuống
        /// </summary>
        public void UpdateProgress(double percent)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = percent;
                TxtPercent.Text = $"{percent:F0}%";
            });
        }
    }
}
