using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Tesseract;

namespace auto_chinhdo.Services
{
    /// <summary>
    /// Kết quả OCR cho một đoạn text
    /// </summary>
    public class OcrResult
    {
        public string Text { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float Confidence { get; set; }

        /// <summary>
        /// Tâm của vùng text
        /// </summary>
        public (int X, int Y) Center => (X + Width / 2, Y + Height / 2);
    }

    /// <summary>
    /// Interface cho OCR Service
    /// </summary>
    public interface IOcrService
    {
        /// <summary>
        /// Đọc tất cả text từ ảnh và trả về danh sách kết quả với vị trí
        /// </summary>
        List<OcrResult> ReadTextFromImage(string imagePath);

        /// <summary>
        /// Tìm text cụ thể trong ảnh, trả về vị trí nếu tìm thấy
        /// </summary>
        OcrResult? FindText(string imagePath, string textToFind, bool exactMatch = false);
    }

    /// <summary>
    /// OCR Service sử dụng Tesseract
    /// </summary>
    public class OcrService : IOcrService, IDisposable
    {
        private TesseractEngine? _engine;
        private readonly string _tessDataPath;
        private bool _disposed = false;

        public OcrService()
        {
            // Tìm tessdata folder
            _tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            
            if (!Directory.Exists(_tessDataPath))
            {
                Directory.CreateDirectory(_tessDataPath);
            }

            InitializeEngine();
        }

        private void InitializeEngine()
        {
            try
            {
                // Thử khởi tạo với tiếng Việt + Anh
                // Nếu không có file .traineddata, sẽ dùng tiếng Anh mặc định
                string language = "vie+eng";
                
                var vieFile = Path.Combine(_tessDataPath, "vie.traineddata");
                var engFile = Path.Combine(_tessDataPath, "eng.traineddata");
                
                if (!File.Exists(vieFile) && !File.Exists(engFile))
                {
                    // Không có file nào, log warning
                    System.Diagnostics.Debug.WriteLine("⚠️ OCR: Không tìm thấy tessdata. Vui lòng tải eng.traineddata hoặc vie.traineddata");
                    return;
                }

                if (File.Exists(vieFile) && File.Exists(engFile))
                    language = "vie+eng";
                else if (File.Exists(vieFile))
                    language = "vie";
                else
                    language = "eng";

                _engine = new TesseractEngine(_tessDataPath, language, EngineMode.Default);
                System.Diagnostics.Debug.WriteLine($"✅ OCR Engine khởi tạo thành công: {language}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ OCR Engine lỗi: {ex.Message}");
                _engine = null;
            }
        }

        public List<OcrResult> ReadTextFromImage(string imagePath)
        {
            var results = new List<OcrResult>();

            if (_engine == null || !File.Exists(imagePath))
                return results;

            try
            {
                using var img = Pix.LoadFromFile(imagePath);
                using var page = _engine.Process(img);

                using var iter = page.GetIterator();
                iter.Begin();

                do
                {
                    if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out var bounds))
                    {
                        var text = iter.GetText(PageIteratorLevel.Word)?.Trim();
                        if (!string.IsNullOrEmpty(text))
                        {
                            results.Add(new OcrResult
                            {
                                Text = text,
                                X = bounds.X1,
                                Y = bounds.Y1,
                                Width = bounds.Width,
                                Height = bounds.Height,
                                Confidence = iter.GetConfidence(PageIteratorLevel.Word)
                            });
                        }
                    }
                } while (iter.Next(PageIteratorLevel.Word));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OCR Error: {ex.Message}");
            }

            return results;
        }

        public OcrResult? FindText(string imagePath, string textToFind, bool exactMatch = false)
        {
            var allResults = ReadTextFromImage(imagePath);

            foreach (var result in allResults)
            {
                bool match = exactMatch
                    ? result.Text.Equals(textToFind, StringComparison.OrdinalIgnoreCase)
                    : result.Text.Contains(textToFind, StringComparison.OrdinalIgnoreCase);

                if (match)
                {
                    return result;
                }
            }

            // Thử match nhiều từ liền nhau
            if (textToFind.Contains(' '))
            {
                // Tìm chuỗi từ liên tiếp
                var words = textToFind.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i <= allResults.Count - words.Length; i++)
                {
                    bool allMatch = true;
                    for (int j = 0; j < words.Length; j++)
                    {
                        if (!allResults[i + j].Text.Contains(words[j], StringComparison.OrdinalIgnoreCase))
                        {
                            allMatch = false;
                            break;
                        }
                    }

                    if (allMatch)
                    {
                        // Trả về vùng bao gồm tất cả các từ
                        var first = allResults[i];
                        var last = allResults[i + words.Length - 1];
                        return new OcrResult
                        {
                            Text = textToFind,
                            X = first.X,
                            Y = Math.Min(first.Y, last.Y),
                            Width = (last.X + last.Width) - first.X,
                            Height = Math.Max(first.Height, last.Height),
                            Confidence = first.Confidence
                        };
                    }
                }
            }

            return null;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _engine?.Dispose();
                _disposed = true;
            }
        }
    }
}
