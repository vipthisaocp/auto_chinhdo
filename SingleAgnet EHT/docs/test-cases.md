# 🧪 Test Cases - Task T003

## 📋 Functional Tests

| ID | Title | Prerequisites | Steps | Expected Result | Result |
|----|-------|---------------|-------|-----------------|--------|
| **TC-NAV-001** | Tự động thoát tab Nhiệm vụ | LDPlayer đang mở tab Nhiệm vụ | Chạy bot PK | Bot phát hiện và bấm về tab Lân cận trong < 1s | ✅ Passed |
| **TC-NAV-002** | Không click lặp khi ở đúng tab | LDPlayer đang mở tab Lân cận | Chạy bot PK | Bot bỏ qua logic chuyển tab, bắt đầu quét PK | ✅ Passed |
| **TC-NAV-003** | Khôi phục sau UI Lag | LDPlayer lag, click lần 1 không ăn | Chạy bot PK | Bot tiếp tục quét và click lại cho đến khi thoát tab | ✅ Passed |

## 🎨 UI/UX Tests

| ID | Title | Steps | Expected Result | Result |
|----|-------|-------|-----------------|--------|
| **TC-UI-001** | Micro-feedback log | Quan sát console log | Trace log hiện: "Phát hiện tab Nhiệm vụ -> Chuyển tab" | ✅ Passed |

---
> 🧪 *Mọi test case phải được đánh dấu ✅ TRƯỚC khi báo cáo hoàn thành.*
