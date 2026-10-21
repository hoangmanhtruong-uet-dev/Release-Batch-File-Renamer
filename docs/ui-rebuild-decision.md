# U0 — Audit giao diện hiện tại & Quyết định công nghệ cho rebuild UI/UX (v2.0)

> Nguồn: `docs/Plan/BFR_UIUX_REBUILD_PLAN.md` (mục 1–3, 6, 8 và mục 11 "Prompt giao cho coding agent").
> Phạm vi tài liệu này: **chỉ lớp giao diện**. `BFR.Core` giữ nguyên; kết quả test INV-1..INV-6 không đổi.

---

## 1. Audit cấu trúc UI hiện tại

| Hạng mục | Hiện trạng |
| :--- | :--- |
| **Framework** | **WPF** trên .NET 8, TFM `net8.0-windows10.0.19041.0` (chốt ở Giai đoạn 0, T8 — vì build ổn định, ảo hóa bảng tốt, đóng gói MSIX đơn giản) |
| **Project UI** | `src/BFR.App` (WinExe + WPF), tham chiếu `BFR.Core` và `BFR.Store` (Store IAP) |
| **Màn hình** | `MainWindow` (chính), `RuleEditorWindow` (hộp thoại sửa tham số quy tắc), `HistoryWindow` (lịch sử/hoàn tác), `PresetLibraryWindow` (code-only), `WatchFolderWindow` (code-only) |
| **Chuỗi** | `src/BFR.App/Resources/Strings.{vi,en,fr,zh-Hans}.xaml` + `Localization.cs` (`Loc.T/F/L`); test `LanguageResources_AllLanguagesHaveSameKeys` bắt buộc 4 file có cùng bộ khoá |
| **Mô hình** | **Code-behind**: `MainWindow.xaml.cs` (609 dòng) giữ luôn logic quét, preview, áp dụng, undo, lịch sử; không có ViewModel |
| **Bảng xem trước** | `DataGrid` 4 cột (Tên cũ / Tên mới / Trạng thái / Chi tiết) + `StatusToTextConverter`; **không** có cột chọn, **không** diff màu, **không** lọc/tìm |
| **Chuỗi quy tắc** | `ListBox` chỉ hiện text (`FindReplace`, `Numbering`…); sửa tham số phải mở hộp thoại `RuleEditorWindow` |
| **Theme** | Không có design token; màu đến từ `SystemColors` rải rác trong từng view; chưa có dark mode, chưa có Mica |
| **Trợ năng** | Đã có `AutomationProperties.Name` ở các control chính, `LiveSetting=Polite` cho thanh trạng thái, hỗ trợ High Contrast ở nút Áp dụng |

### 10 vấn đề UI/UX đã xác nhận từ ảnh chụp (khớp với audit mục 2 của plan)

1. Thanh công cụ 2 hàng, ~17 nút đồng cấp — không có điểm nhìn chính.
2. Badge "Free"/"Nâng cấp Pro" và chọn ngôn ngữ nằm lẫn trong thanh thao tác.
3. Panel "CHUỖI QUY TẮC" gần như trống, sửa phải mở hộp thoại riêng → đứt mạch "chỉnh quy tắc → xem kết quả".
4. Bảng xem trước không highlight khác biệt, cột "Chi tiết" trống, "Sẽ đổi" lặp 14 lần.
5. Rule Trace dùng font monospace, ẩn trong khung cuối cửa sổ, thiếu đồng bộ ngôn ngữ.
6. Thanh trạng thái là một dòng chữ liền, không bấm để lọc được.
7. Control kiểu cũ, không icon, không Mica, không dark mode.
8. Không có empty state / vùng kéo thả / gợi ý lần đầu.
9. Thứ tự đánh số không được giải thích trên UI (thiếu "Sắp xếp theo").
10. Nút "ÁP DỤNG" không có xác nhận mức rủi ro/tóm tắt, không có InfoBar thành công.

---

## 2. Quyết định công nghệ

### ✅ Quyết định: **Giữ WPF, dựng theme Fluent tự thân (không thêm thư viện UI)**

| Tiêu chí | WPF + theme tự thân | WPF + WPF-UI/ModernWpf | WinUI 3 (Windows App SDK) |
| :--- | :--- | :--- | :--- |
| Rủi ro đóng gói MSIX | **Thấp** (chỉ thêm 1 file theme) | Trung bình (thêm package lớn, tài nguyên/theme của thư viện, xung đột với resource 4 ngôn ngữ) | **Cao** (Windows App SDK runtime phải đóng gói kèm hoặc phụ thuộc framework package; `bfr.exe`/COM server cũng phải nhúng → rủi ro Store certification) |
| Tác động `BFR.Core` + test INV-1..6 | Không | Không | Không (nhưng phải viết lại lớp glue) |
| Diff màu trong DataGrid 100k dòng | Tự làm được (Run + DynamicResource) | Được nhưng phải ép style của thư viện | Tốt nhưng công sức chuyển đổi rất lớn |
| Mica / dark mode / accent hệ thống | `DwmSetWindowAttribute` + đọc registry (`AppsUseLightTheme`, `AccentColor`) | Có sẵn một phần | Có sẵn |
| Rủi ro bịa đặt "đã làm" | Không | Trung bình | Cao |
| Chi phí hiệu năng | 0 | +10–20 MB gói | +runtime |

**Lý do chọn:** plan (mục 6) cho phép "WinUI 3 ưu tiên; **nếu giữ WPF thì dùng thư viện Fluent**". Ta chọn hướng *WPF + theme tự thân* vì:
1. Mọi mục UI trong plan (CommandBar 1 hàng, thẻ quy tắc accordion, chip trạng thái, diff màu, InfoBar) đều là **mẫu bố cục**, không phụ thuộc thư viện — chỉ cần style + ResourceDictionary.
2. `DataGrid` của WPF đã ảo hóa, phù hợp mục tiêu 100.000 dòng; thư viện Fluent thường **làm hỏng** style DataGrid/SystemColors (điểm rủi ro High Contrast mà QA-17 đã kiểm).
3. Không thêm phụ thuộc NuGet → không rủi ro đóng gói/size, không rủi ro Store certification ở Giai đoạn 2/3 đang mở.
4. Mica/dark/accent vẫn đạt được bằng Win32 (`DwmSetWindowAttribute`) + registry, không cần Windows App SDK.

**Rủi ro đã biết & cách giảm**

| Rủi ro | Giảm thiểu |
| :--- | :--- |
| Mica chỉ có trên Windows 11 22H2+ | Phát hiện phiên bản Windows; Win10 dùng nền đặc, trải nghiệm vẫn nhất quán |
| `WindowStyle=None` + `WindowChrome` dỡ vỡ khi resize/DPI | **Thực tế: giữ thanh tiêu đề hệ thống** (Windows 11 đã tự vẽ Mica cho thanh tiêu đề) + thêm **hàng tiêu đề riêng trong cửa sổ** (tên app · badge Pro/Free · Cài đặt). Mica được bật best-effort qua DWM; nếu không hiện thì nền đặc vẫn đúng theme. |
| 4 bộ chuỗi phải đồng bộ | Test `LanguageResources_AllLanguagesHaveSameKeys` chạy trong CI, mọi khoá mới phải có đủ 4 file |
| Code-behind hiện đang phình to | Tách `ViewModels/` theo plan mục 7; code-behind chỉ còn wiring view ↔ viewmodel |

---

## 3. Nội dung triển khai theo từng chặng (kế hoạch thực tế)

| Chặng | Nội dung | Trạng thái |
| :--- | :--- | :---: |
| **U0** | Audit + quyết định công nghệ + design token | ✅ tài liệu này |
| **U1** | Theme.xaml (token sáng/tối), Mica, TitleBar tùy biến, CommandBar 1 hàng, bố cục 2 cột có `GridSplitter`, thanh hành động dưới, gom tính năng nâng cao vào menu "⋯ Thêm", chuyển Ngôn ngữ/Theme/Cập nhật/Pro sang trang Cài đặt | ✅ |
| **U2** | Rule Builder: thẻ quy tắc có công tắc bật/tắt, mô tả 1 dòng, **accordion sửa tham số tại chỗ** (thay hộp thoại), nút thêm quy tắc, menu preset (Lưu/Nhập/Xuất/Quản lý) | ✅ (kéo-thả đổi thứ tự thay bằng nút ↑/↓ để bàn phím thao tác được — ghi nhận nợ U2) |
| **U3** | Bảng xem trước: cột chọn, diff màu (thêm/xóa), chip trạng thái, lọc + tìm nhanh (`Ctrl+F`), cột Thư mục, ngăn Rule Trace dạng timeline, kéo-thả thêm tệp | ✅ (kéo-thả **sắp xếp thủ công** theo thứ tự còn nợ) |
| **U4** | Thanh hành động: chip lọc bấm được, Áp dụng nêu số tệp + vô hiệu hóa thông minh, hộp xác nhận tóm tắt, progress + Hủy, InfoBar thành công, Hoàn tác | ✅ |
| **U5** | Màn phụ: Lịch sử, Thư viện preset, Watch Folder, Menu chuột phải, Xuất báo cáo, trang Giấy phép Pro → tất cả gom vào menu "⋯ Thêm" + trang Cài đặt | ✅ (Lịch sử/Thư viện/Watch giữ nguyên chức năng, chỉ đổi vị trí truy cập) |
| **U6** | Empty state (kéo thả + 2 nút + 3 preset gợi ý), phím tắt, mật độ bảng, kiểm tra Narrator/High Contrast | ✅ (empty state + phím tắt) |
| **U7** | Test hồi quy INV-1..INV-6, test ViewModel, ảnh Store mới (sáng + tối) | ✅ (test ViewModel mới + ảnh sáng/tối) |

---

## 4. Quy tắc bất biến khi rebuild

1. **Không sửa logic trong `BFR.Core`** — `RulePipeline`, `RenamePlanner`, `ConflictDetector`, `RenameExecutor`, journal, EXIF đọc… giữ nguyên. UI chỉ gọi qua API công khai.
2. **Kết quả 149 test (142 unit + 7 property) phải giữ nguyên** sau mỗi chặng; INV-1..INV-6 không đổi. *(Xác nhận sau rebuild: 149/149 pass + 14 test ViewModel mới.)*
3. **Không hard-code chuỗi** trong view: mọi chuỗi mới phải có trong cả 4 file `Strings.*.xaml` (kiểm tra tự động bằng test khoá).
4. **Không hard-code màu** trong view: mọi màu lấy từ token `Theme.xaml` (sáng/tối).
5. Mọi control tương tác có `AutomationProperties.Name`; thứ tự Tab hợp lý; tương phản ≥ 4.5:1; hỗ trợ High Contrast.

---

Báo cáo kết quả, ảnh chụp và danh sách nợ còn lại: `docs/UI_REBUILD_REPORT.md`.
