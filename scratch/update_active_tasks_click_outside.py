import os

path = r"Docs/ACTIVE_TASKS.md"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

target = "      - Click lại chính ô đang chọn -> tự động đóng Popup (Toggle thông minh)."
replacement = """      - Click lại chính ô đang chọn -> tự động đóng Popup (Toggle thông minh).
      - **Đóng Popup Khi Click Ra Ngoài (Click Outside to Close):** Khi bảng chi tiết đang mở, nhấp chuột ra ngoài khung popup (khoảng trống túi đồ, tab bar, bảng chỉ số...) sẽ tự động đóng popup ngay lập tức; nếu nhấp trúng ô đồ khác thì dịch chuyển popup sang ô mới đó như cũ."""

assert target in content, "target not found"
content = content.replace(target, replacement)

with open(path, "w", encoding="utf-8") as f:
    f.write(content)

print("ACTIVE_TASKS_CLICK_OUTSIDE_UPDATED_SUCCESSFULLY")
