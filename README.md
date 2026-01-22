<<<<<<< HEAD
# auto_chinhdo
auto_chinhdo
=======
# 🎮 Auto Chinh Đồ

Công cụ tự động hóa game Chinh Đồ Mobile trên LDPlayer. Xây dựng bằng **WPF (.NET 8)**, **OpenCV**, và **ADB**.

---

## ✨ Tính Năng

### 📜 Scripting Engine
- Visual Script Editor
- Xử lý lỗi: Stop, Retry, Skip
- Lưu/Tải file JSON

### ✂️ Template Matching (Khuyên dùng cho game)
- **Tách nền tự động**: Xóa background, giữ chữ/icon
- **Phù hợp với mọi font game**

### 🔤 OCR (Cho app thường)
- Tesseract engine
- Nút 🔍 OCR Debug xem text

> ⚠️ **Lưu ý**: OCR không nhận diện được font game. **Hãy dùng Template Matching (Tap)** thay vì TapText cho game.

---

## 🚀 Hướng Dẫn Nhanh

### Tạo script cho game:
1. Chụp màn hình → **"Cắt ảnh"**
2. Tick **"✂️ Tách nền"** → Cắt vùng chữ/nút
3. Mở **"✏️ Tạo Script"**
4. Thêm bước → Chọn action **"Tap"**
5. Chọn ảnh mẫu vừa cắt
6. Lưu và chạy

---

## 📝 Changelog

### v2.2.0
- ✅ OCR TapText + Debug
- ✅ Kết luận: Template Matching tốt hơn cho game

### v2.1.0
- ✅ Tách nền tự động

### v2.0.0
- ✅ Visual Script Editor

---
*Developed with ❤️ - 2025*
>>>>>>> cd9683c (Initial commit - Auto Chinh Do v1.0 (clean))
