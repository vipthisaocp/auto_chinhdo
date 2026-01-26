# 📋 PROJECT CONTEXT

> 🔄 **File duy trì trạng thái dự án qua các phiên làm việc**
> 
> ⚠️ **QUAN TRỌNG**: AI Agent PHẢI đọc file này đầu tiên khi bắt đầu session mới
>
> 📅 **Last Updated**: [Chưa bắt đầu]

---

## 🎯 Dự Án

**Tên**: Auto Chinh Đồ - LDPlayer Edition  
**Mô tả**: Automation PK, Boss, Train for Chinh Do Mobile  
**Trạng thái**: DISCOVERY (v5.0 - Task T003)

---

## 📌 CURRENT STATE (AI đọc phần này để biết đang ở đâu)

### Phase: `VERIFICATION (v5.0)`

### Current Task: `Fix Tab Nhiệm vụ vs Tab Người chơi`

### What Was Done Last Session:
```
- Viết lại hoàn toàn FirebaseService sang REST API (Loại bỏ admin key).
- Đã build và deploy bản Release chính thức vào ReadyToUse.
- Đã Push toàn bộ code lên GitHub branch develop.
- Đã thiết lập file CHANGELOG.md để theo dõi lịch sử.
```

### What To Do Next:
```
1. Sửa logic trong PkHuntService.cs: Nhận diện nhiemvu.png và bấm lancan.png.
2. Tối ưu hóa tốc độ quét tab để không bám nhầm vào button nhiệm vụ.
3. Test thực tế logic săn Boss + PK mới.
```

---

## 📊 PROGRESS SUMMARY

| Metric | Value |
|--------|-------|
| Total Tasks | 0 |
| Completed | 0 |
| In Progress | 0 |
| Remaining | 0 |
| Progress | 0% |

### 🧪 Test Cases Status

| Category | Total | Passed | Failed |
|----------|-------|--------|--------|
| Functional | 0 | 0 | 0 |
| UI/UX | 0 | 0 | 0 |
| API | 0 | 0 | 0 |
| Security | 0 | 0 | 0 |

### 🎯 3 Tiêu Chí Chất Lượng

| Tiêu Chí | Status |
|----------|--------|
| ✅ Đúng đủ yêu cầu | ⬜ Chưa verify |
| 🎨 Giao diện đẹp, dễ dùng | ⬜ Chưa verify |
| 🔒 Bảo mật code tốt | ⬜ Chưa verify |

---

## 📁 BA Specifications (Phase 0)

| Document | Vai trò | Trạng thái | Notes |
|----------|---------|------------|-------|
| `docs/prd.md` | BA | ✅ Done | Product Requirements |
| `docs/user-stories.md` | BA | ✅ Done | 12 User Stories + AC |
| `docs/data-model.md` | Tech Lead | ✅ Done | 10 Entities |
| `docs/ui-specs.md` | Tech Lead | ✅ Done | Wireframes + Design |
| `docs/api-specs.md` | Tech Lead | ⏳ N/A | Desktop app, no API |
| `docs/test-cases.md` | Tester Lead | ✅ Done | 31 Test Cases |

---

## 🏗️ Kiến Trúc

```
[Sẽ được điền khi planning - Phase 1]
```

---

## 📁 Project Files

| File | Mục đích | Trạng thái |
|------|----------|------------|
| `project-plan.md` | Kế hoạch kỹ thuật | ⏳ Chưa tạo |
| `task-queue.md` | Danh sách tasks | ⏳ Chưa tạo |
| `src/` | Source code | ⏳ Chưa tạo |

---

## ⚠️ BLOCKERS & ISSUES

```
[Không có issues]
```

---

## 📝 SESSION LOG

| 1 | 2026-01-23 | DISCOVERY | Các vai trò thảo luận & Thiết lập Quy trình v5.0 | Hoàn thành bộ khung |
| 2 | 2026-01-23 | DISCOVERY | Phân tích Logic Flow cho Task T003 | Hoàn thành Specs |
| 3 | 2026-01-23 | EXECUTING | Triển khai logic PkHuntService.cs | Hoàn thành Code |
| 4 | 2026-01-23 | VERIFICATION | Kiểm tra logic & Cập nhật tài liệu | Đang thực hiện |

---

## 🔄 RESTART INSTRUCTIONS

Khi bắt đầu session mới, AI Agent nên:

1. **Đọc file này** (`context.md`) để hiểu trạng thái hiện tại
2. **Đọc `docs/*`** (nếu có) để hiểu specifications
3. **Đọc `task-queue.md`** (nếu có) để biết task tiếp theo
4. **Báo user**: "Tôi đã đọc context. [Tóm tắt trạng thái]. Tiếp tục không?"
5. **Làm 1 task** rồi cập nhật lại file này

---

> 🔄 *File này được cập nhật sau MỖI task hoàn thành*
