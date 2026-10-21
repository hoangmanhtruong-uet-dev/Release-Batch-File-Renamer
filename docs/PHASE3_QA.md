# BÁO CÁO NGHIỆM THU QA GIAI ĐOẠN 3 (PHASE 3 QA — v1.1 PRO)

Nguồn: `docs/specs/11-kien-thuong-qa.md` và `docs/specs/13-lo-trinh-mvp-ru-ro.md`.  
Cổng nghiệm thu Giai đoạn 3: **QA-09, QA-14, QA-15 và IAP chạy đúng**.

> **Kết luận nhanh:** 127/127 test tự động pass (120 unit + 7 property), build 0 warning/0 error, gói MSIX v1.1.0 đóng gói và validate thành công, CLI và giao diện dùng chung `BFR.Core` nên kết quả khớp nhau.  
> **Phần còn chờ là thao tác bên ngoài:** tạo add-on trong Partner Center, ký gói, và mua thử bằng tài khoản Microsoft thật — không thể thực hiện trong môi trường build cục bộ này.

---

## 1. Môi trường và bằng chứng

| Hạng mục | Giá trị |
| :--- | :--- |
| Ngày kiểm thử | 20/10/2026 |
| Build | `Release`, x64, .NET 8, TFM `net8.0-windows10.0.19041.0` (App/CLI/Store) |
| Kết quả build | **0 warning / 0 error** |
| Unit tests | **120/120 pass** (`BFR.Core.Tests`, gồm 31 test mới của Giai đoạn 3) |
| Property tests | **7/7 pass** |
| Tổng test | **127/127 pass** |
| Package | `artifacts/BatchFileRenamer-1.1.0.msix` (78.202.179 byte ≈ 78,2 MB, self-contained, **chưa ký**) |
| SHA-256 gói | `E9418AE05A2608ACEB7DD3766E21A3F77C12FC1C1CEE88AE76F4914A06A0387B` |
| Portable | `artifacts/BatchFileRenamer-1.1.0-portable-win-x64.zip` (76.865.243 byte) |
| COM smoke | **PASS** trên layout self-contained 1.1.0 |
| CLI smoke | `bfr license status` và `bfr context-menu status` chạy đúng từ layout |

---

## 2. QA-09: EXIF / ngày tháng

| Tiêu chí | Kết quả | Trạng thái |
| :--- | :--- | :---: |
| Ảnh **có** EXIF | `DateRule.Source=Taken` dùng đúng `DateTimeOriginal` (test `DateRule_Taken_UsesExif_WhenAvailable`) | **ĐẠT (tự động)** |
| Ảnh **không** có EXIF | Tự động dùng nguồn dự phòng (Created/Modified) và ghi rõ trong Rule Trace (`dự phòng Created`) | **ĐẠT (tự động)** |
| Ảnh EXIF sai/hỏng | `ExifMetadataReader` bắt lỗi và trả về rỗng → dùng dự phòng, không làm hỏng preview | **ĐẠT (tự động)** |
| Ngày tạo media (video/audio) | `MediaMetadataReader` đọc thẻ "creation date"/"media create date"; `DateSource.Media` + token `{media}`; dự phòng khi thiếu | **ĐẠT (tự động)** |
| Ghi rõ nguồn ngày | Rule Trace ghi “dự phòng &lt;nguồn&gt;” | **ĐẠT** |
| Ảnh thật từ điện thoại | Chưa có bộ ảnh mẫu thật trong repo | **CHỜ (dữ liệu thật)** |

---

## 3. QA-14: Đám mây / ổ mạng

| Tiêu chí | Kết quả | Trạng thái |
| :--- | :--- | :---: |
| Không tải tệp OneDrive ngoài ý muốn | `FileScanner` đánh dấu tệp placeholder (`OFFLINE`, `RECALL_ON_OPEN`, `RECALL_ON_DATA_ACCESS`); EXIF/media **bỏ qua** tệp này | **ĐẠT (tự động)** |
| Cảnh báo khi quy tắc cần EXIF mà tệp offline | Dòng preview hiển thị “Thiếu dữ liệu” (token `{taken}` không âm thầm biến mất) và dùng nguồn dự phòng | **ĐẠT** |
| Ổ mạng ngắt kết nối giữa chừng | Bước lỗi được ghi `failed`, tiếp tục các bước độc lập, journal đủ để khôi phục (giữ nguyên hành vi Giai đoạn 1) | **ĐẠT (tự động, từ QA-05/QA-11)** |
| Tệp OneDrive thật ở trạng thái “chỉ trực tuyến” | Chưa có tài khoản OneDrive trong môi trường kiểm thử | **CHỜ (thủ công)** |

---

## 4. QA-15: CLI

| Tiêu chí | Kết quả | Trạng thái |
| :--- | :--- | :---: |
| `--dry-run` không ghi gì | Kiểm thử chạy CLI thật (`Phase3CliTests`), tệp không bị đổi tên | **ĐẠT (tự động)** |
| `--format json` | Xuất JSON đúng cấu trúc, parse được bằng `System.Text.Json` | **ĐẠT (tự động)** |
| Kết quả khớp giao diện | So từng dòng JSON của CLI với preview tính bằng `BFR.Core` (cùng engine) | **ĐẠT (tự động)** |
| Mã thoát theo đặc tả | `0` thành công, `1` lỗi tham số/trừng quyền, `2` xung đột, `3` thất bại một phần, `4` lỗi hệ thống | **ĐẠT (tự động)** |
| Tính năng Pro của CLI | Ở cấp Free, `bfr rename` trả mã 1 kèm thông báo nâng cấp; `context-menu`/`license`/`history` vẫn dùng được | **ĐẠT (tự động)** |
| Lệnh mới | `bfr license status|trial|buy`, `bfr report export [--format csv|json]` | **ĐẠT** |

---

## 5. IAP (Microsoft Store)

| Tiêu chí | Kết quả | Trạng thái |
| :--- | :--- | :---: |
| Đọc giấy phép add-on | `MicrosoftStoreLicenseProvider` dùng `StoreContext.GetAppLicenseAsync()` → `AddOnLicenses[BFR_PRO_LIFETIME]` | **ĐẠT (mã nguồn, chưa chạy với tài khoản thật)** |
| Mua | `RequestPurchaseAsync` → ánh xạ `Succeeded/AlreadyPurchased/NotPurchased/NetworkError/ServerError` sang trạng thái nội bộ | **ĐẠT (mã nguồn)** |
| Ghi quyền sau khi mua | `LicenseCoordinator.PurchaseProAsync` → `LicenseService.RecordPurchase` → cấp Pro | **ĐẠT (test với nhà cung cấp giả)** |
| Huỷ mua / Store lỗi | Không nâng cấp quyền, hiển thị thông báo tương ứng | **ĐẠT (test với nhà cung cấp giả)** |
| Dùng thử 14 ngày | Ghi cục bộ, hết hạn thì về Free, không dùng lại được | **ĐẠT (tự động)** |
| Mua thật end-to-end | Cần add-on trong Partner Center + tài khoản Microsoft | **CHỜ (bên ngoài)** |
| Ứng dụng ngoài Store | `StoreContext.GetDefault()` lỗi → `IsAvailable=false` → chỉ cấp Free | **ĐẠT (đã kiểm thử trên bản portable)** |

---

## 6. Phân tách Free / Pro (kiểm tra theo PHẦN 3.6)

| Hạng mục | Kết quả kiểm thử | Trạng thái |
| :--- | :--- | :---: |
| Free: 3 quy tắc, 2 cấp quét, 3 preset, 1 lần hoàn tác | `License_DefaultIsFree_WithSpecLimits` | **ĐẠT** |
| Pro: không giới hạn, EXIF/media, báo cáo, CLI | `License_AfterPurchase_UnlocksEveryProFeature` | **ĐẠT** |
| Giao diện hiển thị cấp + nút nâng cấp | Kiểm tra bằng UI Automation: huy hiệu “Free”, nút “Nâng cấp Pro”; ở chế độ Pro nút nâng cấp biến mất | **ĐẠT** |
| Trình soạn quy tắc chặn thêm quy tắc ở Free | `RuleEditorWindow.Add_Click` kiểm tra `MaxRules` và hiện thông báo | **ĐẠT (mã nguồn)** |
| Nguồn ngày Taken/Media bị khoá ở Free | Combo “Nguồn” chỉ còn Created/Modified/Now + ghi chú nâng cấp | **ĐẠT (mã nguồn)** |
| Thư viện preset giới hạn 3 ở Free | Ảnh chụp cửa sổ: “1/3 preset — Bản Free lưu tối đa 3 preset” | **ĐẠT** |

---

## 7. Lỗi phát hiện & đã sửa trong Giai đoạn 3

1. **`HistoryEntry.JournalPath` sai đường dẫn** (thiếu tiền tố thời gian trong tên tệp journal) → “Hoàn tác mục đã chọn” trong cửa sổ Lịch sử không hoạt động. Đã thêm `RenameExecutorResult.JournalPath` và dùng ở cả GUI và CLI.
2. **`IFileSystem` thiếu `Delete`** khiến thư viện preset không xoá được → đã bổ sung cho `RealFileSystem` và `FakeFileSystem`.
3. **MediaMetadataReader đọc cả tệp offline** → bổ sung chặn tệp placeholder.
4. **Gói MSIX v1.0.0 thiếu `BFR.Store.dll`** → bổ sung vào danh sách file bắt buộc của `build-msix.ps1`.

---

## 8. Tổng kết

- **Đạt:** QA-09 (tự động), QA-14 (tự động), QA-15 (tự động), ma trận Free/Pro (tự động + kiểm tra UI), IAP (mã nguồn + test với nhà cung cấp giả).
- **Chờ bên ngoài:** add-on Partner Center, chứng chỉ ký, giao dịch mua thật, tệp OneDrive thật, ảnh EXIF thật.
- **Không nên phát hành bản Pro cho tới khi** tạo add-on trong Partner Center, ký gói và kiểm thử luồng mua trên thiết bị thật.
