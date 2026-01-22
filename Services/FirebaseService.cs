using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using auto_chinhdo.Models;

namespace auto_chinhdo.Services
{
    /// <summary>
    /// Service quản lý Firebase Authentication và License
    /// </summary>
    public class FirebaseService
    {
        private static FirebaseService? _instance;
        private static readonly object _lock = new();
        
        private FirebaseApp? _app;
        private FirestoreDb? _db;
        
        // Thời gian cleanup device không hoạt động (7 ngày)
        private const int DEVICE_INACTIVE_DAYS = 7;
        
        public bool IsInitialized { get; private set; }
        public string? CurrentUserId { get; private set; }
        public string? CurrentUserEmail { get; private set; }
        public DateTime? LicenseEndDate { get; private set; }
        public string? CurrentLicenseId { get; private set; }
        public int MaxDevices { get; private set; } = 1;
        public bool IsLicenseValid => LicenseEndDate.HasValue && LicenseEndDate.Value > DateTime.Now;
        
        // HWID của máy hiện tại
        private string? _cachedHwid;
        public string CurrentHwid => _cachedHwid ??= GetHardwareId();
        
        // Singleton instance
        public static FirebaseService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new FirebaseService();
                    }
                }
                return _instance;
            }
        }
        
        private FirebaseService() { }
        
        // =========================================================
        // HARDWARE ID - Định danh duy nhất cho máy tính
        // =========================================================
        
        /// <summary>
        /// Lấy Hardware ID duy nhất của máy tính
        /// Kết hợp: CPU ID + Motherboard Serial + Volume Serial
        /// </summary>
        public string GetHardwareId()
        {
            try
            {
                string cpuId = GetWmiProperty("Win32_Processor", "ProcessorId");
                string motherboardSerial = GetWmiProperty("Win32_BaseBoard", "SerialNumber");
                string volumeSerial = GetWmiProperty("Win32_LogicalDisk", "VolumeSerialNumber", "DeviceID = 'C:'");
                
                string combined = $"{cpuId}|{motherboardSerial}|{volumeSerial}";
                
                // Tạo hash SHA256 để bảo mật và rút gọn
                using var sha256 = SHA256.Create();
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                string hwid = Convert.ToBase64String(hashBytes).Substring(0, 32);
                
                System.Diagnostics.Debug.WriteLine($"HWID Generated: {hwid}");
                return hwid;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting HWID: {ex.Message}");
                // Fallback: Dùng Machine GUID từ Registry
                return GetMachineGuid();
            }
        }
        
        private string GetWmiProperty(string wmiClass, string propertyName, string condition = "")
        {
            try
            {
                string query = string.IsNullOrEmpty(condition) 
                    ? $"SELECT {propertyName} FROM {wmiClass}"
                    : $"SELECT {propertyName} FROM {wmiClass} WHERE {condition}";
                    
                using var searcher = new ManagementObjectSearcher(query);
                foreach (var obj in searcher.Get())
                {
                    var value = obj[propertyName]?.ToString();
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }
            catch { }
            return "UNKNOWN";
        }
        
        private string GetMachineGuid()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Cryptography");
                return key?.GetValue("MachineGuid")?.ToString() ?? Guid.NewGuid().ToString();
            }
            catch
            {
                return Guid.NewGuid().ToString();
            }
        }
        
        /// <summary>
        /// Lấy tên máy tính
        /// </summary>
        public string GetDeviceName()
        {
            return Environment.MachineName;
        }
        
        // =========================================================
        // FIREBASE INITIALIZATION
        // =========================================================
        
        /// <summary>
        /// Khởi tạo Firebase với file service account key
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                string keyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firebase-admin-key.json");
                
                if (!File.Exists(keyPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Firebase key file not found: {keyPath}");
                    IsInitialized = false;
                    return false;
                }
                
                // Thiết lập environment variable cho Google Cloud
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", keyPath);
                
                // Khởi tạo Firebase Admin SDK
                if (FirebaseApp.DefaultInstance == null)
                {
                    _app = FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(keyPath)
                    });
                }
                else
                {
                    _app = FirebaseApp.DefaultInstance;
                }
                
                // Khởi tạo Firestore với credential builder
                var firestoreBuilder = new FirestoreDbBuilder
                {
                    ProjectId = "autoldplayer-license",
                    CredentialsPath = keyPath
                };
                _db = await firestoreBuilder.BuildAsync();
                
                IsInitialized = true;
                System.Diagnostics.Debug.WriteLine("Firebase initialized successfully!");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Firebase Init Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                IsInitialized = false;
                return false;
            }
        }
        
        // =========================================================
        // DEVICE MANAGEMENT - Quản lý thiết bị đăng nhập
        // =========================================================
        
        /// <summary>
        /// Kiểm tra giới hạn thiết bị và đăng ký nếu còn slot
        /// </summary>
        public async Task<(bool allowed, string message)> CheckAndRegisterDeviceAsync()
        {
            try
            {
                if (_db == null || string.IsNullOrEmpty(CurrentLicenseId))
                    return (false, "Chưa có thông tin license");
                
                string hwid = CurrentHwid;
                string deviceName = GetDeviceName();
                
                // Lấy sub-collection active_devices
                var devicesRef = _db.Collection("licenses")
                                    .Document(CurrentLicenseId)
                                    .Collection("active_devices");
                
                // Cleanup: Xóa các device không hoạt động > 7 ngày
                await CleanupInactiveDevicesAsync(devicesRef);
                
                // Đếm số device đang hoạt động
                var snapshot = await devicesRef.GetSnapshotAsync();
                int activeCount = snapshot.Count;
                
                // Kiểm tra xem device hiện tại đã đăng ký chưa
                var existingDevice = snapshot.Documents.FirstOrDefault(d => 
                    d.ContainsField("hwid") && d.GetValue<string>("hwid") == hwid);
                
                if (existingDevice != null)
                {
                    // Device đã đăng ký → Cập nhật lastSeen
                    await existingDevice.Reference.UpdateAsync(new Dictionary<string, object>
                    {
                        { "lastSeen", Timestamp.FromDateTime(DateTime.UtcNow) },
                        { "deviceName", deviceName }
                    });
                    
                    System.Diagnostics.Debug.WriteLine($"Device already registered. Updated lastSeen.");
                    return (true, "Thiết bị đã được đăng ký");
                }
                
                // Device mới → Kiểm tra còn slot không
                if (activeCount >= MaxDevices)
                {
                    string deviceList = string.Join(", ", snapshot.Documents
                        .Select(d => d.ContainsField("deviceName") ? d.GetValue<string>("deviceName") : "Unknown"));
                    
                    return (false, $"Đã đạt giới hạn {MaxDevices} thiết bị. Các thiết bị đang hoạt động: {deviceList}");
                }
                
                // Còn slot → Đăng ký device mới
                await devicesRef.AddAsync(new Dictionary<string, object>
                {
                    { "hwid", hwid },
                    { "deviceName", deviceName },
                    { "loginTime", Timestamp.FromDateTime(DateTime.UtcNow) },
                    { "lastSeen", Timestamp.FromDateTime(DateTime.UtcNow) }
                });
                
                System.Diagnostics.Debug.WriteLine($"New device registered. Active: {activeCount + 1}/{MaxDevices}");
                return (true, $"Đăng ký thiết bị thành công ({activeCount + 1}/{MaxDevices})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckAndRegisterDevice Error: {ex.Message}");
                return (false, $"Lỗi kiểm tra thiết bị: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gỡ đăng ký thiết bị khi đóng app
        /// </summary>
        public async Task UnregisterDeviceAsync()
        {
            try
            {
                if (_db == null || string.IsNullOrEmpty(CurrentLicenseId))
                    return;
                
                string hwid = CurrentHwid;
                
                var devicesRef = _db.Collection("licenses")
                                    .Document(CurrentLicenseId)
                                    .Collection("active_devices");
                
                var query = devicesRef.WhereEqualTo("hwid", hwid);
                var snapshot = await query.GetSnapshotAsync();
                
                foreach (var doc in snapshot.Documents)
                {
                    await doc.Reference.DeleteAsync();
                    System.Diagnostics.Debug.WriteLine($"Device unregistered: {hwid}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UnregisterDevice Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Cập nhật lastSeen định kỳ
        /// </summary>
        public async Task UpdateLastSeenAsync()
        {
            try
            {
                if (_db == null || string.IsNullOrEmpty(CurrentLicenseId))
                    return;
                
                string hwid = CurrentHwid;
                
                var devicesRef = _db.Collection("licenses")
                                    .Document(CurrentLicenseId)
                                    .Collection("active_devices");
                
                var query = devicesRef.WhereEqualTo("hwid", hwid);
                var snapshot = await query.GetSnapshotAsync();
                
                foreach (var doc in snapshot.Documents)
                {
                    await doc.Reference.UpdateAsync(new Dictionary<string, object>
                    {
                        { "lastSeen", Timestamp.FromDateTime(DateTime.UtcNow) }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateLastSeen Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Dọn dẹp các device không hoạt động > 7 ngày
        /// </summary>
        private async Task CleanupInactiveDevicesAsync(CollectionReference devicesRef)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddDays(-DEVICE_INACTIVE_DAYS);
                var snapshot = await devicesRef.GetSnapshotAsync();
                
                foreach (var doc in snapshot.Documents)
                {
                    if (doc.ContainsField("lastSeen"))
                    {
                        var lastSeen = doc.GetValue<Timestamp>("lastSeen").ToDateTime();
                        if (lastSeen < cutoffTime)
                        {
                            await doc.Reference.DeleteAsync();
                            System.Diagnostics.Debug.WriteLine($"Cleaned up inactive device: {doc.Id}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CleanupInactiveDevices Error: {ex.Message}");
            }
        }
        
        // =========================================================
        // LOGIN & LICENSE CHECK
        // =========================================================
        
        /// <summary>
        /// Đăng nhập bằng email và password (lưu trong Firestore)
        /// </summary>
        public async Task<(bool success, string message)> LoginAsync(string email, string password)
        {
            try
            {
                if (!IsInitialized)
                {
                    bool initResult = await InitializeAsync();
                    if (!initResult)
                    {
                        return (false, "Không thể kết nối Firebase. Kiểm tra internet và thử lại.");
                    }
                }
                
                // Kiểm tra _db đã được khởi tạo chưa
                if (_db == null)
                {
                    return (false, "Database chưa được khởi tạo. Thử lại sau.");
                }
                
                System.Diagnostics.Debug.WriteLine($"Searching for email: {email}");
                
                // Tìm user theo email
                var usersRef = _db.Collection("user");
                var query = usersRef.WhereEqualTo("email", email);
                var snapshot = await query.GetSnapshotAsync();
                
                System.Diagnostics.Debug.WriteLine($"Found {snapshot.Count} users with email: {email}");
                
                if (snapshot.Count == 0)
                {
                    return (false, $"Email/UserID '{email}' không tồn tại.");
                }
                
                var userDoc = snapshot.Documents[0];
                
                // Kiểm tra password
                string? storedPassword = userDoc.ContainsField("password") 
                    ? userDoc.GetValue<string>("password") 
                    : null;
                
                if (string.IsNullOrEmpty(storedPassword))
                {
                    return (false, "Tài khoản chưa thiết lập mật khẩu.");
                }
                
                if (storedPassword != password)
                {
                    return (false, "Mật khẩu không đúng");
                }
                
                CurrentUserId = userDoc.Id;
                CurrentUserEmail = email;
                
                // Kiểm tra license
                bool licenseValid = await CheckLicenseAsync();
                
                if (!licenseValid)
                {
                    return (false, "License đã hết hạn hoặc không có license");
                }
                
                // KIỂM TRA GIỚI HẠN THIẾT BỊ (maxDevices)
                var (deviceAllowed, deviceMessage) = await CheckAndRegisterDeviceAsync();
                
                if (!deviceAllowed)
                {
                    // Reset thông tin user vì không cho phép đăng nhập
                    CurrentUserId = null;
                    CurrentUserEmail = null;
                    CurrentLicenseId = null;
                    return (false, $"⚠️ {deviceMessage}");
                }
                
                return (true, "Đăng nhập thành công");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi đăng nhập: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Kiểm tra license của user hiện tại
        /// </summary>
        public async Task<bool> CheckLicenseAsync()
        {
            try
            {
                // Query theo email vì license lưu userId = email
                if (string.IsNullOrEmpty(CurrentUserEmail) || _db == null)
                {
                    return false;
                }
                
                var licensesRef = _db.Collection("licenses");
                // Query theo email hoặc userId (hỗ trợ cả 2 cách)
                var query = licensesRef.WhereEqualTo("userId", CurrentUserEmail)
                                       .WhereEqualTo("isActive", true);
                var snapshot = await query.GetSnapshotAsync();
                
                if (snapshot.Count == 0)
                {
                    LicenseEndDate = null;
                    CurrentLicenseId = null;
                    MaxDevices = 1;
                    return false;
                }
                
                var licenseDoc = snapshot.Documents[0];
                
                // Lưu License ID để dùng cho device management
                CurrentLicenseId = licenseDoc.Id;
                
                // Lấy endDate
                var endDate = licenseDoc.GetValue<Timestamp>("endDate");
                LicenseEndDate = endDate.ToDateTime();
                
                // Lấy maxDevices
                MaxDevices = licenseDoc.ContainsField("maxDevices") 
                    ? licenseDoc.GetValue<int>("maxDevices") 
                    : 1;
                
                System.Diagnostics.Debug.WriteLine($"License found: ID={CurrentLicenseId}, MaxDevices={MaxDevices}, EndDate={LicenseEndDate}");
                
                return IsLicenseValid;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Check License Error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Đăng xuất
        /// </summary>
        public void Logout()
        {
            CurrentUserId = null;
            CurrentUserEmail = null;
            LicenseEndDate = null;
            CurrentLicenseId = null;
            MaxDevices = 1;
        }
        
        /// <summary>
        /// Lấy số ngày còn lại của license
        /// </summary>
        public int GetRemainingDays()
        {
            if (!LicenseEndDate.HasValue)
                return 0;
            
            var remaining = (LicenseEndDate.Value - DateTime.Now).Days;
            return Math.Max(0, remaining);
        }
        
        // =========================================================
        // APP UPDATE - Kiểm tra cập nhật phần mềm
        // =========================================================
        
        /// <summary>
        /// Kiểm tra phiên bản mới nhất từ Firestore (settings/app_config)
        /// </summary>
        public async Task<AppUpdateConfig?> CheckForUpdateAsync()
        {
            try
            {
                if (_db == null) await InitializeAsync();
                if (_db == null) return null;
                
                var docRef = _db.Collection("settings").Document("app_config");
                var snapshot = await docRef.GetSnapshotAsync();
                
                if (!snapshot.Exists)
                {
                    System.Diagnostics.Debug.WriteLine("Update config not found in Firestore.");
                    return null;
                }
                
                var config = new AppUpdateConfig
                {
                    LatestVersion = snapshot.ContainsField("latestVersion") ? snapshot.GetValue<string>("latestVersion") : "1.0.0",
                    UpdateUrl = snapshot.ContainsField("updateUrl") ? snapshot.GetValue<string>("updateUrl") : string.Empty,
                    UpdateNotes = snapshot.ContainsField("updateNotes") ? snapshot.GetValue<string>("updateNotes") : "Không có ghi chú.",
                    IsCritical = snapshot.ContainsField("isCritical") && snapshot.GetValue<bool>("isCritical")
                };
                
                return config;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckForUpdate Error: {ex.Message}");
                return null;
            }
        }
    }
}
