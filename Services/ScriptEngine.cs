using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using auto_chinhdo.Helpers;
using auto_chinhdo.Models;
using auto_chinhdo.Models.Scripting;
using AdvancedSharpAdbClient.Models;

namespace auto_chinhdo.Services
{
    public class ScriptEngine : IScriptEngine
    {
        private readonly IAdbService _adbService;
        private readonly IOcrService? _ocrService;

        // Event để log ra UI
        public event Action<string>? OnLog;

        public ScriptEngine(IAdbService adbService, IOcrService? ocrService = null)
        {
            _adbService = adbService;
            _ocrService = ocrService ?? new OcrService();
        }

        public ScriptProfile? LoadScript(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<ScriptProfile>(json);
            }
            catch (Exception ex)
            {
                Log($"Error loading script: {ex.Message}");
                return null;
            }
        }

        private void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
            OnLog?.Invoke(message);
        }

        public async Task RunScriptAsync(DeviceItem device, ScriptProfile script, CancellationToken ct)
        {
            if (device.Raw is not DeviceData rawDevice || script.Steps.Count == 0) return;

            string? currentStepId = script.Steps[0].Id;
            var stepsMap = script.Steps.ToDictionary(s => s.Id);
            string templateDir = AppSettings.GetDeviceTemplateDir(device.Serial);

            var retryCounters = new Dictionary<string, int>();

            Log($"🚀 Bắt đầu kịch bản: {script.Name} ({script.Steps.Count} bước)");

            while (!ct.IsCancellationRequested && !string.IsNullOrEmpty(currentStepId))
            {
                if (!stepsMap.TryGetValue(currentStepId, out var currentStep))
                {
                    Log($"❌ Không tìm thấy step ID: {currentStepId}");
                    break;
                }

                int stepIndex = script.Steps.IndexOf(currentStep) + 1;
                Log($"📍 Bước {stepIndex}/{script.Steps.Count}: {currentStep.Description}");

                bool success = false;
                int targetX = 0, targetY = 0;

                // Capture screen
                string screenPath = AppSettings.GetScreenPath(device.Serial);
                await _adbService.CaptureScreenAsync(device, screenPath);

                if (!File.Exists(screenPath))
                {
                    Log($"⚠️ Không chụp được màn hình, thử lại...");
                    await Task.Delay(500, ct);
                    continue;
                }

                // XỬ LÝ TÙY THEO ACTION TYPE
                if (currentStep.Action == ScriptActionType.TapText)
                {
                    // === OCR: Tìm text trên màn hình ===
                    if (_ocrService != null && !string.IsNullOrEmpty(currentStep.TextToFind))
                    {
                        var ocrResult = await Task.Run(() =>
                            _ocrService.FindText(screenPath, currentStep.TextToFind, currentStep.ExactMatch), ct);

                        if (ocrResult != null)
                        {
                            success = true;
                            targetX = ocrResult.Center.X + currentStep.OffsetX;
                            targetY = ocrResult.Center.Y + currentStep.OffsetY;
                            Log($"🔤 OCR tìm thấy: \"{ocrResult.Text}\" tại ({targetX}, {targetY})");
                        }
                        else
                        {
                            Log($"❌ OCR không tìm thấy: \"{currentStep.TextToFind}\"");
                        }
                    }
                    else
                    {
                        Log($"⚠️ OCR không khả dụng hoặc TextToFind trống");
                    }
                }
                else
                {
                    // === Template Matching: Tìm ảnh ===
                    string tplPath = Path.Combine(templateDir, currentStep.TemplateName);
                    
                    if (!string.IsNullOrEmpty(currentStep.TemplateName) && File.Exists(tplPath))
                    {
                        var match = await Task.Run(() =>
                            OpenCvLogic.MatchAny(screenPath, new[] { tplPath }, currentStep.Threshold), ct);

                        if (match != null)
                        {
                            success = true;
                            targetX = (int)match.Value.center.X + currentStep.OffsetX;
                            targetY = (int)match.Value.center.Y + currentStep.OffsetY;
                            Log($"✅ Tìm thấy template: {currentStep.TemplateName}");
                        }
                        else
                        {
                            Log($"❌ Không tìm thấy: {currentStep.TemplateName}");
                        }
                    }
                    else if (currentStep.Action == ScriptActionType.Wait)
                    {
                        // Wait không cần tìm ảnh
                        success = true;
                    }
                }

                // THỰC HIỆN ACTION NẾU THÀNH CÔNG
                if (success)
                {
                    switch (currentStep.Action)
                    {
                        case ScriptActionType.Tap:
                        case ScriptActionType.TapText:
                            _adbService.PerformTap(rawDevice, targetX, targetY);
                            Log($"👆 Tap ({targetX}, {targetY})");
                            break;

                        case ScriptActionType.Wait:
                            Log($"⏳ Wait {currentStep.DelayAfterMs}ms");
                            break;

                        case ScriptActionType.DoubleTap:
                            _adbService.PerformTap(rawDevice, targetX, targetY);
                            await Task.Delay(100, ct);
                            _adbService.PerformTap(rawDevice, targetX, targetY);
                            Log($"👆👆 DoubleTap ({targetX}, {targetY})");
                            break;
                    }

                    if (currentStep.DelayAfterMs > 0)
                        await Task.Delay(currentStep.DelayAfterMs, ct);

                    retryCounters[currentStepId] = 0;
                }

                // ĐIỀU HƯỚNG
                if (success)
                {
                    currentStepId = currentStep.NextStepId;

                    if (string.IsNullOrEmpty(currentStepId))
                    {
                        var index = script.Steps.IndexOf(currentStep);
                        if (index >= 0 && index < script.Steps.Count - 1)
                        {
                            currentStepId = script.Steps[index + 1].Id;
                        }
                        else
                        {
                            Log($"🏁 Hoàn thành kịch bản!");
                            currentStepId = null;
                        }
                    }
                }
                else
                {
                    // XỬ LÝ LỖI
                    switch (currentStep.OnFail)
                    {
                        case OnFailBehavior.Stop:
                            Log($"🛑 DỪNG: Không tìm thấy tại bước {stepIndex}");
                            currentStepId = null;
                            break;

                        case OnFailBehavior.RetryFromStart:
                            Log($"🔄 Quay về bước 1...");
                            currentStepId = script.Steps[0].Id;
                            await Task.Delay(currentStep.RetryDelayMs, ct);
                            break;

                        case OnFailBehavior.RetryCurrentStep:
                            if (!retryCounters.ContainsKey(currentStepId))
                                retryCounters[currentStepId] = 0;

                            retryCounters[currentStepId]++;

                            if (retryCounters[currentStepId] < currentStep.RetryCount)
                            {
                                Log($"🔁 Thử lại ({retryCounters[currentStepId]}/{currentStep.RetryCount})...");
                                await Task.Delay(currentStep.RetryDelayMs, ct);
                            }
                            else
                            {
                                Log($"🛑 Đã thử {currentStep.RetryCount} lần. DỪNG.");
                                currentStepId = null;
                            }
                            break;

                        case OnFailBehavior.SkipToNext:
                            Log($"⏭️ Bỏ qua...");
                            var idx = script.Steps.IndexOf(currentStep);
                            currentStepId = idx >= 0 && idx < script.Steps.Count - 1
                                ? script.Steps[idx + 1].Id : null;
                            break;

                        case OnFailBehavior.GotoStep:
                            currentStepId = currentStep.OnFailStepId;
                            break;
                    }
                }
            }

            Log($"📋 Kịch bản kết thúc.");
        }
    }
}
