# GIAI ĐOẠN 0 — PROTOTYPE LÕI (BFR.Core)

Nguồn: `docs/specs/13-lo-trinh-mvp-ru-ro.md` — Giai đoạn 0 (1 tuần).

## Phạm vi đã triển khai
- `FileItem`, `RenameState`, `RuleContext`, `IRule` — mô hình dữ liệu cơ bản, hàm thuần.
- `RulePipeline` — chạy chuỗi quy tắc, sinh `PreviewItem` kèm Rule Trace.
- 3 quy tắc prototype: `FindReplaceRule` (văn bản/Regex, matchTimeout 250 ms), `NumberingRule` (đệm 0, vị trí, kiểu số), `TrimRule` (cắt tỉa theo spec FR-B5).
- `NameValidator` — kiểm tra tên hợp lệ (rỗng, ký tự cấm, trailing dot/space, tên dành riêng, độ dài).
- `ConflictDetector` — phát hiện trùng trong lô, va chạm ngoài lô (không phân biệt hoa/thường).
- `RenamePlanner` — thuật toán topo ngược cho chuỗi, tên tạm cho vòng hoán đổi (`a↔b`, `a→b→c`).
- `RenameExecutor` + `Journal` — thực thi ghi trước (write-ahead JSON Lines), flush mỗi bước, xử lý lỗi từng tệp, hủy.
- `IFileSystem` + `FakeFileSystem` — trừu tượng hóa FS; bản giả in-memory dùng cho kiểm thử và giả lập lỗi/mất điện.

## Kiểm thử
- `BFR.Core.Tests` — unit test từng quy tắc, validator, detector, planner, executor, fake FS.
- `BFR.Core.PropertyTests` — property-based test cho bất biến INV-1..INV-6.
- `BFR.Benchmarks` — BenchmarkDotNet cho NFR-1/NFR-2 (1.000 / 10.000 / 100.000 tệp).

## Cổng nghiệm thu (theo spec)
- [ ] Qua property test INV-1..INV-6 ở mức prototype.
- [ ] Có số đo hiệu năng thật (benchmark).
- [ ] Chốt khung giao diện (WinUI 3 vs WPF) sau khi thử bảng 100.000 dòng.

## Cách chạy

```powershell
# Build toàn bộ solution
dotnet build BatchFileRenamer.sln

# Chạy unit test + property test
dotnet test

# Chạy benchmark (NFR-1/NFR-2)
dotnet run --project tests/BFR.Benchmarks -c Release
```

## Tiếp theo (Giai đoạn 1 — MVP Free)
Đủ quy tắc Free, Preview thời gian thực (debounce 150 ms), Rule Trace, xung đột, thực thi + journal + khôi phục, Undo 1 bước, kéo thả, preset dựng sẵn, tiếng Việt/Anh.
