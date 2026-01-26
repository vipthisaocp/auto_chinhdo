# 📦 Modules Directory

> 🗂️ **Thư mục chứa các module của dự án**

---

## 📋 Workflow

### Tech Lead tạo module mới

```bash
1. Copy folder _template/ thành module-[name]/
2. Điền thông tin vào readme.md (Work Order)
3. Thông báo User để giao cho Worker
```

### User giao việc cho Worker

```bash
1. Mở VSCode/Cursor MỚI
2. File → Open Folder → chọn module-[name]/
3. Agent tự động đọc readme.md và bắt đầu làm
4. Khi xong, Agent cập nhật status.md
5. User đóng window và quay lại Tech Lead
```

### Tech Lead review

```bash
1. User: "Review module-[name]"
2. Tech Lead đọc code trong folder
3. Viết review.md hoặc approve
```

---

## 📊 Modules Overview

| # | Module | Priority | Status | Notes |
|---|--------|----------|--------|-------|
| - | _template | - | 📋 Template | Không sử dụng trực tiếp |

---

## 📁 Template Structure

```
_template/
├── readme.md    ← Work Order template (Tech Lead điền)
└── status.md    ← Status template (Worker cập nhật)
```

---

## 🔄 Status Legend

| Icon | Status |
|------|--------|
| ⏳ | Pending - Chưa giao |
| 🔄 | In Progress - Đang làm |
| 📤 | Submitted - Đã submit |
| 🔍 | In Review - Đang review |
| 📝 | Changes Requested - Cần sửa |
| ✅ | Approved - Đã duyệt |

---

> 🔄 *Cập nhật bảng Overview khi có module mới*
