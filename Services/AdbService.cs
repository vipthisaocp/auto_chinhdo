using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using auto_chinhdo.Models;
using DeviceData = AdvancedSharpAdbClient.Models.DeviceData;

namespace auto_chinhdo.Services
{
    public class AdbService : IAdbService
    {
        private readonly AdbClient _client;
        private const int ADB_PORT = 5037;

        public AdbService()
        {
            _client = new AdbClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, ADB_PORT));
        }

        public void EnsureServerStarted()
        {
            try
            {
                if (!AdbServer.Instance.GetStatus().IsRunning)
                {
                    var adbPath = AppSettings.Instance.AdbPath;
                    if (File.Exists(adbPath))
                    {
                        AdbServer.Instance.StartServer(adbPath, restartServerIfNewer: false);
                    }
                }
            }
            catch { }
        }

        public async Task EnsureServerIsHealthy(bool forceRestart = false)
        {
            await Task.CompletedTask;
        }

        public async Task<List<DeviceData>> GetDevicesAsync()
        {
            return await Task.Run(() => _client.GetDevices().ToList());
        }

        public async Task CaptureScreenAsync(DeviceItem device, string outputPath)
        {
            if (device.Raw is not DeviceData raw) return;

            await Task.Run(() =>
            {
                try
                {
                    _client.ExecuteRemoteCommand("screencap -p /sdcard/s.png", raw, null, Encoding.UTF8);

                    using (var service = new SyncService(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, ADB_PORT), raw))
                    {
                        using (var stream = File.OpenWrite(outputPath))
                        {
                             service.Pull("/sdcard/s.png", stream, null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Capture Error: {ex}");
                }
            });
        }

        public bool PerformTap(DeviceData device, int x, int y)
        {
            Task.Run(() => _client.ExecuteRemoteCommand($"input tap {x} {y}", device, null, Encoding.UTF8));
            return true;
        }

        public (int Width, int Height) GetScreenSize(DeviceData device)
        {
            return (1280, 720);
        }

        public string ExecuteCommand(DeviceData device, string command)
        {
            // Disable receiver to avoid build error
            _client.ExecuteRemoteCommand(command, device, null, Encoding.UTF8);
            return "";
        }
    }
}
