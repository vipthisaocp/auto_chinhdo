# 📋 [TÊN MODULE] - Work Order

> 🔧 **Work Order cho Worker Agent**
> 
> ⚠️ **WORKER: Đọc file này TRƯỚC khi làm bất cứ điều gì!**

---

## 📌 Thông tin cơ bản

| Field | Value |
|-------|-------|
| **Module** | [Tên module] |
| **Priority** | 🔴 High / 🟡 Medium / 🟢 Low |
| **Created** | [YYYY-MM-DD] |
| **Status** | ⏳ Pending |

---

## 1. 🎯 Mục tiêu

[Mô tả ngắn gọn module này cần làm gì]

---

## 2. 📋 Requirements

### Chức năng bắt buộc

```
[REQ-01] [Mô tả requirement 1]
[REQ-02] [Mô tả requirement 2]
[REQ-03] [Mô tả requirement 3]
```

### Constraints

- [Giới hạn về tech stack]
- [Giới hạn về thời gian/scope]
- [Quy tắc naming, coding style]

---

## 3. 🔧 Technical Specs

### Tech Stack

| Layer | Technology |
|-------|------------|
| Language | [C# / JavaScript / ...] |
| Framework | [.NET 8 / React / ...] |

### File Structure mong đợi

```
src/
├── [folder1]/
│   └── [file1.cs]
├── [folder2]/
│   └── [file2.cs]
└── ...
```

### Interfaces/Contracts

```csharp
// Interface mà module này cần implement
[Code example nếu cần]
```

---

## 4. ✅ Definition of Done

Khi nào coi là HOÀN THÀNH:

```
[ ] [Criteria 1]
[ ] [Criteria 2]
[ ] [Criteria 3]
[ ] Code build thành công
[ ] Đã test các happy paths
[ ] Đã cập nhật status.md
```

---

## 5. 📚 References

### Tài liệu liên quan
- [Link hoặc path tới docs]

### Code tham khảo
- [Link hoặc path tới code mẫu]

---

## 6. ⚠️ Rules

### ✅ DO
- Implement code trong folder `src/`
- Cập nhật `status.md` khi hoàn thành
- Comment code phức tạp
- Handle errors properly

### ❌ DON'T
- KHÔNG sửa file ngoài folder này
- KHÔNG thay đổi requirements
- KHÔNG skip testing
- KHÔNG để hardcoded values

---

## 📤 Khi hoàn thành

1. Cập nhật `status.md` với:
   - Status: ✅ Completed
   - List files đã tạo
   - Known issues (nếu có)

2. User sẽ thông báo cho Tech Lead để review

---

> 📞 **Questions?** Nếu có bất kỳ điểm nào chưa rõ, hãy hỏi User TRƯỚC khi implement.
