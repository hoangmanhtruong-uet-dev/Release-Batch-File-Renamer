# GIAI ĐOẠN 1 — MVP (FREE)

Nguồn: `docs/specs/13-lo-trinh-mvp-ru-ro.md` — Giai đoạn 1 (4 tuần).  
Phạm vi: đủ quy tắc Free, Preview thời gian thực, Rule Trace, xung đột, thực thi + journal + khôi phục, Undo, kéo thả, preset dựng sẵn, tiếng Việt/Anh.

## Đã triển khai

### Quy tắc Free (BFR.Core)
| Quy tắc | File |
| :--- | :--- |
| Tìm/thay + Regex (matchTimeout 250 ms) | `FindReplaceRule.cs` |
| Số thứ tự (đệm 0, vị trí, kiểu số, reset) | `NumberingRule.cs` |
| Ngày tháng (Created/Modified/Taken/Now, chuỗi dự phòng) | `DateRule.cs` |
| Cắt tỉa | `TrimRule.cs` |
| Chèn | `InsertRule.cs` |
| Hoa/thường (Lower/Upper/Title/Sentence, ngoại lệ) | `CaseRule.cs` |
| Bỏ dấu + chuẩn hóa + Slug | `SanitizeRule.cs` |
| Phần mở rộng (có cờ cho phép) | `ExtensionRule.cs` |
| Mẫu tên (Template) | `TemplateRule.cs` |
| **Token Engine** `{name} {ext} {orig} {n:n} {parent} {created:fmt} {modified:fmt} {taken:fmt} {now:fmt} {size} {rand:n}` | `TokenEngine.cs` |

### Preview, xung đột, Rule Trace
- `PreviewEngine` — kết hợp pipeline + `NameValidator` + `ConflictDetector`, gắn trạng thái từng dòng (`PreviewStatus`): Ok, Unchanged, Conflict, InvalidName, PathTooLong.
- Rule Trace có sẵn trong `PreviewItem.Trace`.
- `PreviewItem.Detail` nêu rõ lý do chặn (trùng lô, va chạm ngoài lô, tên dành riêng, đường dẫn quá dài...).
- Xung đột được xét **theo từng thư mục**; `OutsideBatchIndex` tự dựng danh sách tệp/thư mục ngoài lô cho UI và CLI.
- Kiểm tra cả độ dài **đường dẫn đầy đủ** (QA-13), không chỉ tên tệp.
- Chạy nền có `CancellationToken` (không khóa UI).

### EXIF / metadata
- `ExifMetadataReader` dùng MetadataExtractor: `DateTimeOriginal → DateTimeDigitized → IFD0 DateTime`.
- Có bộ nhớ đệm theo `(đường dẫn, thời gian sửa, kích thước)`.
- `MetadataRequirement` chỉ đọc EXIF khi chuỗi quy tắc thực sự cần (`Date.Source=Taken` hoặc token `{taken...}`), tránh I/O vô ích.

### Preset
- `Preset`, `PresetStore` (ghi nguyên tử + `.bak`), `RuleFactory`, `PresetCompiler`.
- Thư viện dựng sẵn `BuiltInPresets`: Dọn dẹp tên tệp, Bỏ dấu + gạch nối, Đánh số, Ảnh theo ngày chụp, Chữ thường, Tiền tố thư mục cha, Chuẩn hóa NFC.
- UI có **Sửa quy tắc** (thêm/xóa/đổi thứ tự, form cấu hình cho cả 9 loại), **Lưu preset** và **Nhập preset**.

### Thực thi an toàn
- `RenamePlanner` (sắp xếp topo ngược + tên tạm cho vòng).
- `RenameExecutor` + journal ghi trước (JSONL, flush, `done`/`failed` kèm From/To).
- `UndoService` (hoàn tác theo journal, hoàn tác từng phần kèm lý do).
- `RecoveryService` (phát hiện journal dở dang, hoàn tất hoặc khôi phục).
- `HistoryStore` (lịch sử áp dụng, ghi nguyên tử).

### Nạp tệp
- `FileScanner` (đệ quy, độ sâu, include/exclude glob, loại ẩn/hệ thống, gồm thư mục từ lá lên gốc).
- `NaturalSortComparer` (StrCmpLogicalW + fallback).

### Giao diện (WPF — phương án B)
`src/BFR.App` gồm:
- Cửa sổ chính: chọn preset, thêm tệp/thư mục, kéo thả, bảng preview ảo hóa (kèm cột **Chi tiết** lý do chặn), Rule Trace, Áp dụng, Hoàn tác. Preview debounce 150 ms + chạy nền có hủy. Danh sách nguồn tự cập nhật sau khi đổi tên/hoàn tác.
- `RuleEditorWindow`: sửa chuỗi quy tắc, form động theo loại.
- `HistoryWindow`: lịch sử các lần áp dụng, hoàn tác từng mục.
- Hộp thoại **khôi phục sau sự cố** khi khởi động nếu phát hiện journal dở dang.
- **Đa ngôn ngữ vi/en** cho toàn bộ giao diện (ResourceDictionary + `Loc`); lựa chọn được lưu vào `settings.json` và khôi phục khi khởi động.

### CLI (Pro)
`src/BFR.Cli` (`bfr.exe`): `rename`, `undo`, `history`, `preset list|export|import`; `--dry-run`, `--format text|json|csv`, mã thoát 0/1/2/3/4.

## Kiểm thử
- `BFR.Core.Tests`: **74 test** — quy tắc, token, preview, preset, undo/recovery, scanner, history, RuleFactory và các ca QA-02, 04, 05, 06, 07, 08, 09, 11, 13.
- `BFR.Core.PropertyTests`: **7 test** — INV-1..INV-6 và QA-01 với **1.000 ca ngẫu nhiên** (preview khớp áp dụng).
- Tổng: **81/81 pass**.
- QA-05 chạy mô phỏng sự cố tại **mọi bước** của lô 200 tệp, rồi Complete + Rollback.
- QA-08 kiểm tra cả mẫu sai cú pháp và mẫu backtracking nặng (timeout 250 ms, không treo).
- Tìm/thay văn bản thuần không diễn giải `$1`; token trong chuỗi thay thế được mở rộng đúng ngữ cảnh.
- Đã xác minh CLI thật: rename 2 tệp → undo về đúng trạng thái ban đầu.
- Đã xác minh ứng dụng WPF khởi động và chạy ổn định (không lỗi XAML/resource).

## Cách chạy
```powershell
dotnet build BatchFileRenamer.sln -c Release
dotnet test BatchFileRenamer.sln -c Release

# Giao diện
dotnet run --project src/BFR.App -c Release

# CLI
dotnet run --project src/BFR.Cli -c Release -- preset list
dotnet run --project src/BFR.Cli -c Release -- rename "D:\Anh" --preset "Ảnh theo ngày chụp" --dry-run
```

## Việc còn lại của Giai đoạn 1 (theo cổng nghiệm thu)
- [ ] QA-10 trên máy thật với 100.000 tệp trong cây sâu (benchmark NFR-1 đã đạt; cần đo cả UI).
- [ ] QA-11/QA-13 trên máy thật (tầng Core đã có test giả lập).
- [ ] Dogfooding 2 tuần trên bản sao dữ liệu thật.
- [ ] Rà soát QA-12/14/16/17 (thuộc Giai đoạn 2 — MSIX, menu chuột phải, đám mây, trợ năng).
