using System;
using Google.Cloud.Firestore;

namespace AdminPanel.Models
{
    /// <summary>
    /// Model cho User trong Firestore
    /// </summary>
    public class UserModel
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
