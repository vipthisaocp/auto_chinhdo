# 👁️ Vision Spec - Task T003

## 📐 Template Settings

| Template Name | File Path | Threshold | ROI (Vùng quét) | Ghi chú |
|---------------|-----------|-----------|-----------------|---------|
| **NHIEMVU_TITLE** | `pk_shared/nhiemvu.png` | 0.85 | Top-Right (X: 600-900, Y: 0-200) | Nhận diện chữ "Nhiệm vụ" |
| **LANCAN_BTN** | `pk_shared/lancan.png` | 0.90 | Mid-Right (X: 800-960, Y: 100-300) | Nút tab Người chơi |

## 🖱️ Control coordinates (960x540)
- **Nút Lân Cận (Tab Người chơi)**: `X: 890, Y: 155` (Tỉ lệ %: `X: 92.7%, Y: 28.7%`)

## ⚙️ Performance Target
- **Detection Speed**: < 200ms
- **Memory Usage**: Không rò rỉ (Sử dụng `using Mat`)
