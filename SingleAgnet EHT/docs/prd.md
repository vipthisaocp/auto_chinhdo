# 📋 Product Requirements Document (PRD) - Auto Chinh Đồ

> **Project**: Auto Chinh Đồ  
> **Version**: 1.2  
> **Date**: 2026-01-23

---

## 1. Problem Statement

Chơi game Chinh Đồ trên LDPlayer yêu cầu PK người chơi và săn Boss liên tục. Vấn đề lớn nhất là:
- Bot dễ bị kẹt ở tab Nhiệm vụ khi thực hiện các thao tác chuyển tab.
- Việc nhận diện kẻ địch cần độ chính xác cao để không PK nhầm hoặc bỏ lỡ mục tiêu.
- Quản lý thiết bị và bản quyền cần an toàn, không lộ private key.

---

## 2. Goals

| Goal | Metric |
|------|--------|
| Tự động chuyển tab thông minh | 100% thoát tab Nhiệm vụ khi cần PK |
| Bảo mật License | Không sử dụng File JSON Admin Key trực tiếp trên client |
| PK linh hoạt | Ưu tiên Người chơi > Boss > Quái |

---

## 3. Core Features

### F1: Firebase REST Auth
- Đăng nhập bằng Email/Username qua REST API.
- Kiểm tra License và giới hạn thiết bị (HWID).

### F2: State Machine PK (V36)
- 7 trạng thái: INIT, SCAN, PK, FOLLOW, FIND_BOSS, FIGHT_BOSS, SCOUT.
- Nhận diện Vital Signs (Máu đỏ, Tên tím/vàng).

### F3: Smart Navigation
- Nhận diện `nhiemvu.png` để biết đang bị kẹt.
- Bấm `lancan.png` để chuyển sang tab Người chơi/PK.

---

## 4. Technical Constraints
- **Framework**: .NET 9, WPF, MVVM.
- **Image Lib**: OpenCvSharp4.
- **Communication**: ADB (SharpAdbClient).
