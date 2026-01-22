using System;

namespace auto_chinhdo.Models
{
    public class AppUpdateConfig
    {
        public string LatestVersion { get; set; } = "1.0.0";
        public string UpdateUrl { get; set; } = string.Empty;
        public string UpdateNotes { get; set; } = string.Empty;
        public bool IsCritical { get; set; } = false;
        
        // Trạng thái so sánh
        public bool HasUpdate(string currentVersion)
        {
            if (string.IsNullOrEmpty(LatestVersion) || string.IsNullOrEmpty(currentVersion))
                return false;
            
            // Loại bỏ ký tự "v" hoặ "V" ở đầu nếu có
            string latestClean = LatestVersion.TrimStart('v', 'V');
            string currentClean = currentVersion.TrimStart('v', 'V');
                
            try
            {
                Version vLatest = new Version(latestClean);
                Version vCurrent = new Version(currentClean);
                return vLatest > vCurrent;
            }
            catch
            {
                // Fallback so sánh chuỗi nếu không đúng định dạng Version
                return latestClean != currentClean;
            }
        }
    }
}
