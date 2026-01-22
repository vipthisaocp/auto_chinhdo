using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using auto_chinhdo.Models.Scripting;
using Microsoft.Win32;

namespace auto_chinhdo.Views
{
    public partial class ScriptEditorWindow : Window
    {
        private ObservableCollection<ScriptStep> _steps = new ObservableCollection<ScriptStep>();
        private ScriptStep? _currentStep;
        private bool _isRecording = false;
        private bool _isUpdatingUI = false; // Prevent recursive updates
        private string? _currentFilePath;
        private string _templateDir;

        // Event để MainWindow biết khi record mode đang chờ click
        public event Action<string>? OnRecordStepRequested;

        public ScriptEditorWindow(string templateDirectory, string? existingScriptPath = null)
        {
            InitializeComponent();
            _templateDir = templateDirectory;
            StepsListView.ItemsSource = _steps;

            if (!string.IsNullOrEmpty(existingScriptPath) && File.Exists(existingScriptPath))
            {
                LoadScript(existingScriptPath);
            }
        }

        private void LoadScript(string path)
        {
            try
            {
                var json = File.ReadAllText(path);
                var profile = JsonSerializer.Deserialize<ScriptProfile>(json);
                if (profile != null)
                {
                    ScriptNameBox.Text = profile.Name;
                    _steps.Clear();
                    foreach (var step in profile.Steps)
                    {
                        _steps.Add(step);
                    }
                    RefreshStepNumbers();
                    _currentFilePath = path;
                    StatusText.Text = $"Đã tải: {Path.GetFileName(path)}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải kịch bản: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshStepNumbers()
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                _steps[i].StepNumber = i + 1;
            }
            StepsListView.Items.Refresh();
        }

        #region Toolbar Handlers

        private void BtnRecord_Click(object sender, RoutedEventArgs e)
        {
            _isRecording = !_isRecording;
            if (_isRecording)
            {
                BtnRecord.Content = "⏹️ Dừng Record";
                StatusText.Text = "🔴 RECORD: Click vào màn hình game để thêm bước mới...";
                // Notify MainWindow to enter record mode
                OnRecordStepRequested?.Invoke("START");
            }
            else
            {
                BtnRecord.Content = "🔴 Record";
                StatusText.Text = "Record đã dừng.";
                OnRecordStepRequested?.Invoke("STOP");
            }
        }

        // Called by MainWindow when user clicks on screen during record mode
        public void AddRecordedStep(string templatePath, string description)
        {
            var newStep = new ScriptStep
            {
                Description = description,
                TemplateName = Path.GetFileName(templatePath),
                Action = ScriptActionType.Tap,
                TimeoutMs = 5000,
                DelayAfterMs = 1000,
                OnFail = OnFailBehavior.Stop
            };
            _steps.Add(newStep);
            RefreshStepNumbers();
            StepsListView.SelectedItem = newStep;
            StatusText.Text = $"✅ Đã thêm bước: {description}";
        }

        private void BtnAddStep_Click(object sender, RoutedEventArgs e)
        {
            var newStep = new ScriptStep
            {
                Description = $"Bước {_steps.Count + 1}",
                Action = ScriptActionType.Tap,
                TimeoutMs = 5000,
                DelayAfterMs = 1000,
                OnFail = OnFailBehavior.Stop
            };
            _steps.Add(newStep);
            RefreshStepNumbers();
            StepsListView.SelectedItem = newStep;
        }

        private void BtnDeleteStep_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep != null)
            {
                _steps.Remove(_currentStep);
                RefreshStepNumbers();
                _currentStep = null;
                StepDetailsPanel.IsEnabled = false;
            }
        }

        private void BtnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep == null) return;
            int index = _steps.IndexOf(_currentStep);
            if (index > 0)
            {
                _steps.Move(index, index - 1);
                RefreshStepNumbers();
            }
        }

        private void BtnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep == null) return;
            int index = _steps.IndexOf(_currentStep);
            if (index < _steps.Count - 1)
            {
                _steps.Move(index, index + 1);
                RefreshStepNumbers();
            }
        }

        #endregion

        #region Step Details

        private void StepsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentStep = StepsListView.SelectedItem as ScriptStep;
            if (_currentStep != null)
            {
                StepDetailsPanel.IsEnabled = true;
                LoadStepDetails(_currentStep);
            }
            else
            {
                StepDetailsPanel.IsEnabled = false;
            }
        }

        private void LoadStepDetails(ScriptStep step)
        {
            _isUpdatingUI = true;
            TxtDescription.Text = step.Description;
            TxtTimeout.Text = step.TimeoutMs.ToString();
            TxtDelayAfter.Text = step.DelayAfterMs.ToString();
            TxtRetryCount.Text = step.RetryCount.ToString();

            // Action ComboBox - tìm theo Tag
            SetComboBoxByTag(CmbAction, step.Action.ToString());
            
            // OCR Panel
            TxtTextToFind.Text = step.TextToFind ?? "";
            ChkExactMatch.IsChecked = step.ExactMatch;
            OcrPanel.Visibility = step.Action == ScriptActionType.TapText 
                ? Visibility.Visible : Visibility.Collapsed;

            // OnFail ComboBox
            CmbOnFail.SelectedIndex = step.OnFail switch
            {
                OnFailBehavior.Stop => 0,
                OnFailBehavior.RetryFromStart => 1,
                OnFailBehavior.RetryCurrentStep => 2,
                OnFailBehavior.SkipToNext => 3,
                _ => 0
            };

            RetryPanel.Visibility = step.OnFail == OnFailBehavior.RetryCurrentStep 
                ? Visibility.Visible : Visibility.Collapsed;

            // Load template preview
            var tplPath = Path.Combine(_templateDir, step.TemplateName ?? "");
            if (File.Exists(tplPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(tplPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    TemplatePreview.Source = bitmap;
                }
                catch { TemplatePreview.Source = null; }
            }
            else
            {
                TemplatePreview.Source = null;
            }

            _isUpdatingUI = false;
        }

        private void SetComboBoxByTag(ComboBox cmb, string tag)
        {
            for (int i = 0; i < cmb.Items.Count; i++)
            {
                if (cmb.Items[i] is ComboBoxItem item && item.Tag?.ToString() == tag)
                {
                    cmb.SelectedIndex = i;
                    return;
                }
            }
            cmb.SelectedIndex = 0;
        }

        private void CmbAction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUI || _currentStep == null) return;

            var selected = CmbAction.SelectedItem as ComboBoxItem;
            var tag = selected?.Tag?.ToString() ?? "Tap";
            
            if (Enum.TryParse<ScriptActionType>(tag, out var action))
            {
                _currentStep.Action = action;
            }

            // Show/hide OCR panel
            OcrPanel.Visibility = _currentStep.Action == ScriptActionType.TapText 
                ? Visibility.Visible : Visibility.Collapsed;

            StepsListView.Items.Refresh();
        }

        private void StepDetail_Changed(object sender, EventArgs e)
        {
            if (_isUpdatingUI || _currentStep == null) return;

            _currentStep.Description = TxtDescription.Text;
            if (int.TryParse(TxtTimeout.Text, out int timeout)) _currentStep.TimeoutMs = timeout;
            if (int.TryParse(TxtDelayAfter.Text, out int delay)) _currentStep.DelayAfterMs = delay;
            if (int.TryParse(TxtRetryCount.Text, out int retry)) _currentStep.RetryCount = retry;

            // OCR fields
            _currentStep.TextToFind = TxtTextToFind.Text;
            _currentStep.ExactMatch = ChkExactMatch.IsChecked == true;

            _currentStep.OnFail = CmbOnFail.SelectedIndex switch
            {
                0 => OnFailBehavior.Stop,
                1 => OnFailBehavior.RetryFromStart,
                2 => OnFailBehavior.RetryCurrentStep,
                3 => OnFailBehavior.SkipToNext,
                _ => OnFailBehavior.Stop
            };

            RetryPanel.Visibility = _currentStep.OnFail == OnFailBehavior.RetryCurrentStep 
                ? Visibility.Visible : Visibility.Collapsed;

            StepsListView.Items.Refresh();
        }

        private void BtnSelectTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep == null) return;

            var ofd = new OpenFileDialog
            {
                Filter = "Image files|*.png;*.jpg;*.bmp",
                Title = "Chọn ảnh mẫu",
                InitialDirectory = _templateDir
            };

            if (ofd.ShowDialog() == true)
            {
                // Copy to template dir if not already there
                var destPath = Path.Combine(_templateDir, Path.GetFileName(ofd.FileName));
                if (ofd.FileName != destPath && !File.Exists(destPath))
                {
                    File.Copy(ofd.FileName, destPath);
                }
                _currentStep.TemplateName = Path.GetFileName(ofd.FileName);
                LoadStepDetails(_currentStep);
                StepsListView.Items.Refresh();
            }
        }

        #endregion

        #region Drag & Drop

        private Point _dragStartPoint;

        private void StepsListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void StepsListView_Drop(object sender, DragEventArgs e)
        {
            // Basic drag-drop reorder
            if (e.Data.GetDataPresent(typeof(ScriptStep)))
            {
                var droppedData = e.Data.GetData(typeof(ScriptStep)) as ScriptStep;
                var target = ((FrameworkElement)e.OriginalSource).DataContext as ScriptStep;

                if (droppedData != null && target != null && droppedData != target)
                {
                    int oldIndex = _steps.IndexOf(droppedData);
                    int newIndex = _steps.IndexOf(target);
                    _steps.Move(oldIndex, newIndex);
                    RefreshStepNumbers();
                }
            }
        }

        #endregion

        #region Save/Cancel

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "JSON Script|*.json",
                Title = "Lưu kịch bản",
                FileName = ScriptNameBox.Text.Replace(" ", "_") + ".json",
                InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts")
            };

            if (!Directory.Exists(sfd.InitialDirectory))
                Directory.CreateDirectory(sfd.InitialDirectory);

            if (sfd.ShowDialog() == true)
            {
                var profile = new ScriptProfile
                {
                    Name = ScriptNameBox.Text,
                    Author = Environment.UserName,
                    Description = $"Kịch bản với {_steps.Count} bước",
                    ModifiedAt = DateTime.Now,
                    Steps = _steps.ToList()
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(profile, options);
                File.WriteAllText(sfd.FileName, json);

                MessageBox.Show($"Đã lưu kịch bản: {sfd.FileName}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                _currentFilePath = sfd.FileName;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion
    }
}
