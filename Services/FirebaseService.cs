using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using auto_chinhdo.Models;

namespace auto_chinhdo.Services
{
    /// <summary>
    /// Service quản lý Firebase Authentication và License
    /// Sử dụng hoàn toàn REST API - không cần firebase-admin-key.json
    /// </summary>
    public class FirebaseService
    {
        private static FirebaseService? _instance;
        private static readonly object _lock = new();
        private static readonly HttpClient _httpClient = new HttpClient();
        
        // === Firebase REST API Configuration ===
        private const string FIREBASE_API_KEY = "AIzaSyAz0_o_MrC8X9dX9zARQdhAMAgPLdpbpX4";
        private const string FIREBASE_PROJECT_ID = "autoldplayer-license";
        
        // Firebase Auth REST API
        private const string AUTH_URL = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword";
        
        // Firestore REST API
        private const string FIRESTORE_BASE_URL = "https://firestore.googleapis.com/v1/projects/{0}/databases/(default)/documents";
        
        // Thời gian cleanup device không hoạt động (7 ngày)
        private const int DEVICE_INACTIVE_DAYS = 7;
        
        // ID Token từ Firebase Auth (dùng để xác thực Firestore REST API)
        private string? _idToken;
        private DateTime _tokenExpiry;
        
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
        
        private FirebaseService() 
        {
            IsInitialized = true; // Không cần khởi tạo phức tạp nữa
        }
        
        // =========================================================
        // HARDWARE ID - Định danh duy nhất cho máy tính
        // =========================================================
        
        public string GetHardwareId()
        {
            try
            {
                string cpuId = GetWmiProperty("Win32_Processor", "ProcessorId");
                string motherboardSerial = GetWmiProperty("Win32_BaseBoard", "SerialNumber");
                string volumeSerial = GetWmiProperty("Win32_LogicalDisk", "VolumeSerialNumber", "DeviceID = 'C:'");
                
                string combined = $"{cpuId}|{motherboardSerial}|{volumeSerial}";
                
                using var sha256 = SHA256.Create();
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                string hwid = Convert.ToBase64String(hashBytes).Substring(0, 32);
                
                return hwid;
            }
            catch
            {
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
        
        public string GetDeviceName() => Environment.MachineName;
        
        // =========================================================
        // FIREBASE AUTH REST API
        // =========================================================
        
        /// <summary>
        /// Đăng nhập với Firebase Auth REST API hoặc Firestore
        /// </summary>
        public async Task<(bool success, string message)> LoginAsync(string emailOrUsername, string password)
        {
            try
            {
                bool isEmail = emailOrUsername.Contains("@");
                System.Diagnostics.Debug.WriteLine($"[Login] Input: {emailOrUsername}, IsEmail: {isEmail}");
                
                string? userId = null;
                
                if (isEmail)
                {
                    // Thử Firebase Auth trước
                    var authResult = await AuthenticateWithFirebaseAuthAsync(emailOrUsername, password);
                    
                    if (authResult.success)
                    {
                        userId = authResult.userId;
                        _idToken = authResult.idToken;
                        _tokenExpiry = DateTime.Now.AddHours(1);
                        System.Diagnostics.Debug.WriteLine($"[Firebase Auth] Success. UID: {userId}");
                    }
                    else
                    {
                        // Fallback: Thử Firestore
                        System.Diagnostics.Debug.WriteLine("[Firebase Auth] Failed, trying Firestore...");
                        var firestoreResult = await AuthenticateWithFirestoreAsync(emailOrUsername, password);
                        
                        if (!firestoreResult.success)
                        {
                            return (false, firestoreResult.message);
                        }
                        userId = firestoreResult.userId;
                    }
                }
                else
                {
                    // Username: Dùng Firestore
                    var firestoreResult = await AuthenticateWithFirestoreAsync(emailOrUsername, password);
                    
                    if (!firestoreResult.success)
                    {
                        return (false, firestoreResult.message);
                    }
                    userId = firestoreResult.userId;
                }
                
                CurrentUserEmail = emailOrUsername;
                CurrentUserId = userId;
                
                // Kiểm tra license
                bool licenseValid = await CheckLicenseAsync();
                if (!licenseValid)
                {
                    return (false, "License đã hết hạn hoặc không có license");
                }
                
                // Kiểm tra giới hạn thiết bị
                var (deviceAllowed, deviceMessage) = await CheckAndRegisterDeviceAsync();
                if (!deviceAllowed)
                {
                    CurrentUserId = null;
                    CurrentUserEmail = null;
                    CurrentLicenseId = null;
                    return (false, $"⚠️ {deviceMessage}");
                }
                
                return (true, "Đăng nhập thành công");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Login] Error: {ex.Message}");
                return (false, $"Lỗi đăng nhập: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Xác thực với Firebase Auth REST API
        /// </summary>
        private async Task<(bool success, string message, string? userId, string? idToken)> AuthenticateWithFirebaseAuthAsync(string email, string password)
        {
            try
            {
                var requestUrl = $"{AUTH_URL}?key={FIREBASE_API_KEY}";
                var requestBody = new { email, password, returnSecureToken = true };
                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(requestUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    var root = doc.RootElement;
                    string localId = root.GetProperty("localId").GetString() ?? "";
                    string idToken = root.GetProperty("idToken").GetString() ?? "";
                    return (true, "OK", localId, idToken);
                }
                else
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(responseBody);
                        var error = doc.RootElement.GetProperty("error");
                        var errorMessage = error.GetProperty("message").GetString() ?? "Unknown error";
                        
                        string userMessage = errorMessage switch
                        {
                            "EMAIL_NOT_FOUND" => "Email không tồn tại.",
                            "INVALID_PASSWORD" => "Mật khẩu không đúng.",
                            "INVALID_EMAIL" => "Email không hợp lệ.",
                            "USER_DISABLED" => "Tài khoản đã bị vô hiệu hóa.",
                            "TOO_MANY_ATTEMPTS_TRY_LATER" => "Quá nhiều lần thử. Thử lại sau.",
                            "INVALID_LOGIN_CREDENTIALS" => "Email hoặc mật khẩu không đúng.",
                            _ => $"Lỗi: {errorMessage}"
                        };
                        return (false, userMessage, null, null);
                    }
                    catch
                    {
                        return (false, $"Lỗi kết nối: {response.StatusCode}", null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi mạng: {ex.Message}", null, null);
            }
        }
        
        /// <summary>
        /// Xác thực với Firestore REST API (cho username không có @)
        /// </summary>
        private async Task<(bool success, string message, string? userId)> AuthenticateWithFirestoreAsync(string username, string password)
        {
            try
            {
                // Query Firestore REST API: GET documents where email == username
                var baseUrl = string.Format(FIRESTORE_BASE_URL, FIREBASE_PROJECT_ID);
                var queryUrl = $"{baseUrl}:runQuery?key={FIREBASE_API_KEY}";
                
                var query = new
                {
                    structuredQuery = new
                    {
                        from = new[] { new { collectionId = "user" } },
                        where = new
                        {
                            fieldFilter = new
                            {
                                field = new { fieldPath = "email" },
                                op = "EQUAL",
                                value = new { stringValue = username }
                            }
                        },
                        limit = 1
                    }
                };
                
                var jsonContent = JsonSerializer.Serialize(query);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(queryUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"[Firestore Query] Status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    return (false, $"Lỗi truy vấn database: {response.StatusCode}", null);
                }
                
                using var doc = JsonDocument.Parse(responseBody);
                var results = doc.RootElement.EnumerateArray().ToList();
                
                if (results.Count == 0 || !results[0].TryGetProperty("document", out var docElement))
                {
                    return (false, $"Tài khoản '{username}' không tồn tại.", null);
                }
                
                // Lấy document name để extract ID
                var docName = docElement.GetProperty("name").GetString() ?? "";
                var docId = docName.Split('/').Last();
                
                // Lấy password từ fields
                var fields = docElement.GetProperty("fields");
                if (!fields.TryGetProperty("password", out var passwordField))
                {
                    return (false, "Tài khoản chưa thiết lập mật khẩu.", null);
                }
                
                var storedPassword = passwordField.GetProperty("stringValue").GetString() ?? "";
                
                if (storedPassword != password)
                {
                    return (false, "Mật khẩu không đúng.", null);
                }
                
                return (true, "OK", docId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Firestore Auth] Error: {ex.Message}");
                return (false, $"Lỗi xác thực: {ex.Message}", null);
            }
        }
        
        // =========================================================
        // LICENSE CHECK - Firestore REST API
        // =========================================================
        
        public async Task<bool> CheckLicenseAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentUserEmail))
                    return false;
                
                var baseUrl = string.Format(FIRESTORE_BASE_URL, FIREBASE_PROJECT_ID);
                var queryUrl = $"{baseUrl}:runQuery?key={FIREBASE_API_KEY}";
                
                // Query: licenses where userId == CurrentUserEmail AND isActive == true
                var query = new
                {
                    structuredQuery = new
                    {
                        from = new[] { new { collectionId = "licenses" } },
                        where = new
                        {
                            compositeFilter = new
                            {
                                op = "AND",
                                filters = new object[]
                                {
                                    new {
                                        fieldFilter = new {
                                            field = new { fieldPath = "userId" },
                                            op = "EQUAL",
                                            value = new { stringValue = CurrentUserEmail }
                                        }
                                    },
                                    new {
                                        fieldFilter = new {
                                            field = new { fieldPath = "isActive" },
                                            op = "EQUAL",
                                            value = new { booleanValue = true }
                                        }
                                    }
                                }
                            }
                        },
                        limit = 1
                    }
                };
                
                var jsonContent = JsonSerializer.Serialize(query);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(queryUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[License Check] Error: {response.StatusCode}");
                    return false;
                }
                
                using var doc = JsonDocument.Parse(responseBody);
                var results = doc.RootElement.EnumerateArray().ToList();
                
                if (results.Count == 0 || !results[0].TryGetProperty("document", out var docElement))
                {
                    LicenseEndDate = null;
                    CurrentLicenseId = null;
                    MaxDevices = 1;
                    return false;
                }
                
                // Extract license info
                var docName = docElement.GetProperty("name").GetString() ?? "";
                CurrentLicenseId = docName.Split('/').Last();
                
                var fields = docElement.GetProperty("fields");
                
                // Parse endDate
                if (fields.TryGetProperty("endDate", out var endDateField) &&
                    endDateField.TryGetProperty("timestampValue", out var timestampValue))
                {
                    if (DateTime.TryParse(timestampValue.GetString(), out var endDate))
                    {
                        LicenseEndDate = endDate;
                    }
                }
                
                // Parse maxDevices
                if (fields.TryGetProperty("maxDevices", out var maxDevicesField) &&
                    maxDevicesField.TryGetProperty("integerValue", out var intValue))
                {
                    MaxDevices = int.Parse(intValue.GetString() ?? "1");
                }
                
                System.Diagnostics.Debug.WriteLine($"[License] ID={CurrentLicenseId}, MaxDevices={MaxDevices}, EndDate={LicenseEndDate}");
                
                return IsLicenseValid;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[License Check] Error: {ex.Message}");
                return false;
            }
        }
        
        // =========================================================
        // DEVICE MANAGEMENT - Firestore REST API
        // =========================================================
        
        public async Task<(bool allowed, string message)> CheckAndRegisterDeviceAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentLicenseId))
                    return (false, "Chưa có thông tin license");
                
                string hwid = CurrentHwid;
                string deviceName = GetDeviceName();
                
                var baseUrl = string.Format(FIRESTORE_BASE_URL, FIREBASE_PROJECT_ID);
                
                // Get all active devices
                var devicesUrl = $"{baseUrl}/licenses/{CurrentLicenseId}/active_devices?key={FIREBASE_API_KEY}";
                var response = await _httpClient.GetAsync(devicesUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    // Collection might not exist yet - that's OK
                    System.Diagnostics.Debug.WriteLine($"[Devices] No devices collection yet");
                }
                
                var responseBody = await response.Content.ReadAsStringAsync();
                var existingDevices = new List<(string docId, string hwid, string deviceName, DateTime lastSeen)>();
                
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("documents", out var documents))
                    {
                        foreach (var deviceDoc in documents.EnumerateArray())
                        {
                            var name = deviceDoc.GetProperty("name").GetString() ?? "";
                            var docId = name.Split('/').Last();
                            var fields = deviceDoc.GetProperty("fields");
                            
                            string devHwid = "";
                            string devName = "";
                            DateTime lastSeen = DateTime.MinValue;
                            
                            if (fields.TryGetProperty("hwid", out var hwidField))
                                devHwid = hwidField.GetProperty("stringValue").GetString() ?? "";
                            if (fields.TryGetProperty("deviceName", out var nameField))
                                devName = nameField.GetProperty("stringValue").GetString() ?? "";
                            if (fields.TryGetProperty("lastSeen", out var lastSeenField) &&
                                lastSeenField.TryGetProperty("timestampValue", out var tsValue))
                            {
                                DateTime.TryParse(tsValue.GetString(), out lastSeen);
                            }
                            
                            existingDevices.Add((docId, devHwid, devName, lastSeen));
                        }
                    }
                }
                catch { }
                
                // Cleanup inactive devices (> 7 days)
                var cutoffTime = DateTime.UtcNow.AddDays(-DEVICE_INACTIVE_DAYS);
                foreach (var dev in existingDevices.Where(d => d.lastSeen < cutoffTime))
                {
                    await DeleteDeviceAsync(dev.docId);
                }
                
                // Check if current device already registered
                var existingDevice = existingDevices.FirstOrDefault(d => d.hwid == hwid);
                
                if (!string.IsNullOrEmpty(existingDevice.docId))
                {
                    // Update lastSeen
                    await UpdateDeviceLastSeenAsync(existingDevice.docId);
                    return (true, "Thiết bị đã được đăng ký");
                }
                
                // Check slot availability
                int activeCount = existingDevices.Count(d => d.lastSeen >= cutoffTime);
                if (activeCount >= MaxDevices)
                {
                    var deviceList = string.Join(", ", existingDevices.Select(d => d.deviceName));
                    return (false, $"Đã đạt giới hạn {MaxDevices} thiết bị. Thiết bị đang hoạt động: {deviceList}");
                }
                
                // Register new device
                await RegisterNewDeviceAsync(hwid, deviceName);
                
                return (true, $"Đăng ký thiết bị thành công ({activeCount + 1}/{MaxDevices})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Device Check] Error: {ex.Message}");
                return (false, $"Lỗi kiểm tra thiết bị: {ex.Message}");
            }
        }
        
        private async Task RegisterNewDeviceAsync(string hwid, string deviceName)
        {
            var baseUrl = string.Format(FIRESTORE_BASE_URL, FIREBASE_PROJECT_ID);
            var createUrl = $"{baseUrl}/licenses/{CurrentLicenseId}/active_devices?key={FIREBASE_API_KEY}";
            
            var deviceData = new
            {
                fields = new
                {
                    hwid = new { stringValue = hwid },
                    deviceName = new { stringValue = deviceName },
                    loginTime = new { timestampValue = DateTime.UtcNow.ToString("o") },
                    lastSeen = new { timestampValue = DateTime.UtcNow.ToString("o") }
                }
            };
            
            var jsonContent = JsonSerializer.Serialize(deviceData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            await _httpClient.PostAsync(createUrl, content);
        }
        
        private async Task UpdateDeviceLastSeenAsync(string docId)
        {
            var baseUrl = string.Format(FIRESTORE_BASE_URL, FIREBASE_PROJECT_ID);
            var updateUrl = $"{baseUrl}/licenses/{CurrentLicenseId}/active_devices/{docId}?updateMask.fieldPaths=lastSeen&key={FIREBASE_API_KEY}";
            
            var updateData = new
            {
                fields = new
                {
                    lastSeen = new { timestampValue = DateTime.UtcNow.ToString("o") }
                }
            };
            
            var jsonContent = JsonSerializer.Serialize(updateData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            await _httpClient.PatchAsync(updateUrl, content);
        }
        
        private async Task DeleteDeviceAsync(string docId)
        {
            var baseUrl = string.Format(FIRESTORE_BASE_URL, FIREBASE_PROJECT_ID);
            var deleteUrl = $"{baseUrl}/licenses/{CurrentLicenseId}/active_devices/{docId}?key={FIREBASE_API_KEY}";
            
            await _httpClient.DeleteAsync(deleteUrl);
        }
        
        public async Task UnregisterDeviceAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentLicenseId))
                    return;
                
                string hwid = CurrentHwid;
                var baseUrl = string.Format(FIRESTORE_BASE_URL, FIREBASE_PROJECT_ID);
                var devicesUrl = $"{baseUrl}/licenses/{CurrentLicenseId}/active_devices?key={FIREBASE_API_KEY}";
                
                var response = await _httpClient.GetAsync(devicesUrl);
                if (!response.IsSuccessStatusCode) return;
                
                var responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);
                
                if (doc.RootElement.TryGetProperty("documents", out var documents))
                {
                    foreach (var deviceDoc in documents.EnumerateArray())
                    {
                        var fields = deviceDoc.GetProperty("fields");
                        if (fields.TryGetProperty("hwid", out var hwidField))
                        {
                            var devHwid = hwidField.GetProperty("stringValue").GetString() ?? "";
                            if (devHwid == hwid)
                            {
                                var name = deviceDoc.GetProperty("name").GetString() ?? "";
                                var docId = name.Split('/').Last();
                                await DeleteDeviceAsync(docId);
                                break;
                            }
                        }
                    }
                }
            }
            catch { }
        }
        
        public async Task UpdateLastSeenAsync()
        {
            // Simplified - already handled in CheckAndRegisterDevice
        }
        
        // =========================================================
        // OTHER METHODS
        // =========================================================
        
        public void Logout()
        {
            CurrentUserId = null;
            CurrentUserEmail = null;
            LicenseEndDate = null;
            CurrentLicenseId = null;
            MaxDevices = 1;
            _idToken = null;
        }
        
        public int GetRemainingDays()
        {
            if (!LicenseEndDate.HasValue)
                return 0;
            
            var remaining = (LicenseEndDate.Value - DateTime.Now).Days;
            return Math.Max(0, remaining);
        }
        
        public async Task<bool> InitializeAsync()
        {
            // Không cần khởi tạo phức tạp với REST API
            IsInitialized = true;
            return true;
        }
        
        public async Task<AppUpdateConfig?> CheckForUpdateAsync()
        {
            try
            {
                var baseUrl = string.Format(FIRESTORE_BASE_URL, FIREBASE_PROJECT_ID);
                var docUrl = $"{baseUrl}/settings/app_config?key={FIREBASE_API_KEY}";
                
                var response = await _httpClient.GetAsync(docUrl);
                if (!response.IsSuccessStatusCode) return null;
                
                var responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);
                
                if (!doc.RootElement.TryGetProperty("fields", out var fields))
                    return null;
                
                var config = new AppUpdateConfig
                {
                    LatestVersion = GetStringField(fields, "latestVersion") ?? "1.0.0",
                    UpdateUrl = GetStringField(fields, "updateUrl") ?? "",
                    UpdateNotes = GetStringField(fields, "updateNotes") ?? "",
                    IsCritical = GetBoolField(fields, "isCritical")
                };
                
                return config;
            }
            catch
            {
                return null;
            }
        }
        
        private string? GetStringField(JsonElement fields, string fieldName)
        {
            if (fields.TryGetProperty(fieldName, out var field) &&
                field.TryGetProperty("stringValue", out var value))
            {
                return value.GetString();
            }
            return null;
        }
        
        private bool GetBoolField(JsonElement fields, string fieldName)
        {
            if (fields.TryGetProperty(fieldName, out var field) &&
                field.TryGetProperty("booleanValue", out var value))
            {
                return value.GetBoolean();
            }
            return false;
        }
    }
}
