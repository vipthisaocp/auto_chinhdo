using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using auto_chinhdo.Models;
using auto_chinhdo.Views;
using System.Linq;

namespace auto_chinhdo.Services
{
    public class UpdateService
    {
        private static UpdateService? _instance;
        public static UpdateService Instance => _instance ??= new UpdateService();

        private UpdateService() { }

        /// <summary>
        /// Thực hiện quy trình cập nhật với giao diện WPF đẹp
        /// </summary>
        public async Task ProcessUpdateAsync(AppUpdateConfig config)
        {
            // Lưu log trong thư mục phần mềm
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string logPath = Path.Combine(appDir, "update_log.txt");
            
            void WriteLog(string message)
            {
                var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllText(logPath, logLine + Environment.NewLine);
                System.Diagnostics.Debug.WriteLine(logLine);
            }
            
            var progressWindow = new UpdateProgressWindow();
            progressWindow.Closing += (s, e) =>
            {
                // Nếu người dùng đóng cửa sổ khi đang update, tắt sạch app để tránh zombie
                WriteLog("Người dùng đóng cửa sổ tiến trình. Buộc thoát ứng dụng.");
                Environment.Exit(0);
            };
            progressWindow.Show();

            try
            {
                WriteLog($"=== BẮT ĐẦU CẬP NHẬT ===");
                WriteLog($"Phiên bản mới: {config.LatestVersion}");
                WriteLog($"URL: {config.UpdateUrl}");
                
                string tempPath = Path.Combine(appDir, "update_temp");
                WriteLog($"Thư mục tạm: {tempPath}");
                
                // Xóa thư mục tạm cũ (retry nếu bị lock)
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        if (Directory.Exists(tempPath))
                            Directory.Delete(tempPath, true);
                        break;
                    }
                    catch (IOException)
                    {
                        WriteLog($"Thư mục tạm đang bị lock, thử lại ({i + 1}/5)...");
                        await Task.Delay(1000);
                    }
                }
                Directory.CreateDirectory(tempPath);

                string zipFile = Path.Combine(tempPath, "update.zip");
                WriteLog($"File zip: {zipFile}");

                // 1. Tải file ZIP
                progressWindow.UpdateStatus("🔄 Đang tải bản cập nhật...");
                WriteLog("Bắt đầu tải file...");
                
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(10);
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    
                    using var response = await client.GetAsync(config.UpdateUrl, HttpCompletionOption.ResponseHeadersRead);
                    WriteLog($"HTTP Status: {response.StatusCode}");
                    WriteLog($"Content-Type: {response.Content.Headers.ContentType}");
                    response.EnsureSuccessStatusCode();
                    
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    WriteLog($"Kích thước file: {totalBytes} bytes");
                    var canReportProgress = totalBytes != -1;
                    
                    using var contentStream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(zipFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                    
                    var buffer = new byte[8192];
                    long totalBytesRead = 0;
                    int bytesRead;
                    
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                        
                        if (canReportProgress)
                        {
                            var percent = (double)totalBytesRead / totalBytes * 100;
                            progressWindow.UpdateProgress(percent);
                        }
                    }
                    
                    WriteLog($"Đã tải xong: {totalBytesRead} bytes");
                }

                // 2. Kiểm tra file ZIP hợp lệ
                progressWindow.UpdateStatus("🔍 Đang kiểm tra file...");
                var fileInfo = new FileInfo(zipFile);
                WriteLog($"Kích thước file: {fileInfo.Length} bytes");
                
                byte[] headerBytes = new byte[Math.Min(500, (int)fileInfo.Length)];
                using (var fs = new FileStream(zipFile, FileMode.Open, FileAccess.Read))
                {
                    await fs.ReadAsync(headerBytes, 0, headerBytes.Length);
                }
                string headerText = System.Text.Encoding.UTF8.GetString(headerBytes);
                
                if (fileInfo.Length < 1000 || headerBytes[0] != 0x50 || headerBytes[1] != 0x4B)
                {
                    WriteLog($"File không hợp lệ. Magic bytes: {headerBytes[0]:X2} {headerBytes[1]:X2}");
                    if (headerText.Contains("<!DOCTYPE") || headerText.Contains("<html"))
                    {
                        throw new Exception("Link tải trả về trang HTML thay vì file ZIP!\n\nKiểm tra lại link trong Admin Panel.");
                    }
                    throw new Exception("File tải về không phải ZIP hợp lệ.\n\nXem log: " + logPath);
                }
                WriteLog("File ZIP hợp lệ!");

                // 3. Giải nén
                progressWindow.UpdateStatus("📦 Đang giải nén file cập nhật...");
                progressWindow.UpdateProgress(0);
                WriteLog("Bắt đầu giải nén...");
                
                string extractPath = Path.Combine(tempPath, "extracted");
                Directory.CreateDirectory(extractPath);
                
                await Task.Run(() => ZipFile.ExtractToDirectory(zipFile, extractPath, true));
                WriteLog("Giải nén hoàn tất");

                // Tìm thư mục chứa file auto_chinhdo.dll để làm gốc copy (xử lý mọi kiểu nén)
                try
                {
                    var dllFiles = Directory.GetFiles(extractPath, "auto_chinhdo.dll", SearchOption.AllDirectories);
                    if (dllFiles.Length > 0)
                    {
                        extractPath = Path.GetDirectoryName(dllFiles[0])!;
                        WriteLog($"Phát hiện thư mục gốc của bản cập nhật: {extractPath}");
                    }
                    else
                    {
                        WriteLog("CẢNH BÁO: Không tìm thấy auto_chinhdo.dll trong file giải nén.");
                    }
                }
                catch (Exception ex)
                {
                    WriteLog($"Lỗi khi quét thư mục giải nén: {ex.Message}");
                }

                // 4. Tạo script để copy TẤT CẢ file và khởi động lại
                // QUAN TRỌNG: Không copy trong C# vì nhiều DLL bị lock khi app đang chạy!
                progressWindow.UpdateStatus("✅ Sẵn sàng cập nhật! Đang khởi động lại...");
                WriteLog("Tạo script cập nhật (sẽ copy file sau khi app tắt)...");
                
                string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                string batchPath = Path.Combine(tempPath, "update_script.bat");
                string restartLog = Path.Combine(appDir, "restart_log.txt");
                
                // Script sẽ: đợi app tắt -> kill nếu còn treo -> copy TẤT CẢ file từ extractPath sang appDir -> khởi động lại
                string script = $@"
@echo off
chcp 65001 >nul
echo [%date% %time%] === BAT DAU CAP NHAT === > ""{restartLog}""
echo [%date% %time%] Thu muc nguon: ""{extractPath}"" >> ""{restartLog}""
echo [%date% %time%] Thu muc dich: ""{appDir}"" >> ""{restartLog}""

REM Doi app tat hoan toan (2 giay)
echo [%date% %time%] Cho app tat... >> ""{restartLog}""
timeout /t 2 /nobreak > nul

REM Kill process neu van con treo de tranh locked file
echo [%date% %time%] Dam bao process da tat... >> ""{restartLog}""
taskkill /F /IM ""{Path.GetFileName(currentExe)}"" /T >nul 2>&1

REM Copy tat ca file tu thu muc giai nen sang thu muc app
echo [%date% %time%] Dang copy file... >> ""{restartLog}""
xcopy /E /Y /Q ""{extractPath}\*"" ""{appDir}"" /C >> ""{restartLog}"" 2>&1

if errorlevel 1 (
    echo [%date% %time%] LOI: Copy that bai! >> ""{restartLog}""
    echo [%date% %time%] Vui long tat han app va thu lai. >> ""{restartLog}""
    pause
    exit /b 1
)

echo [%date% %time%] Copy thanh cong! >> ""{restartLog}""

REM Khoi dong lai app
echo [%date% %time%] Dang khoi dong app... >> ""{restartLog}""
start """" ""{currentExe}""

echo [%date% %time%] === HOAN TAT === >> ""{restartLog}""

REM Xoa thu muc tam sau 5 giay
timeout /t 5 /nobreak > nul
rmdir /S /Q ""{tempPath}"" 2>nul
del ""%~f0""
";
                await File.WriteAllTextAsync(batchPath, script, System.Text.Encoding.UTF8);
                WriteLog($"Đã tạo script: {batchPath}");
                
                // Chạy script và thoát app
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batchPath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Minimized
                });

                WriteLog("=== THOÁT ỨNG DỤNG ĐỂ SCRIPT CẬP NHẬT CHẠY ===");
                await Task.Delay(500);
                progressWindow.Close();
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                WriteLog($"LỖI: {ex.Message}");
                WriteLog($"Chi tiết: {ex.StackTrace}");
                progressWindow.Close();
                MessageBox.Show($"Lỗi cập nhật: {ex.Message}\n\nXem log tại: {logPath}", "Cập nhật thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
