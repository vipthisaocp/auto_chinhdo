using System;

namespace AdminPanel.Models
{
    /// <summary>
    /// Model cho thiết bị đang hoạt động trong hệ thống
    /// </summary>
    public class ActiveDeviceModel
    {
        /// <summary>
        /// Document ID trong Firestore
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Hardware ID duy nhất của máy tính
        /// </summary>
        public string Hwid { get; set; } = string.Empty;
        
        /// <summary>
        /// Tên máy tính
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;
        
        /// <summary>
        /// Thời điểm đăng nhập
        /// </summary>
        public DateTime LoginTime { get; set; }
        
        /// <summary>
        /// Lần cuối hoạt động
        /// </summary>
        public DateTime LastSeen { get; set; }
        
        /// <summary>
        /// License ID mà thiết bị này thuộc về
        /// </summary>
        public string LicenseId { get; set; } = string.Empty;
        
        /// <summary>
        /// Chuyển LastSeen từ UTC sang local time (Firestore luôn lưu UTC)
        /// </summary>
        private DateTime LocalLastSeen
        {
            get
            {
                // Firestore Timestamp.ToDateTime() trả về UTC với Kind=Utc
                // Nhưng nếu Kind=Unspecified, ta coi như là UTC và chuyển sang local
                if (LastSeen.Kind == DateTimeKind.Utc)
                    return LastSeen.ToLocalTime();
                else if (LastSeen.Kind == DateTimeKind.Unspecified)
                    return DateTime.SpecifyKind(LastSeen, DateTimeKind.Utc).ToLocalTime();
                else
                    return LastSeen; // Đã là local time
            }
        }
        
        /// <summary>
        /// Hiển thị thời gian không hoạt động
        /// </summary>
        public string InactiveTime
        {
            get
            {
                var diff = DateTime.Now - LocalLastSeen;
                
                // Cho phép sai lệch 5 phút (nếu clock máy admin chậm hơn máy client)
                if (diff.TotalSeconds < 300 && diff.TotalSeconds > -300)
                    return "Vừa xong";
                    
                if (diff.TotalSeconds < 0)
                    return "Vừa xong"; // Lệch múi giờ/clock 
                    
                if (diff.TotalSeconds < 60)
                    return "Vừa xong";
                if (diff.TotalMinutes < 60)
                    return $"{(int)diff.TotalMinutes} phút trước";
                if (diff.TotalHours < 24)
                    return $"{(int)diff.TotalHours} giờ trước";
                return $"{(int)diff.TotalDays} ngày trước";
            }
        }
        
        /// <summary>
        /// Trạng thái hoạt động (dựa trên lastSeen trong vòng 30 phút - vì app update mỗi 10 phút)
        /// Nới lỏng thành 1 tiếng để an toàn.
        /// </summary>
        public bool IsOnline
        {
            get
            {
                var diff = DateTime.Now - LocalLastSeen;
                // Online nếu hoạt động trong vòng 30 phút (vì app send heartbeat mỗi 10p)
                // Chấp nhận lệch clock +- 15 phút
                return Math.Abs(diff.TotalMinutes) < 30;
            }
        }
        
        /// <summary>
        /// Màu sắc trạng thái đại diện cho Online/Offline
        /// </summary>
        public string StatusBrush => IsOnline ? "#22c55e" : "#94a3b8"; // Green : Grey

        /// <summary>
        /// Hiển thị trạng thái (Dùng emoji kết hợp text)
        /// </summary>
        public string StatusText => IsOnline ? "Online" : "Offline";

        /// <summary>
        /// Emoji đại diện (Để text-only nếu cần)
        /// </summary>
        public string StatusEmoji => IsOnline ? "🟢" : "⚪";
    }
}
