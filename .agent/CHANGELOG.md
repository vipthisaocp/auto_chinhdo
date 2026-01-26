# 📝 Development Changelog - Auto LDPlayer

## 2026-01-23

### 🔄 ĐANG LÀM DỞ - Sẽ tiếp tục lúc 14h
- **Vấn đề**: Bot bấm nhầm vào vị trí nhiệm vụ khi ở tab Nhiệm vụ
- **Giải pháp**: Thêm logic kiểm tra `nhiemvu.png` → bấm `lancan.png` để chuyển sang tab Người chơi
- **File cần sửa**: `Services/PkHuntService.cs` - dòng 147 (case BotState.INIT)
- **Template có sẵn**: `nhiemvu.png`, `lancan.png` trong `templates/pk_shared/`

### 🔐 Firebase Client SDK Migration - HOÀN THÀNH ✅
- **Thời gian**: 09:00 - 11:10
- **Thay đổi**: Chuyển hoàn toàn sang Firebase REST API
- **Files**: `Services/FirebaseService.cs` (viết lại 100%)
- **Chi tiết**:
  - ❌ Loại bỏ dependency `firebase-admin-key.json`
  - ✅ Firebase Auth REST API cho login (email có @)
  - ✅ Firestore REST API cho license/device check
  - ✅ Fallback Firestore cho username không có @
  - ✅ Web API Key: `AIzaSyAz0_o_MrC8X9dX9zARQdhAMAgPLdpbpX4`

### ⚔️ PK Logic Status
- **File chính**: `Services/PkHuntService.cs`
- **Version**: V36 State Machine
- **7 States**: INIT → SCAN_PLAYER → PK → FOLLOW → FIND_BOSS → FIGHT_BOSS → SCOUT_PK
- **Tính năng**:
  - ✅ Vital Signs Detection (HP đỏ + Tên vàng/tím)
  - ✅ Đọc ROI từ `hp_bar_config.json`
  - ✅ Logic bấm nút "Lân cận" khởi tạo tab
  - ✅ Hybrid PK + Grind (treo máy khi không có địch)
  - ✅ Boss Hunter (ưu tiên Player > Boss > Grind)

---

## 2026-01-22

### 🔧 PK Hunt V2 (Vital Signs Detection)
- **Thời gian**: 14:00 - 15:30
- **Files**: `Services/PkHuntServiceV2.cs` (đã merge vào PkHuntService.cs)
- **Chi tiết**:
  - Tạo PkHuntServiceV2 với Vital Signs Detection
  - Đọc ROI từ hp_bar_config.json
  - Mở rộng ROI lên 25px để bao gồm tên
  - Thêm logic InitializeTab bấm nút Lân cận

---

## Lưu ý sử dụng

Mỗi khi code xong một feature, tôi sẽ cập nhật file này với:
- Thời gian
- Files thay đổi
- Chi tiết công việc
- Trạng thái (✅ Hoàn thành, 🔄 Đang làm, ❌ Hủy bỏ)
