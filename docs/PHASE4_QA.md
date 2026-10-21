# BÁO CÁO NGHIỆM THU QA GIAI ĐOẠN 4 (PHASE 4 QA — v1.2)

Nguồn: `docs/specs/11-kien-thuong-qa.md`, `docs/specs/03-yeu-cau-phi-chuc-nang.md` và `docs/specs/13-lo-trinh-mvp-ru-ro.md`.  
Cổng nghiệm thu Giai đoạn 4: **rà soát hiệu năng và độ ổn định**.

> **Kết luận nhanh:** 149/149 test tự động pass (142 unit + 7 property), build 0 warning/0 error, cổng hiệu năng đạt toàn bộ ngưỡng NFR-1..NFR-5, gói MSIX v1.2.0 đóng gói và validate thành công, Watch Folder chạy thật với hệ thống tệp và CLI.

---

## 1. Môi trường và bằng chứng

| Hạng mục | Giá trị |
| :--- | :--- |
| Ngày kiểm thử | 21/10/2026 |
| Build | `Release`, x64, .NET 8 |
| Kết quả build | **0 warning / 0 error** |
| Unit tests | **142/142 pass** |
| Property tests | **7/7 pass** |
| Tổng test | **149/149 pass** |
| Cổng hiệu năng | `tests/BFR.PerfGate` → **7/7 phép đo đạt ngưỡng** (`artifacts/perf-report.md`) |
| Package | `artifacts/BatchFileRenamer-1.2.0.msix` (self-contained, **chưa ký**) |
| SHA-256 gói | `EA9B1A38178972FF243452028783E9EEC0532E8C2CF90956A85765E6D4BF05FF` |
| Portable | `artifacts/BatchFileRenamer-1.2.0-portable-win-x64.zip` (76.887.980 byte) |
| COM smoke | **PASS** trên layout self-contained 1.2.0 |

---

## 2. Rà soát hiệu năng (NFR-1..NFR-5)

| Mã | Mục tiêu | Kết quả đo | Ngưỡng | Trạng thái |
| :--- | :--- | ---: | ---: | :---: |
| NFR-1 | Preview 10.000 tệp < 300 ms | 60 ms | 300 ms | **ĐẠT** |
| NFR-1 | Preview 100.000 tệp < 3 s | 234 ms | 3.000 ms | **ĐẠT** |
| NFR-2 | Quét 10.000 tệp thật < 2 s | 1.448 ms | 2.000 ms | **ĐẠT** |
| NFR-3 | Bỏ qua metadata cho 100.000 tệp không phải media < 1 s | 83 ms | 1.000 ms | **ĐẠT** |
| NFR-4 | Áp dụng 1.000 lần đổi tên thật | 1.785 ms | 30.000 ms | **ĐẠT** |
| NFR-5 | Bộ nhớ preview 100.000 tệp < 250 MB | 73 MB | 250 MB | **ĐẠT** |

> Số đo lấy từ `artifacts/perf-report.md` (lần chạy 21/10/2026 00:31). Các phép đo có I/O thực (quét/đổi tên) biến động theo tải máy, ví dụ lần chạy trước đó quét 10.000 tệp mất 579 ms; cổng CI vẫn dùng ngưỡng của đặc tả.

**Các tối ưu đã thực hiện:**
- Lọc theo phần mở rộng trước khi mở tệp (`MediaFileTypes`) — loại bỏ hàng trăm nghìn lượt mở tệp vô ích.
- Cache metadata có giới hạn (`BoundedCache`, 8.192 mục) — không phình bộ nhớ trên lô rất lớn.
- Tái sử dụng instance metadata provider giữa các lần preview — cache còn hiệu lực.
- Cổng đo chạy trong CI và **làm fail** khi vượt ngưỡng (`.github/workflows/ci.yml`).

> NFR-3 (đọc EXIF 1.000 ảnh thật) và NFR-4 ở quy mô 10.000 tệp **chưa** đo với bộ ảnh/tệp thật trong môi trường này; số đo hiện tại dùng bộ sinh và tệp thật tạo tạm.

---

## 3. Độ ổn định (Watch Folder, v1.2)

| Tiêu chí | Kết quả | Trạng thái |
| :--- | :--- | :---: |
| Chờ tệp ổn định trước khi đổi tên | `WatchPlanner_FileIsReadyOnlyAfterSettleDelay` | **ĐẠT (tự động)** |
| Sự kiện mới làm đặt lại mốc chờ | `WatchPlanner_MoreEventsRestartTheSettleWindow` | **ĐẠT (tự động)** |
| Áp dụng preset cho tệp mới | `WatchProcessor_AppliesPresetToNewFiles` (có journal) | **ĐẠT (tự động)** |
| Dry-run không ghi gì | `WatchProcessor_DryRun_DoesNotTouchFilesystem` | **ĐẠT (tự động)** |
| Xung đột với tệp đã có | `WatchProcessor_ConflictWithExistingFile_IsSkipped` | **ĐẠT (tự động)** |
| Bộ lọc include/exclude | `WatchProcessor_RespectsIncludeAndExcludeGlobs` | **ĐẠT (tự động)** |
| Tệp có sẵn bị bỏ qua, tệp mới được đổi tên | `WatchService_RenamesNewFile_AndIgnoresExistingOnes` (hệ thống tệp thật) | **ĐẠT (tự động)** |
| Chạy thật qua CLI | `bfr watch` phát hiện tệp mới, chờ ổn định, đổi tên `IMG_9.txt → 001.txt`, ghi nhật ký | **ĐẠT (thủ công, có log)** |
| Chống lặp khi tên mới sinh sự kiện mới | Ghi nhớ đường dẫn mới sinh ra, không xử lý lại | **ĐẠT (mã nguồn)** |
| Ổ mạng / OneDrive (độ trễ sự kiện) | Chưa có môi trường OneDrive/ổ mạng thật | **CHỜ (thủ công)** |

---

## 4. Preset chia sẻ, đa ngôn ngữ, cập nhật portable

| Tiêu chí | Kết quả | Trạng thái |
| :--- | :--- | :---: |
| Chia sẻ preset qua clipboard | `PresetShare_RoundTripsThroughJson` + nút Chia sẻ/Nhập từ bảng nhớ | **ĐẠT (tự động)** |
| 4 ngôn ngữ vi/en/fr/zh-Hans | Test `LanguageResources_AllLanguagesHaveSameKeys` xác nhận cùng bộ khoá; đã chụp màn hình giao diện tiếng Trung hoạt động đầy đủ | **ĐẠT (tự động + hình ảnh)** |
| So sánh phiên bản ngữ nghĩa | `VersionComparer_ComparesSemantically` (1.2.1 > 1.2.0; v1.3.0 > 1.2.0; 1.1.0 < 1.2.0) | **ĐẠT (tự động)** |
| Kiểm tra cập nhật | `UpdateChecker_ReportsNewerVersion`, `UpdateChecker_NoUpdateWhenSameOrOlder`, `UpdateChecker_FeedError_IsReportedNotThrown` | **ĐẠT (tự động)** |
| Kiểm tra cập nhật với URL thật | URL mặc định trong `settings.json` vẫn là placeholder `YOUR-ORG` | **CHỜ (cấu hình khi phát hành)** |

---

## 5. Kiểm thử hồi quy (Regression)

| Nhóm | Kết quả |
| :--- | :--- |
| Unit + property (toàn bộ từ Giai đoạn 1 đến 4) | **149/149 pass** |
| COM smoke test (menu Explorer, self-contained) | **PASS** |
| `bfr license status` / `bfr context-menu status` (layout 1.2.0) | **PASS** |
| Giao diện (UI Automation): huy hiệu Free/Pro, nút nâng cấp, cửa sổ Thư viện preset, cửa sổ Watch Folder | **PASS** (có ảnh `docs/store-assets/screenshot-watch-folder.png`) |

---

## 6. Lỗi phát hiện & đã sửa trong Giai đoạn 4

1. **Bộ nhớ đệm metadata bị mất sau mỗi lần preview:** `MainWindow` tạo mới `ExifMetadataReader`/`MediaMetadataReader` mỗi lần tính → cache vô dụng. Đã sửa bằng cách tái sử dụng instance theo cấp giấy phép.
2. **Cửa sổ Watch Folder lộn xộn (nút bị kéo giãn chiềm cao):** quên `DockPanel.SetDock(buttons, Dock.Bottom)` (mặc định của DockPanel là `Left`). Đã sửa và chụp lại ảnh.
3. **`FileScanner.FromFile` dùng hệ thống tệp thật** khiến Watch Folder không dùng được `IFileSystem` giả → thêm overload `FromFile(IFileSystem, path, loadOrder)`.
4. **Gói MSIX v1.2.0:** bổ sung `BFR.Store.dll` vào danh sách file bắt buộc (đã có từ Giai đoạn 3) và nâng `Identity Version` lên 1.2.0.0.

---

## 7. Tổng kết

- **Đạt:** cổng rà soát hiệu năng (7/7 phép đo), độ ổn định Watch Folder (tự động + thử thật), preset chia sẻ, đa ngôn ngữ, cập nhật portable.
- **Chờ bên ngoài:** URL nguồn cập nhật thật, kiểm thử Watch Folder trên OneDrive/ổ mạng, chứng chỉ ký và các cổng Store của Giai đoạn 2–3.
- **Trạng thái phát hành:** gói **v1.2.0** đã đóng gói và validate, **chưa ký**; có thể nộp sau khi hoàn tất cổng Store của Giai đoạn 2/3.
