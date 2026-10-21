# GIAI ĐOẠN 4 — v1.2 (WATCH FOLDER, ĐA NGÔN NGỮ, TỐI ƯU HIỆU NĂNG, CẬP NHẬT PORTABLE)

Nguồn: `docs/specs/13-lo-trinh-mvp-ru-ro.md` — Giai đoạn 4 (3–4 tuần).  
Phạm vi: **Watch Folder**, **preset chia sẻ/nhập**, **nhiều ngôn ngữ**, **tối ưu hiệu năng**, **tự động cập nhật bản portable**.  
Cổng nghiệm thu: **rà soát hiệu năng và độ ổn định**.

> **Trạng thái:** đã triển khai đầy đủ và kiểm thử tự động. Báo cáo hiệu năng đo thật nằm ở `artifacts/perf-report.md`; cổng hiệu năng được chạy trong CI (`tests/BFR.PerfGate`) và làm fail khi vượt ngưỡng.

---

## 1. Watch Folder (FR-D4, Pro)

Tự động đổi tên tệp mới xuất hiện trong một thư mục, không cần bấm tay.

| Thành phần | Vị trí | Vai trò |
| :--- | :--- | :--- |
| `WatchFolderOptions` | `BFR.Core/WatchFolder/` | Tuỳ chọn: thư mục, preset, đệ quy, thời gian chờ ổn định, bộ lọc glob, chế độ dry-run |
| `WatchFolderPlanner` | `BFR.Core/WatchFolder/` | Quyết định tệp nào "đã ổn định" (tách khỏi hệ thống tệp để kiểm thử được) |
| `WatchFolderProcessor` | `BFR.Core/WatchFolder/` | Áp dụng preset cho một lô tệp, **dùng chung pipeline** với GUI/CLI |
| `WatchFolderService` | `BFR.Core/WatchFolder/` | `FileSystemWatcher` + hẹn giờ gom lô, nhật ký, chống thao tác lặp |
| `WatchFolderWindow` | `BFR.App/` | Giao diện: chọn thư mục/preset, bật-tắt, xem nhật ký |
| `bfr watch` | `BFR.Cli` | Chạy Watch Folder từ dòng lệnh (dừng bằng Ctrl+C) |

### Ba cơ chế bảo an toàn
1. **Chờ tệp ổn định (settle delay, mặc định 1.500 ms):** không đổi tên tệp đang được sao chép.
2. **Bỏ qua tệp có sẵn từ trước khi bật đầu:** chỉ xử lý tệp **mới**.
3. **Chống lặp:** ghi nhớ các tệp mà Watch Folder vừa tạo ra, nên tên mới không bị đổi tiếp.

Mỗi lô đổi tên đều ghi **journal** (như mọi thao tác khác) nên có thể hoàn tác từ Lịch sử.

---

## 2. Preset chia sẻ / nhập

- `PresetStore.ToJson()` / `FromJson()` (mới) — chuyển preset thành chuỗi JSON.
- Trong cửa sổ **Thư viện preset**: nút **Chia sẻ** (chép JSON vào clipboard) và **Nhập từ bảng nhớ tạm** — người dùng gửi preset cho nhau bằng email/Zalo mà không cần máy chủ.
- Nhập/xuất tệp `.bfrpreset` vẫn dùng như cũ (Pro).

---

## 3. Đa ngôn ngữ

| Ngôn ngữ | Mã | Tệp |
| :--- | :--- | :--- |
| Tiếng Việt | `vi` | `src/BFR.App/Resources/Strings.vi.xaml` |
| English | `en` | `src/BFR.App/Resources/Strings.en.xaml` |
| Français | `fr` | `src/BFR.App/Resources/Strings.fr.xaml` |
| 简体中文 | `zh-Hans` | `src/BFR.App/Resources/Strings.zh-Hans.xaml` |

- Test `LanguageResources_AllLanguagesHaveSameKeys` bảo đảm 4 file luôn có **cùng bộ khoá** (không có chuỗi bị thiếu khi thêm ngôn ngữ mới).
- Ngôn ngữ được lưu trong `settings.json` và khôi phục khi mở ứng dụng.

---

## 4. Tối ưu hiệu năng (NFR-1..NFR-5)

| Thay đổi | Tác dụng |
| :--- | :--- |
| `MediaFileTypes` | Bỏ qua việc mở tệp khi phần mở rộng chắc chắn không có metadata (`.txt`, `.pdf`, `.zip`…) → preview 100.000 tệp không còn mở hàng trăm nghìn tệp vô ích |
| `BoundedCache` | Cache metadata có giới hạn 8.192 phần tử → không phình bộ nhớ trên lô rất lớn |
| Tái sử dụng instance metadata provider | Cache còn hiệu lực giữa các lần preview (trước đây tạo mới mỗi lần → cache mất hoàn toàn) |
| `tests/BFR.PerfGate` | Cổng đo hiệu năng chạy trong CI, **fail** nếu vượt ngưỡng |

### Kết quả đo (máy 12 luồng, SSD, 21/10/2026)

| Phép đo | Mục tiêu | Kết quả | Ngưỡng |
| :--- | :--- | ---: | ---: |
| Preview 10.000 tệp | < 300 ms | **60 ms** | 300 ms |
| Preview 100.000 tệp | < 3 s | **234 ms** | 3.000 ms |
| Bộ nhớ preview 100.000 tệp | < 250 MB | **73 MB** | 250 MB |
| Bỏ qua metadata 100.000 tệp không phải media | < 1.000 ms | **83 ms** | 1.000 ms |
| Quét 10.000 tệp thật | < 2 s | **1.448 ms** | 2.000 ms |
| Áp dụng 1.000 lần đổi tên thật | 10.000 lần trong 10–30 s | **1.785 ms** | 30.000 ms |
| Gom 100.000 sự kiện Watch Folder | < 1.000 ms | **40 ms** | 1.000 ms |

> Báo cáo đầy đủ: `artifacts/perf-report.md` (sinh lại bằng `dotnet run --project tests/BFR.PerfGate`). Các phép đo có I/O thực biến động theo tải máy; cổng CI dùng ngưỡng của đặc tả.

---

## 5. Tự động cập nhật bản portable

- `UpdateChecker` + `IUpdateFeed` (Core) — so sánh phiên bản ngữ nghĩa (`1.2.0` < `1.10.0`).
- `GitHubReleasesUpdateFeed` — đọc API "latest release" (URL do người dùng cấu hình trong `settings.json`).
- Nút **Kiểm tra cập nhật** trên thanh công cụ: bản Store được Store cập nhật tự động (hiện thông báo tương ứng), bản portable mới gọi mạng.
- **Opt-in:** mặc định **TẮT**; chỉ gọi đúng một URL công khai, không gửi dữ liệu người dùng (đã ghi trong Privacy Policy).

---

## 6. CLI mới

```powershell
bfr watch <thư mục> --preset <tên> [--include <glob>] [--exclude <glob>] \
                   [--settle <ms>] [--no-recursive] [--dry-run]
```

Ví dụ:

```powershell
$env:BFR_DEV_TIER = "pro"   # chỉ khi chạy ngoài gói MSIX
bfr watch "C:\Downloads\Inbox" --preset "Đánh số theo thứ tự" --include "*.jpg"
```

---

## 7. Kiểm thử

| Nhóm | Số test | Kết quả |
| :--- | ---: | :--- |
| `BFR.Core.Tests` | 142 | 142/142 pass |
| `BFR.Core.PropertyTests` | 7 | 7/7 pass |
| **Tổng** | **149** | **149/149 pass** |
| Cổng hiệu năng | 7 phép đo | đạt toàn bộ ngưỡng |

Test mới của Giai đoạn 4 (`tests/BFR.Core.Tests/Phase4Tests.cs`): planner ổn định, processor (đổi tên/dry-run/xung đột/bộ lọc), service thật (tệp mới được đổi tên, tệp có sẵn bị bỏ qua), update checker, so sánh phiên bản, lọc phần mở rộng, bộ nhớ đệm có giới hạn, chia sẻ preset, tính nhất quán khoá giữa 4 ngôn ngữ.

---

## 8. Cổng nghiệm thu Giai đoạn 4

| Hạng mục | Trạng thái |
| :--- | :--- |
| Watch Folder (FR-D4) | **Đạt** — tự động + kiểm thử thật với hệ thống tệp |
| Preset chia sẻ/nhập | **Đạt** — JSON qua clipboard + tệp `.bfrpreset` |
| Nhiều ngôn ngữ | **Đạt** — 4 ngôn ngữ, test tự động bảo đảm cùng bộ khoá |
| Tối ưu hiệu năng | **Đạt** — đo thật, có cổng chặn hồi quy trong CI |
| Tự động cập nhật portable | **Đạt (mã nguồn)** — cần kiểm thử với URL thật khi phát hành |

### Việc còn lại (bên ngoài)
- Điền URL nguồn cập nhật thật trong `settings.json` (mặc định đang là placeholder `YOUR-ORG`).
- Kiểm thử Watch Folder trên ổ mạng/OneDrive (nơi sự kiện tệp đến chậm và thưa).
- Đóng gói v1.2.0 đã xong nhưng **chưa ký**; các cổng Store của Giai đoạn 2/3 vẫn còn nguyên.

---

## 9. Giao diện v2.0 (rebuild theo `docs/Plan/BFR_UIUX_REBUILD_PLAN.md`)

Trong Giai đoạn 4, giao diện được dựng lại theo plan: CommandBar 1 hàng, Rule Builder dạng thẻ với accordion sửa tham số tại chỗ, bảng xem trước có diff màu + cột chọn + lọc/tìm, thanh hành động dưới với hộp xác nhận, theme sáng/tối có token, trang Cài đặt, empty state.

- Quyết định công nghệ: `docs/ui-rebuild-decision.md`
- Báo cáo đầy đủ + ảnh + nợ còn lại: `docs/UI_REBUILD_REPORT.md`
- Kiểm thử: 14 test ViewModel (`tests/BFR.Presentation.Tests`) + smoke test UI (`packaging/smoke-ui.ps1`) chạy trong CI.
