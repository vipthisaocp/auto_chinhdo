# 📚 README - AI Agent Entry Point

> 🤖 **File khởi động cho AI Agent**
>
> ⚡ **RESTART-SAFE WORKFLOW v3.0**: Mọi thứ được lưu trong files, restart không mất dữ liệu
>
> 📋 **DOCUMENTATION-DRIVEN**: Phân tích kỹ, document trước, implement chuẩn

---

## 🚀 QUICK START

### Khi Bắt Đầu Session Mới (hoặc sau Restart)

```
1. Đọc context.md     → Hiểu trạng thái dự án & phase hiện tại
2. Đọc docs/*         → Hiểu specifications (nếu đã có)
3. Đọc task-queue.md  → Biết task tiếp theo (nếu có)
4. Báo user tóm tắt   → Xác nhận trước khi làm
5. Làm 1 task         → Theo đúng specs
6. Cập nhật files     → Lưu progress + update docs nếu cần
```

### Lệnh Khởi Động Chuẩn

User nói: **"Đọc context.md và tiếp tục"**

AI sẽ:
1. Đọc `context.md`
2. Đọc `docs/*` để có full specs
3. Báo: "Dự án [X] đang ở phase [Y]. Task tiếp theo là [Z]. Tiếp tục không?"
4. Đợi user confirm rồi mới làm

---

## 📁 CẤU TRÚC FILES

```
📁 Project/
│
├── 📄 readme.md        ← BẠN ĐANG Ở ĐÂY (Entry point)
│
├── 🔴 CORE STATE (Quan trọng nhất)
│   ├── context.md      ← Trạng thái hiện tại của dự án
│   └── task-queue.md   ← Danh sách tasks cần làm
│
├── 📘 BA SPECIFICATIONS (Phase 0)
│   └── docs/
│       ├── prd.md              ← Product Requirements
│       ├── user-stories.md     ← User Stories + Acceptance Criteria
│       ├── data-model.md       ← Database Design (ERD, Tables, Columns)
│       ├── ui-specs.md         ← UI Layout, Components, Actions
│       └── api-specs.md        ← API Endpoints, Request/Response
│
├── 📗 PROJECT DOCS
│   ├── about.md        ← Thông tin dự án
│   ├── project-plan.md ← Kế hoạch kỹ thuật (tạo Phase 1)
│   └── system.md       ← Profile năng lực AI
│
├── 📙 PROCESS DOCS
│   ├── workflow.md     ← Quy trình 4 phases
│   ├── thinking.md     ← Framework tư duy
│   └── quality.md      ← Checklist chất lượng
│
└── 📁 src/             ← Source code (tạo Phase 2)
```

---

## 🎯 WORKFLOW: 4 PHASES

```
┌─────────────────────────────────────────────────────────────────┐
│                    WORKFLOW v3.0 (4 PHASES)                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   ┌───────────┐   ┌───────────┐   ┌───────────┐   ┌──────────┐ │
│   │ DISCOVERY │──▶│ PLANNING  │──▶│ EXECUTING │──▶│ VERIFY   │ │
│   │ Phase 0   │   │ Phase 1   │   │ Phase 2   │   │ Phase 3  │ │
│   └───────────┘   └───────────┘   └───────────┘   └──────────┘ │
│        │               │               │               │        │
│        ▼               ▼               ▼               ▼        │
│   docs/*.md      project-plan    src/*          walkthrough    │
│   (BA specs)     task-queue      (code)         (summary)      │
│                                                                  │
│   ✅ Phân tích kỹ trước khi code                                │
│   ✅ Implementation theo specs                                   │
│   ✅ Restart-safe, không mất dữ liệu                            │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📋 WORKFLOW CHI TIẾT

### Phase 0: DISCOVERY & ANALYSIS 🔍

**Trigger**: User nói "Tôi muốn làm [dự án]"

```
AI sẽ:
1. Hỏi clarifying questions
2. Tạo docs/prd.md (Product Requirements)
3. Tạo docs/user-stories.md (với Acceptance Criteria)
4. Tạo docs/data-model.md (ERD, Tables, Columns chi tiết)
5. Tạo docs/ui-specs.md (Wireframes, Components, Actions)
6. Tạo docs/api-specs.md (Endpoints, Request/Response)
7. Hỏi user approve từng doc
```

### Phase 1: PLANNING 📝

**Trigger**: Sau khi Discovery docs được approve

```
AI sẽ:
1. Đọc tất cả docs/*
2. Tạo project-plan.md (Tech stack, Architecture)
3. Tạo task-queue.md (Tasks reference đến docs)
4. Cập nhật context.md (Phase: PLANNING → EXECUTING)
5. Hỏi user approve plan
```

### Phase 2: EXECUTING 🔨

**Trigger**: User nói "Tiếp tục" hoặc "Làm task tiếp theo"

```
AI sẽ:
1. Đọc task-queue.md → Lấy task tiếp theo
2. Đọc related docs (User Story, Data Model, UI, API specs)
3. Implement theo đúng specs
4. Update docs nếu có thay đổi
5. Đánh dấu task done trong task-queue.md
6. Cập nhật context.md
```

### Phase 3: VERIFICATION ✅

**Trigger**: Tất cả tasks done

```
AI sẽ:
1. Verify all Acceptance Criteria passed
2. Ensure docs accurate với code
3. Tạo summary/walkthrough
4. Cập nhật context.md (Phase: COMPLETED)
```

---

## 💡 COMMANDS THƯỜNG DÙNG

| User nói | AI làm gì |
|----------|-----------|
| "Đọc context.md và tiếp tục" | Khôi phục state, làm task tiếp theo |
| "Tôi muốn làm [dự án]" | Bắt đầu Phase 0: Discovery |
| "Status" | Đọc context.md, báo tóm tắt |
| "Làm task tiếp theo" | Lấy task từ queue, implement |
| "Review docs" | Xem lại tài liệu phân tích |
| "Tạm dừng" | Cập nhật context.md (Phase: PAUSED) |
| "Danh sách tasks" | Đọc task-queue.md, liệt kê |

---

## ⚠️ QUY TẮC QUAN TRỌNG

### 1. Phase 0 Phải Kỹ Lưỡng
- Mỗi table có đủ columns + types
- Mỗi page có wireframe + actions  
- Mỗi API có full request/response
- User approve docs trước khi code

### 2. Implement Theo Specs
- Đọc User Story + AC trước khi code
- Follow Data Model (columns, types)
- Match UI Specs (layout, components)
- Match API Specs (format, validation)

### 3. Sync Docs Với Code
- Code thay đổi? → Docs phải update!
- Thêm column → Update data-model.md
- Thêm API → Update api-specs.md
- Thay đổi UI → Update ui-specs.md

### 4. Mỗi Session = 1-2 Tasks Max
- Tránh làm quá nhiều → Lag, bối rối
- Làm xong task → Cập nhật files → Báo user

### 5. Restart = Không Mất Gì
- Tất cả state trong files
- User chỉ cần nói: "Đọc context.md và tiếp tục"

---

## 📘 TÀI LIỆU THAM KHẢO

| File | Khi nào đọc |
|------|-------------|
| [workflow.md](workflow.md) | Chi tiết quy trình 4 phases |
| [docs/*-template.md](docs/) | Templates cho BA docs |
| [system.md](system.md) | Cần biết phong cách làm việc |
| [thinking.md](thinking.md) | Gặp vấn đề phức tạp |
| [quality.md](quality.md) | Trước khi báo task done |

---

> 🌟 *Bắt đầu bằng cách đọc `context.md` để hiểu trạng thái dự án!*
