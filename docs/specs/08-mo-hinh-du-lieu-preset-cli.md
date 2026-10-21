# 08. MÔ HÌNH DỮ LIỆU, PRESET, CẤU HÌNH VÀ CLI

Nguồn: `docs/batch_file_renamer_spec_v1.0.md` — Phần 9.

---

## 8.1 Lưu trữ

- Cài đặt: `%LocalAppData%\BatchFileRenamer\settings.json`.
- Preset: `%LocalAppData%\BatchFileRenamer\presets\*.bfrpreset` (mỗi tệp một preset, dễ chia sẻ).
- Lịch sử/journal: `%LocalAppData%\BatchFileRenamer\journal\` (xem đặc tả 06).
- **Ghi nguyên tử** với `File.Replace` và tệp `.bak`; trường `schemaVersion` + migration giữa các phiên bản.

---

## 8.2 Schema Preset (`.bfrpreset`)

```json
{
  "schemaVersion": "1.0.0",
  "name": "Dọn dẹp ảnh theo ngày chụp",
  "description": "Bỏ (1), gộp khoảng trắng, đặt tên theo ngày chụp và số thứ tự",
  "options": {
    "recursive": false,
    "includeFolders": false,
    "allowExtensionChange": false,
    "sortBy": "Name.Natural"
  },
  "rules": [
    {
      "type": "Trim",
      "name": "Dọn dẹp",
      "enabled": true,
      "params": { "removeChars": "()", "collapseSpaces": true, "trimEnds": true }
    },
    {
      "type": "Template",
      "enabled": true,
      "params": { "template": "{taken:yyyy-MM-dd}_{n:3}" }
    },
    {
      "type": "Numbering",
      "enabled": true,
      "params": { "start": 1, "step": 1, "resetMode": "ByTokenChange", "resetToken": "{taken:yyyy-MM-dd}" }
    },
    {
      "type": "Case",
      "enabled": true,
      "params": { "mode": "Lower", "scope": "Extension" }
    }
  ],
  "dateSourceFallback": ["Taken", "Created", "Modified"]
}
```

---

## 8.3 Thư viện Preset dựng sẵn (đề xuất)

| Tên | Tác dụng |
| :--- | :--- |
| Dọn dẹp tên tệp | Bỏ `(1)`, `- Copy`, gộp khoảng trắng, cắt hai đầu |
| Bỏ dấu tiếng Việt + gạch nối | `Báo cáo tháng 5.docx` thành `bao-cao-thang-5.docx` |
| Đánh số theo thứ tự | `001`, `002`, … theo natural sort |
| Ảnh theo ngày chụp | `YYYY-MM-DD_001.jpg` từ EXIF |
| Chữ thường toàn bộ | Chuyển tên và đuôi sang chữ thường |
| Thêm tiền tố thư mục cha | `{parent}_{name}` |
| Chuẩn hóa Unicode (NFC) | Sửa tên tệp "dấu rời" |

---

## 8.4 Đặc tả CLI (`bfr.exe`, Pro)

```text
bfr rename <paths...> [options]

  --preset <name|file.bfrpreset>   Dùng preset
  --find <pattern> --replace    Tìm/thay nhanh (kết hợp --regex, --ignore-case)
  --number <start>[:step[:width]]  Đánh số nhanh
  --recursive                      Quét thư mục con
  --include <glob> / --exclude <glob>
  --dry-run                        Chỉ in kết quả, không ghi gì (mặc định cho script an toàn)
  --yes                            Bỏ qua câu hỏi xác nhận
  --format text|json|csv           Định dạng kết quả

bfr undo [--last | --id <id>]      Hoàn tác
bfr history                        Liệt kê lịch sử
bfr preset list|export|import      Quản lý preset
```

Mã thoát: `0` thành công; `1` lỗi tham số; `2` có xung đột/tên không hợp lệ (không thay đổi gì); `3` thực thi thất bại một phần (đã in danh sách); `4` lỗi hệ thống.

CLI và giao diện dùng **chung `BFR.Core`**, nên hành vi giống hệt nhau.
