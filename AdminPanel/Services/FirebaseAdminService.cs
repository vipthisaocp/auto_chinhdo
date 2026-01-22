using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using AdminPanel.Models;

namespace AdminPanel.Services
{
    /// <summary>
    /// Service quản lý Firebase cho Admin Panel
    /// </summary>
    public class FirebaseAdminService
    {
        private static FirebaseAdminService? _instance;
        private static readonly object _lock = new();
        
        private FirestoreDb? _db;
        public bool IsInitialized { get; private set; }
        
        public static FirebaseAdminService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new FirebaseAdminService();
                    }
                }
                return _instance;
            }
        }
        
        private FirebaseAdminService() { }
        
        /// <summary>
        /// Khởi tạo Firebase
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                string keyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firebase-admin-key.json");
                
                if (!File.Exists(keyPath))
                {
                    return false;
                }
                
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", keyPath);
                
                if (FirebaseApp.DefaultInstance == null)
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(keyPath)
                    });
                }
                
                var firestoreBuilder = new FirestoreDbBuilder
                {
                    ProjectId = "autoldplayer-license",
                    CredentialsPath = keyPath
                };
                _db = await firestoreBuilder.BuildAsync();
                
                IsInitialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        #region Admin Auth
        
        /// <summary>
        /// Đăng nhập quyền Admin
        /// </summary>
        public async Task<(bool success, string message)> AdminLoginAsync(string username, string password)
        {
            try
            {
                if (!IsInitialized)
                {
                    await InitializeAsync();
                }

                if (_db == null)
                {
                    return (false, "Lỗi kết nối Firebase (DB null)");
                }

                // Query tìm tài khoản admin
                var query = _db.Collection("admin_user").WhereEqualTo("tai_khoan", username);
                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Count == 0)
                {
                    return (false, "Tài khoản không tồn tại");
                }

                var doc = snapshot.Documents[0];
                string dbPassword = doc.ContainsField("password") ? doc.GetValue<string>("password") : "";

                if (dbPassword == password)
                {
                    return (true, "Đăng nhập thành công");
                }
                else
                {
                    return (false, "Mật khẩu không chính xác");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi hệ thống: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Users
        
        /// <summary>
        /// Lấy danh sách users
        /// </summary>
        public async Task<List<UserModel>> GetUsersAsync()
        {
            var users = new List<UserModel>();
            if (_db == null) return users;
            
            var snapshot = await _db.Collection("user").GetSnapshotAsync();
            foreach (var doc in snapshot.Documents)
            {
                users.Add(new UserModel
                {
                    Id = doc.Id,
                    Email = doc.ContainsField("email") ? doc.GetValue<string>("email") : "",
                    Password = doc.ContainsField("password") ? doc.GetValue<string>("password") : "",
                    DisplayName = doc.ContainsField("displayName") ? doc.GetValue<string>("displayName") : "",
                    CreatedAt = doc.ContainsField("createdAt") ? doc.GetValue<Timestamp>("createdAt").ToDateTime() : DateTime.Now
                });
            }
            return users;
        }
        
        /// <summary>
        /// Thêm user mới
        /// </summary>
        public async Task<bool> AddUserAsync(UserModel user)
        {
            if (_db == null) return false;
            
            var docRef = _db.Collection("user").Document();
            await docRef.SetAsync(new Dictionary<string, object>
            {
                { "email", user.Email },
                { "password", user.Password },
                { "displayName", user.DisplayName },
                { "createdAt", Timestamp.FromDateTime(DateTime.UtcNow) }
            });
            return true;
        }
        
        /// <summary>
        /// Xóa user
        /// </summary>
        public async Task<bool> DeleteUserAsync(string userId)
        {
            if (_db == null) return false;
            await _db.Collection("user").Document(userId).DeleteAsync();
            return true;
        }
        
        #endregion
        
        #region Licenses
        
        /// <summary>
        /// Lấy danh sách licenses
        /// </summary>
        public async Task<List<LicenseModel>> GetLicensesAsync()
        {
            var licenses = new List<LicenseModel>();
            if (_db == null) return licenses;
            
            var snapshot = await _db.Collection("licenses").GetSnapshotAsync();
            foreach (var doc in snapshot.Documents)
            {
                licenses.Add(new LicenseModel
                {
                    Id = doc.Id,
                    UserId = doc.ContainsField("userId") ? doc.GetValue<string>("userId") : "",
                    StartDate = doc.ContainsField("startDate") ? doc.GetValue<Timestamp>("startDate").ToDateTime() : DateTime.Now,
                    EndDate = doc.ContainsField("endDate") ? doc.GetValue<Timestamp>("endDate").ToDateTime() : DateTime.Now,
                    IsActive = doc.ContainsField("isActive") && doc.GetValue<bool>("isActive"),
                    MaxDevices = doc.ContainsField("maxDevices") ? doc.GetValue<int>("maxDevices") : 1
                });
            }
            return licenses;
        }
        
        /// <summary>
        /// Thêm license mới
        /// </summary>
        public async Task<bool> AddLicenseAsync(LicenseModel license)
        {
            if (_db == null) return false;
            
            var docRef = _db.Collection("licenses").Document();
            await docRef.SetAsync(new Dictionary<string, object>
            {
                { "userId", license.UserId },
                { "startDate", Timestamp.FromDateTime(license.StartDate.ToUniversalTime()) },
                { "endDate", Timestamp.FromDateTime(license.EndDate.ToUniversalTime()) },
                { "isActive", license.IsActive },
                { "maxDevices", license.MaxDevices }
            });
            return true;
        }
        
        /// <summary>
        /// Gia hạn license
        /// </summary>
        public async Task<bool> ExtendLicenseAsync(string licenseId, int days)
        {
            if (_db == null) return false;
            
            var docRef = _db.Collection("licenses").Document(licenseId);
            var doc = await docRef.GetSnapshotAsync();
            
            if (!doc.Exists) return false;
            
            var currentEndDate = doc.GetValue<Timestamp>("endDate").ToDateTime();
            var newEndDate = currentEndDate > DateTime.UtcNow ? currentEndDate.AddDays(days) : DateTime.UtcNow.AddDays(days);
            
            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                { "endDate", Timestamp.FromDateTime(newEndDate) },
                { "isActive", true }
            });
            return true;
        }
        
        /// <summary>
        /// Vô hiệu hóa license
        /// </summary>
        public async Task<bool> DeactivateLicenseAsync(string licenseId)
        {
            if (_db == null) return false;
            
            await _db.Collection("licenses").Document(licenseId).UpdateAsync(new Dictionary<string, object>
            {
                { "isActive", false }
            });
            return true;
        }
        
        #endregion
        
        #region Devices
        
        /// <summary>
        /// Lấy danh sách thiết bị đang hoạt động của một license
        /// </summary>
        public async Task<List<ActiveDeviceModel>> GetActiveDevicesAsync(string licenseId)
        {
            var devices = new List<ActiveDeviceModel>();
            if (_db == null || string.IsNullOrEmpty(licenseId)) return devices;
            
            var devicesRef = _db.Collection("licenses")
                                .Document(licenseId)
                                .Collection("active_devices");
            
            var snapshot = await devicesRef.GetSnapshotAsync();
            
            foreach (var doc in snapshot.Documents)
            {
                devices.Add(new ActiveDeviceModel
                {
                    Id = doc.Id,
                    LicenseId = licenseId,
                    Hwid = doc.ContainsField("hwid") ? doc.GetValue<string>("hwid") : "",
                    DeviceName = doc.ContainsField("deviceName") ? doc.GetValue<string>("deviceName") : "Unknown",
                    LoginTime = doc.ContainsField("loginTime") ? doc.GetValue<Timestamp>("loginTime").ToDateTime() : DateTime.MinValue,
                    LastSeen = doc.ContainsField("lastSeen") ? doc.GetValue<Timestamp>("lastSeen").ToDateTime() : DateTime.MinValue
                });
            }
            
            return devices;
        }
        
        /// <summary>
        /// Lấy tất cả thiết bị đang hoạt động trên toàn hệ thống
        /// </summary>
        public async Task<List<ActiveDeviceModel>> GetAllActiveDevicesAsync()
        {
            var allDevices = new List<ActiveDeviceModel>();
            if (_db == null) return allDevices;
            
            // Lấy tất cả licenses
            var licensesSnapshot = await _db.Collection("licenses").GetSnapshotAsync();
            
            foreach (var licenseDoc in licensesSnapshot.Documents)
            {
                var devices = await GetActiveDevicesAsync(licenseDoc.Id);
                foreach (var device in devices)
                {
                    // Thêm thông tin userId để hiển thị
                    device.LicenseId = licenseDoc.ContainsField("userId") 
                        ? licenseDoc.GetValue<string>("userId") 
                        : licenseDoc.Id;
                }
                allDevices.AddRange(devices);
            }
            
            return allDevices;
        }
        
        /// <summary>
        /// Xóa thiết bị thủ công (Admin)
        /// </summary>
        public async Task<bool> ForceRemoveDeviceAsync(string licenseId, string deviceId)
        {
            if (_db == null || string.IsNullOrEmpty(licenseId) || string.IsNullOrEmpty(deviceId)) 
                return false;
            
            try
            {
                await _db.Collection("licenses")
                         .Document(licenseId)
                         .Collection("active_devices")
                         .Document(deviceId)
                         .DeleteAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Xóa tất cả thiết bị của một license
        /// </summary>
        public async Task<int> ClearAllDevicesAsync(string licenseId)
        {
            if (_db == null || string.IsNullOrEmpty(licenseId)) return 0;
            
            int count = 0;
            var devicesRef = _db.Collection("licenses")
                                .Document(licenseId)
                                .Collection("active_devices");
            
            var snapshot = await devicesRef.GetSnapshotAsync();
            
            foreach (var doc in snapshot.Documents)
            {
                await doc.Reference.DeleteAsync();
                count++;
            }
            
            return count;
        }
        
        /// <summary>
        /// Dọn dẹp thiết bị không hoạt động > X ngày
        /// </summary>
        public async Task<int> CleanupInactiveDevicesAsync(int inactiveDays = 7)
        {
            if (_db == null) return 0;
            
            int cleanedCount = 0;
            var cutoffTime = DateTime.UtcNow.AddDays(-inactiveDays);
            
            // Lấy tất cả licenses
            var licensesSnapshot = await _db.Collection("licenses").GetSnapshotAsync();
            
            foreach (var licenseDoc in licensesSnapshot.Documents)
            {
                var devicesRef = _db.Collection("licenses")
                                    .Document(licenseDoc.Id)
                                    .Collection("active_devices");
                
                var devicesSnapshot = await devicesRef.GetSnapshotAsync();
                
                foreach (var deviceDoc in devicesSnapshot.Documents)
                {
                    if (deviceDoc.ContainsField("lastSeen"))
                    {
                        var lastSeen = deviceDoc.GetValue<Timestamp>("lastSeen").ToDateTime();
                        if (lastSeen < cutoffTime)
                        {
                            await deviceDoc.Reference.DeleteAsync();
                            cleanedCount++;
                        }
                    }
                }
            }
            
            return cleanedCount;
        }
        
        #endregion
        
        #region App Update
        
        /// <summary>
        /// Lấy cấu hình cập nhật hiện tại
        /// </summary>
        public async Task<Dictionary<string, object>?> GetUpdateConfigAsync()
        {
            if (_db == null) return null;
            
            var docRef = _db.Collection("settings").Document("app_config");
            var snapshot = await docRef.GetSnapshotAsync();
            
            if (snapshot.Exists)
                return snapshot.ToDictionary();
            
            return null;
        }
        
        /// <summary>
        /// Phát hành phiên bản mới
        /// </summary>
        public async Task<bool> PublishUpdateAsync(string version, string url, string notes, bool isCritical)
        {
            if (_db == null) return false;
            
            try
            {
                var docRef = _db.Collection("settings").Document("app_config");
                await docRef.SetAsync(new Dictionary<string, object>
                {
                    { "latestVersion", version },
                    { "updateUrl", url },
                    { "updateNotes", notes },
                    { "isCritical", isCritical },
                    { "publishedAt", Timestamp.FromDateTime(DateTime.UtcNow) }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        #endregion
    }
}
