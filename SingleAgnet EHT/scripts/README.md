# 📜 Automation Scripts

Các script PowerShell để tự động hóa workflow Multi-Agent.

---

## 🔍 watcher.ps1 - Status Watcher

**Chạy nền để theo dõi trạng thái modules:**

```powershell
# Chạy với cài đặt mặc định
.\scripts\watcher.ps1

# Tùy chỉnh interval (poll mỗi 3 giây)
.\scripts\watcher.ps1 -PollIntervalSeconds 3

# Bật Windows Toast notifications
.\scripts\watcher.ps1 -ShowToast
```

**Output:**
- Tự động cập nhật `notifications.md` khi có status changes
- Tự động cập nhật `dashboard.md` với trạng thái tất cả modules
- Console log các thay đổi realtime

---

## 📊 dashboard-live.ps1 - Live Dashboard

**Hiển thị dashboard trực quan trong terminal:**

```powershell
# Chạy dashboard
.\scripts\dashboard-live.ps1

# Refresh nhanh hơn (mỗi 1 giây)
.\scripts\dashboard-live.ps1 -RefreshSeconds 1
```

**Features:**
- Bảng đẹp với màu sắc theo status
- Progress bar cho từng module
- Summary tổng hợp

---

## ⚡ check-status.ps1 - Quick Check

**Kiểm tra nhanh một lần (không loop):**

```powershell
# Xem trạng thái
.\scripts\check-status.ps1

# Output JSON (cho scripting)
.\scripts\check-status.ps1 -Json
```

---

## 📦 create-module.ps1 - Create Module

**Tạo module mới từ template:**

```powershell
# Tạo module đơn giản
.\scripts\create-module.ps1 -Name "auth"

# Tạo với title và description
.\scripts\create-module.ps1 -Name "auth" -Title "Authentication Module" -Description "Handle user login/logout"
```

---

## 🚀 Workflow Được Đề Xuất

### Terminal 1 - Watcher (chạy nền)
```powershell
cd D:\Code\AntiGravity\Code\MultiAgentTemplate
.\scripts\watcher.ps1
```

### Terminal 2 - Dashboard (optional, xem trạng thái)
```powershell
cd D:\Code\AntiGravity\Code\MultiAgentTemplate
.\scripts\dashboard-live.ps1
```

### Trong IDE (Tech Lead)
```
# Khi cần biết modules nào xong, đọc:
- notifications.md  ← Các thay đổi gần đây
- dashboard.md      ← Trạng thái tổng quan
```

---

## 📝 Status Keywords

Scripts nhận diện các keywords sau trong `status.md`:

| Status | Keywords |
|--------|----------|
| ✅ COMPLETED | `COMPLETED`, `DONE`, `FINISHED` |
| 🔄 IN_PROGRESS | `IN_PROGRESS`, `WORKING`, `IMPLEMENTING` |
| ⏳ PENDING | `PENDING`, `TODO`, `NOT_STARTED` |
| 🚫 BLOCKED | `BLOCKED`, `ERROR`, `FAILED` |
| 👀 NEEDS_REVIEW | `REVIEW`, `NEEDS_REVIEW` |

**Ví dụ trong status.md:**
```markdown
## Current Status
**Status**: IN_PROGRESS (50%)
```
