# 📖 User Stories - Auto Chinh Đồ

## Epic: PK & Navigation

### US-PK-001: Thoát tab Nhiệm vụ tự động
**As a** bot  
**I want to** nhận diện khi đang ở tab Nhiệm vụ  
**So that** I can bấm chuyển sang tab Người chơi để PK.

**Acceptance Criteria:**
- AC1: Nhận diện `nhiemvu.png` với độ chính xác > 0.85.
- AC2: Bấm vào tọa độ của `lancan.png` khi phát hiện tab nhiệm vụ.
- AC3: Không thực hiện lặp lại nếu đã chuyển tab thành công.

### US-PK-002: Ưu tiên mục tiêu
**As a** bot  
**I want to** ưu tiên PK người chơi trước khi săn Boss  
**So that** bảo vệ bản thân và giành lợi thế.

**Acceptance Criteria:**
- AC1: Nếu thấy tên Tím/Máu đỏ người chơi -> Chuyển trạng thái PK ngay.
- AC2: Nếu không thấy người chơi sau 10s mới chuyển sang tìm Boss.
