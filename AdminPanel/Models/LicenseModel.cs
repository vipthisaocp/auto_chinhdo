using System;
using Google.Cloud.Firestore;

namespace AdminPanel.Models
{
    /// <summary>
    /// Model cho License trong Firestore
    /// </summary>
    public class LicenseModel
    {
        public string Id { get; set; } = "";
        public string UserId { get; set; } = "";
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(30);
        public bool IsActive { get; set; } = true;
        public int MaxDevices { get; set; } = 1;
        
        // Computed properties
        public bool IsExpired => EndDate < DateTime.Now;
        public int RemainingDays => Math.Max(0, (EndDate - DateTime.Now).Days);
        public string Status => IsActive ? (IsExpired ? "Hết hạn" : "Đang hoạt động") : "Đã vô hiệu";
    }
}
