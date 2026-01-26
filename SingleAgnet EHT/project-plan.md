# 🏗️ Project Plan - PK Navigation Fix

> **Task**: T003 - Fix Tab Nhiệm Vụ (Auto-Switch)
> **Goal**: Đảm bảo bot luôn ở tab "Người chơi" (Lân cận) để thực hiện PK, tránh bị kẹt tại tab "Nhiệm vụ".

## 🛠️ Tech Stack & Architecture
- **Language**: C# (.NET 9)
- **Framework**: WPF (MVVM)
- **Vision**: OpenCvSharp4 (Template Matching)
- **Control**: ADB (Android Debug Bridge)

## 📁 Folder Structure Impact
- `Services/PkHuntService.cs`: Sửa logic State Machine.
- `templates/pk_shared/`: Nơi chứa `nhiemvu.png` và `lancan.png`.

## ⚙️ Implementation Strategy
1. **Constant Definition**:
   - Định nghĩa đường dẫn template `nhiemvu.png` và `lancan.png`.
2. **State Machine Injection**:
   - Chèn logic kiểm tra tab trong `BotState.INIT` của `PkHuntService.cs`.
   - Sử dụng `FindTemplateAsync` để quét `nhiemvu.png`.
3. **Action Execution**:
   - Nếu phát hiện `nhiemvu.png`, thực hiện `adb shell input tap` vào tọa độ của nút Lân Cận.
   - Thêm `Task.Delay` (300-500ms) để UI LDPlayer cập nhật.

## 📅 Milestones
- [ ] Milestone 1: Define templates & thresholds (Confidence: 0.85).
- [ ] Milestone 2: Inject image detection in INIT state.
- [ ] Milestone 3: Implement click action & delay.
- [ ] Milestone 4: Verification & Stress Test.

## 🧪 Verification Plan
### Manual Tests
1. Mở LDPlayer, vào game, mở tab "Nhiệm vụ".
2. Khởi chạy bot PK.
3. **Kỳ vọng**: Bot phát hiện tab "Nhiệm vụ", click nút "Lân cận", sau đó bắt đầu quét người chơi.

### Edge Cases
- Đang ở tab "Người chơi": Bot không được bấm lại nút "Lân cận" (tránh lặp vô ích).
- Template `nhiemvu.png` bị che: Bot vẫn phải tiếp tục logic PK nếu có thể.
