# 🔄 WORKFLOW - SingleAgent EHT v5.0 (Stateful & Professional)

> 📅 **Version**: 5.0 | **Updated**: 2026-01-23
> 🎯 **Philosophy**: **Logic-First → Security-Embedded → Test-Driven → Visual-Verification**

---

## 🌟 3 RÔLES - 1 MISSION (Internal Discussion Result)

### 🎯 BA (Business Analyst) - "Người giữ mục tiêu"
- **Nhiệm vụ**: Chuyển hóa ý tưởng người dùng thành các **User Stories** và **Acceptance Criteria (AC)**.
- **Tiêu chuẩn**: Mọi task phải trả lời được câu hỏi: *"Người dùng được lợi gì? Làm sao để biết task đã thành công?"*

### 🛠️ Tech Lead (Kiến trúc sư) - "Người xây nền tảng"
- **Nhiệm vụ**: Thiết kế **Logic Flow (State Machine)**, **Vision Spec (ROI/Threshold)** và **Security Architecture**.
- **Tiêu chuẩn**: Code phải sạch (Clean Code), hiệu suất cao (CPU/Ram) và bảo mật (License protection).

### 🧪 Tester Lead (Chuyên gia chất lượng) - "Người gác cổng"
- **Nhiệm vụ**: Thiết kế **Test Cases (TC)** trước khi code và kiểm tra **Verification Evidence**.
- **Tiêu chuẩn**: Không có lỗi logic, giao diện phải "WOW" (Premium UI/UX) và mọi tính năng đều có bằng chứng kiểm thử.

---

## 📋 QUY TRÌNH 4 GIAI ĐOẠN (Phối hợp đa vai trò)

### Phase 0: DISCOVERY (Phân tích & Thiết kế Logic) 🔍
*Đây là giai đoạn quan trọng nhất để tránh sai lầm.*
1.  **BA**: Tạo `prd.md` và `user-stories.md`.
2.  **Tech Lead**: Tạo `logic-flow.md` (Sơ đồ Mermaid) + `vision-spec.md` (ROI/Threshold).
3.  **Tester Lead**: Tạo `test-cases.md` dựa trên AC của BA.
4.  **Security Check**: Tech Lead xác định các điểm nhạy cảm cần bảo vệ.

### Phase 1: PLANNING (Lập kế hoạch thực thi) 📝
1.  **Tech Lead**: Tạo `task-queue.md` - Chia nhỏ task < 2 giờ làm việc.
2.  **Mapping**: Mỗi task phải link tới: [User Story ID] + [Logic Node] + [Test Case ID].
3.  **Approve**: Chờ User (Người dùng) xem qua "Bản đồ công việc".

### Phase 2: EXECUTING (Thực thi & Test nhanh) 🔨
1.  **Code**: Lập trình viên viết code theo Specs.
2.  **Unit Test**: Chạy ngay Test Case tương ứng.
3.  **Documentation**: Cập nhật `CHANGELOG.md` và `context.md` sau mỗi task hoàn thành.

### Phase 3: VERIFICATION (Nghiệm thu & Đóng gói) ✅
1.  **Final Test**: Chạy toàn bộ bộ test regression.
2.  **UI/UX Polish**: Kiểm tra micro-animations và thẩm mỹ giao diện.
3.  **Walkthrough**: Tạo `walkthrough.md` kèm theo **Ảnh/Video** làm bằng chứng (Evidence).
4.  **Release**: Đóng gói vào thư mục `ReadyToUse/` cho người dùng.

---

## 📁 CẤU TRÚC THƯ MỤC CHUẨN

```
📁 SingleAgnet EHT/
├── 📘 docs/ (Tài liệu BA & Specs)
│   ├── prd.md
│   ├── user-stories.md
│   ├── logic-flow.md (MỚI: Sơ đồ Mermaid)
│   └── vision-spec.md (MỚI: Tọa độ & Độ nhạy)
├── 📗 quality/ (Kiểm soát chất lượng)
│   ├── test-cases.md
│   └── walkthroughs/ (Bằng chứng nghiệm thu)
├── 🔴 state/ (Trạng thái dự án)
│   ├── context.md
│   └── task-queue.md
└── 🏗️ technical/ (Kỹ thuật & Kế hoạch)
    ├── project-plan.md
    └── architecture.md
```

---

## ⚡ NGUYÊN TẮC "VÀNG" CỦA PHÒNG IT EHT
1.  **Không có Sơ đồ Logic = Không Code.**
2.  **Không có Test Case = Không Executing.**
3.  **Không có Ảnh/Video Bằng chứng = Chưa hoàn thành.**
4.  **Bảo mật License là ưu tiên số 1.**

---
> 🚀 *Quy trình v5.0 được thiết kế để xây dựng những sản phẩm Auto-Game đẳng cấp nhất thế giới.*
