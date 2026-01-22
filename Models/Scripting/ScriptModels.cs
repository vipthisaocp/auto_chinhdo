using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace auto_chinhdo.Models.Scripting
{
    /// <summary>
    /// Định nghĩa các hành động có thể thực hiện trong kịch bản
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ScriptActionType
    {
        Tap,            // Click vào vị trí tìm thấy ảnh
        TapText,        // Click vào vị trí tìm thấy text (OCR) - MỚI
        DoubleTap,      // Double click
        Swipe,          // Vuốt màn hình
        Wait,           // Chờ đợi 
        Type,           // Nhập văn bản (ADB text)
        Exist,          // Chỉ kiểm tra sự tồn tại (không làm gì)
        Log,            // Ghi log
        Stop            // Dừng kịch bản
    }

    /// <summary>
    /// Hành vi khi bước thất bại (không tìm thấy ảnh)
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OnFailBehavior
    {
        Stop,               // Dừng kịch bản và log lỗi
        RetryFromStart,     // Quay về bước đầu tiên và thử lại
        RetryCurrentStep,   // Thử lại bước hiện tại (theo RetryCount)
        SkipToNext,         // Bỏ qua và chuyển sang bước tiếp theo
        GotoStep            // Nhảy đến một bước cụ thể (dùng OnFailStepId)
    }

    /// <summary>
    /// Một bước trong kịch bản auto
    /// </summary>
    public class ScriptStep : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        // --- ĐIỀU KIỆN TÌM KIẾM ---
        private string _description = string.Empty;
        public string Description 
        { 
            get => _description; 
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        private string _templateName = string.Empty;
        public string TemplateName 
        { 
            get => _templateName; 
            set { _templateName = value; OnPropertyChanged(nameof(TemplateName)); }
        }

        public double Threshold { get; set; } = 0.9;
        public int TimeoutMs { get; set; } = 5000;

        // --- HÀNH ĐỘNG ---
        public ScriptActionType Action { get; set; } = ScriptActionType.Tap;
        public int OffsetX { get; set; } = 0;
        public int OffsetY { get; set; } = 0;
        public int DelayAfterMs { get; set; } = 1000;

        // --- OCR (cho action TapText) ---
        public string TextToFind { get; set; } = string.Empty; // Text cần tìm bằng OCR
        public bool ExactMatch { get; set; } = false; // true = match chính xác, false = contains

        // --- XỬ LÝ LỖI ---
        public OnFailBehavior OnFail { get; set; } = OnFailBehavior.Stop;
        public string? OnFailStepId { get; set; } // Dùng khi OnFail = GotoStep
        public int RetryCount { get; set; } = 3;  // Số lần thử lại (khi OnFail = RetryCurrentStep)
        public int RetryDelayMs { get; set; } = 1000; // Delay giữa các lần retry

        // --- ĐIỀU HƯỚNG THÀNH CÔNG ---
        public string? NextStepId { get; set; } // null = bước tiếp theo trong list

        // --- UI BINDING ---
        [JsonIgnore]
        public int StepNumber { get; set; } // Số thứ tự hiển thị trong UI

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Toàn bộ kịch bản
    /// </summary>
    public class ScriptProfile
    {
        public string Name { get; set; } = "New Script";
        public string Author { get; set; } = "User";
        public string Description { get; set; } = string.Empty; // Fixed typo
        public int Version { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;

        public List<ScriptStep> Steps { get; set; } = new List<ScriptStep>();
    }
}
