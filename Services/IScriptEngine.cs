using System.Threading;
using System.Threading.Tasks;
using auto_chinhdo.Models;
using auto_chinhdo.Models.Scripting;

namespace auto_chinhdo.Services
{
    public interface IScriptEngine
    {
        /// <summary>
        /// Event để log thông tin ra UI
        /// </summary>
        event Action<string>? OnLog;

        /// <summary>
        /// Tải kịch bản từ file JSON
        /// </summary>
        ScriptProfile? LoadScript(string filePath);

        /// <summary>
        /// Chạy kịch bản trên một thiết bị cụ thể
        /// </summary>
        Task RunScriptAsync(DeviceItem device, ScriptProfile script, CancellationToken ct);
    }
}
