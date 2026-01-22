using System.ComponentModel;
using CvPoint = OpenCvSharp.Point;

namespace auto_chinhdo.Models
{
    /// <summary>
    /// Trạng thái của thiết bị trong chế độ Auto/PK
    /// </summary>
    public enum AutoState
    {
        IDLE_OR_PRIMARY_TASK,
        ATTACKING_ENEMY,
        RETURNING_TO_BASE
    }

    /// <summary>
    /// Chế độ Auto cho từng thiết bị
    /// </summary>
    public enum AutoMode
    {
        Auto = 0,       // Auto thường (template matching)
        PK = 1,         // Chế độ PK người chơi
        Hybrid = 2      // Hybrid (PK + Boss + Theo sau)
    }

    /// <summary>
    /// Model đại diện cho một thiết bị Android/LDPlayer được kết nối qua ADB
    /// </summary>
    public class DeviceItem : INotifyPropertyChanged
    {
        public string Serial { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public string SizeText => (Width > 0 && Height > 0) ? $"{Width}x{Height}" : string.Empty;
        
        /// <summary>
        /// Raw device data từ ADB client
        /// </summary>
        public AdvancedSharpAdbClient.Models.DeviceData? Raw { get; set; } = null;

        private bool _isSelected;
        /// <summary>
        /// Đánh dấu thiết bị được chọn để chạy Auto
        /// </summary>
        public bool IsSelected 
        { 
            get => _isSelected; 
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); } 
        }

        private AutoMode _selectedAutoMode = AutoMode.Auto;
        /// <summary>
        /// Chế độ Auto riêng cho thiết bị này
        /// </summary>
        public AutoMode SelectedAutoMode 
        { 
            get => _selectedAutoMode; 
            set { _selectedAutoMode = value; OnPropertyChanged(nameof(SelectedAutoMode)); OnPropertyChanged(nameof(AutoModeText)); } 
        }

        /// <summary>
        /// Text hiển thị chế độ Auto (cho binding)
        /// </summary>
        public string AutoModeText => SelectedAutoMode switch
        {
            AutoMode.PK => "⚔️ PK",
            AutoMode.Hybrid => "🔥 Hybrid",
            _ => "📋 Auto"
        };

        private int _appearTimeoutMs = 15000;
        /// <summary>
        /// Thời gian timeout chờ template xuất hiện (ms)
        /// </summary>
        public int AppearTimeoutMs 
        { 
            get => _appearTimeoutMs; 
            set { _appearTimeoutMs = value; OnPropertyChanged(nameof(AppearTimeoutMs)); } 
        }

        private int _waitAfterAppearMs = 100;
        /// <summary>
        /// Thời gian chờ sau khi template xuất hiện trước khi tap (ms)
        /// </summary>
        public int WaitAfterAppearMs 
        { 
            get => _waitAfterAppearMs; 
            set { _waitAfterAppearMs = value; OnPropertyChanged(nameof(WaitAfterAppearMs)); } 
        }

        /// <summary>
        /// Trạng thái hiện tại của thiết bị trong chế độ PK
        /// </summary>
        public AutoState CurrentState { get; set; } = AutoState.IDLE_OR_PRIMARY_TASK;
        
        /// <summary>
        /// Vị trí tap căn cứ (base) cho chế độ PK
        /// </summary>
        public CvPoint BaseTapPosition { get; set; } = new CvPoint(0, 0);

        private int _attackCooldownMs = 800;
        /// <summary>
        /// Thời gian cooldown giữa các lần tấn công (ms)
        /// </summary>
        public int AttackCooldownMs 
        { 
            get => _attackCooldownMs; 
            set { _attackCooldownMs = value; OnPropertyChanged(nameof(AttackCooldownMs)); } 
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Backward compatibility với MainWindow.xaml.cs
        public void OnChanged(string propertyName) => OnPropertyChanged(propertyName);
    }
}
