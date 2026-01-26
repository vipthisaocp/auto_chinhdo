# 📋 Task Queue - Auto Chinh Đồ

> **Status Legend**: ⏳ Pending | 🔄 In Progress | ✅ Done | ❌ Blocked

---

## Milestone 1: Core Systems (Done)
*Các hệ thống nền tảng đã hoàn thành và ổn định*

### T001: Firebase REST API Migration
**Status**: ✅ Done  
**Description**: Chuyển đổi từ Admin SDK sang REST API để bảo mật và loại bỏ private key.

### T002: GitHub & Deployment Setup
**Status**: ✅ Done  
**Description**: Đẩy code lên GitHub branch develop và thiết lập thư mục ReadyToUse.

---

## Milestone 2: PK & Navigation Optimization (Current)
*Tối ưu hóa khả năng nhận diện và điều hướng*

### T003: Fix Tab Nhiệm Vụ (Auto-Switch)
**Status**: ✅ Done  
**User Story**: US-PK-001  
**Description**: Nếu thấy tab Nhiệm Vụ (`nhiemvu.png`) -> Bấm nút Lân Cận (`lancan.png`) để về tab PK.
**Acceptance Criteria**:
- Bot không bị kẹt ở tab nhiệm vụ.
- Tốc độ nhận diện < 500ms.
- Không bấm nhầm khi đang ở đúng tab.

### T004: ROI Optimization for HP & Names
**Status**: ⏳ Pending  
**Description**: Tinh chỉnh ROI quét máu và tên để tránh quét nhầm button UI khác.

---

## Milestone 3: Săn Boss & Combat AI
*Nâng cấp logic ưu tiên mục tiêu*

### T005: Boss Priority & Tab Rotation
**Status**: ⏳ Pending  
**Description**: Tinh chỉnh vòng lặp 5s/10s để tối ưu giữa việc Săn Boss và Thám thính PK.

---

## Summary

| Milestone | Tasks | Status |
|-----------|-------|--------|
| M1: Core Systems | T001-T002 | ✅ Done |
| M2: PK & Nav | T003-T004 | 🔄 In Progress (T003 Done) |
| M3: Combat AI | T005 | ⏳ Pending |

**Total Tasks**: 5  
**Completed**: 3  
**Progress**: 60%
