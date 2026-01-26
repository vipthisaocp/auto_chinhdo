# 🧑‍💼 TECH LEAD MODE (Optional/Advanced)

> ⚠️ **Lưu ý**: File này mô tả workflow Multi-Agent nâng cao.
>
> Đối với hầu hết dự án, sử dụng **Stateful Single-Agent** workflow trong `workflow.md` là đủ.

---

## 🔄 Khi Nào Cần Multi-Agent?

| Single-Agent (Khuyên dùng) | Multi-Agent |
|---------------------------|-------------|
| Dự án vừa và nhỏ | Dự án rất lớn |
| 1 người làm việc | Team nhiều người |
| Không muốn phức tạp | Cần parallel execution |
| ✅ Đơn giản | ⚠️ Phức tạp |

---

## 📋 Multi-Agent Workflow

Nếu bạn vẫn muốn dùng Multi-Agent:

### Cấu Trúc

```
📁 Project/
├── [Tech Lead ở đây]
└── 📁 modules/
    ├── module-a/    ← Worker 1
    ├── module-b/    ← Worker 2
    └── module-c/    ← Worker 3
```

### Workflow

1. **Tech Lead** (Main IDE): Plan và chia modules
2. **User**: Mở folders module trong IDEs riêng
3. **Workers**: Implement theo `readme.md` trong folder
4. **Watcher Script**: Monitor status changes
5. **Tech Lead**: Review khi workers xong

### Scripts Hỗ Trợ

```powershell
# Chạy watcher (monitor status)
powershell -ExecutionPolicy Bypass -File scripts\watcher.ps1

# Xem status nhanh
powershell -ExecutionPolicy Bypass -File scripts\check-status.ps1

# Tạo module mới
powershell -ExecutionPolicy Bypass -File scripts\create-module.ps1 -Name "auth"
```

---

## 🎯 Khuyến Nghị

Với hầu hết use cases, **Stateful Single-Agent** là lựa chọn tốt hơn vì:

1. ✅ Đơn giản - Chỉ 1 IDE
2. ✅ Không mất data khi restart
3. ✅ Không cần sync giữa các agents
4. ✅ User chỉ tương tác 1 chỗ

Chỉ dùng Multi-Agent khi:
- Dự án cực kỳ lớn (10+ modules)
- Có team thật sự nhiều người
- Cần parallel execution thực sự

---

> 📖 Xem `workflow.md` để dùng Stateful Single-Agent workflow (khuyên dùng)
