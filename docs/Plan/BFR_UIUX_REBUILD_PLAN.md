# Batch File Renamer — Kế hoạch rebuild UI/UX (v2.0)

> Phạm vi: chỉ lớp giao diện/trải nghiệm. `BFR.Core` (RulePipeline, RenamePlanner, ConflictDetector, RenameExecutor + journal) **giữ nguyên**, UI chỉ gọi qua interface. Mọi thay đổi UI không được làm đổi kết quả test INV-1..INV-6.

---

## 1. Mục tiêu

1. Người dùng mới đổi tên được 1 lô tệp **trong dưới 30 giây, không cần đọc hướng dẫn**.
2. Luồng chính rõ ràng: **Thêm tệp → Chọn/Chỉnh quy tắc → Xem trước → Áp dụng → (Hoàn tác)**.
3. Giao diện đạt chuẩn Windows 11 (Fluent, Mica, bo góc, dark/light theo hệ thống) để qua review Microsoft Store và nhìn "native".
4. Tính năng nâng cao (Watch Folder, Thư viện preset, Menu chuột phải, Lịch sử, Xuất báo cáo) vẫn truy cập được nhưng **không chiếm chỗ của luồng chính**.
5. Hỗ trợ 10.000+ tệp mượt (ảo hóa danh sách), thao tác bằng bàn phím đầy đủ, đạt WCAG AA.

---

## 2. Audit giao diện hiện tại (từ ảnh chụp)

| # | Vấn đề | Hậu quả |
|---|--------|---------|
| 1 | Thanh công cụ 2 hàng, ~17 nút cùng kích thước, cùng độ nổi (Thêm tệp ngang hàng với Xuất báo cáo, Kiểm tra cập nhật) | Không có điểm nhìn chính, người mới bị choáng |
| 2 | Nút "Free", "Nâng cấp Pro", chọn ngôn ngữ nằm lẫn trong thanh công cụ thao tác | Lẫn giữa tài khoản/giấy phép và công việc |
| 3 | Panel "CHUỖI QUY TẮC" chỉ hiện chữ "Numbering", muốn sửa phải bấm "Sửa quy tắc" mở hộp thoại riêng | Chỉnh quy tắc → xem kết quả bị đứt mạch, panel chiếm 1/3 màn hình nhưng gần như trống |
| 4 | Bảng Tên cũ / Tên mới / Trạng thái / Chi tiết: không highlight phần thay đổi, cột "Chi tiết" trống, "Sẽ đổi" lặp 14 lần | Khó thấy sự khác biệt, tốn chiều ngang vô ích |
| 5 | "Rule Trace" dùng font monospace, chỉ hiện sau khi chọn dòng, chữ tiếng Anh lẫn Việt | Tính năng hay nhưng bị giấu và thiếu đồng bộ |
| 6 | Thanh trạng thái dưới chỉ là một dòng chữ liền ("14 tệp | Sẽ đổi 14 | Không đổi 0 | Lỗi 0") | Không bấm để lọc được, lỗi không nổi bật |
| 7 | Control mặc định kiểu cũ (nút xám phẳng, checkbox/combobox cổ), không có icon, không Mica/dark mode | Không giống app Windows 11, kém tin cậy với người dùng và reviewer |
| 8 | Không thấy empty state, vùng kéo-thả, hay gợi ý lần đầu | Mở app lần đầu không biết bắt đầu từ đâu |
| 9 | Thứ tự đánh số (vd. `Anh minh hoa` ra 004 trong khi `Bai giang` ra 001) không được giải thích trên UI | Người dùng khó hiểu kết quả; cần hiện rõ "Sắp xếp theo" |
| 10 | Nút "Ghi" nguy hiểm duy nhất là ÁP DỤNG nhưng không có xác nhận mức rủi ro, tóm tắt, hay thông báo thành công/Hoàn tác nổi bật | Dễ bấm nhầm, khó tin tưởng |

---

## 3. Nguyên tắc thiết kế

1. **Một hành động chính trên mỗi màn hình** (primary button duy nhất: Áp dụng).
2. **Xem trước luôn sống**: đổi bất kỳ tham số quy tắc nào → bảng tên mới cập nhật ngay (debounce 150 ms).
3. **Hiện sự thay đổi, không hiện kết luận**: tô màu phần bị xóa/thêm trong tên, thay vì chỉ ghi "Sẽ đổi".
4. **Nâng cao ẩn dần (progressive disclosure)**: dùng menu, ngăn kéo, hộp thoại — không đặt ở thanh chính.
5. **An toàn trước, tiện sau**: xung đột/lỗi chặn nút Áp dụng và chỉ rõ dòng nào; luôn có Hoàn tác.
6. **Bàn phím là công dân hạng nhất**: mọi thao tác có phím tắt, thứ tự Tab hợp lý.
7. **Native Windows 11**: Fluent controls, Segoe UI Variable, theo accent color/theme hệ thống.

---

## 4. Kiến trúc thông tin (IA)

```
Cửa sổ chính
├─ Thanh tiêu đề (Mica) : logo · tên app · [Pro/Free badge] · [⚙ Cài đặt]
├─ Thanh lệnh (CommandBar, 1 hàng):
│     [＋ Thêm ▾ (Tệp / Thư mục / Kéo thả)]  [Quét thư mục con ☐] [Gồm cả thư mục ☐]   …   [Xóa danh sách]  [⋯ Thêm]
│     └─ ⋯ Thêm: Theo dõi thư mục · Menu chuột phải · Lịch sử · Xuất báo cáo · Thư viện preset · Nhập/Lưu preset
├─ Vùng làm việc (chia 2 cột, kéo được):
│     ├─ TRÁI (360 px): Rule Builder  (preset + chuỗi thẻ quy tắc, sửa trực tiếp tại chỗ)
│     └─ PHẢI: Bảng xem trước (Tên cũ → Tên mới, diff màu) + khung Chi tiết quy tắc (flyout/ngăn dưới)
└─ Thanh hành động dưới: [chips: 14 tệp · 14 sẽ đổi · 0 không đổi · 0 lỗi]    [Hoàn tác]  [Áp dụng 14 tệp ▶]
Cài đặt (trang riêng): Ngôn ngữ · Giao diện (Sáng/Tối/Hệ thống) · Cập nhật · Giấy phép Pro · Quyền riêng tư
```

Chuyển các mục không thuộc luồng chính khỏi thanh công cụ:

| Hiện tại | Chuyển tới |
|----------|-----------|
| Free / Nâng cấp Pro | Badge ở thanh tiêu đề → mở trang Giấy phép trong Cài đặt |
| Tiếng Việt (chọn ngôn ngữ) | Cài đặt → Ngôn ngữ |
| Kiểm tra cập nhật | Cài đặt → Cập nhật |
| Theo dõi thư mục, Menu chuột phải, Thư viện preset, Lịch sử, Xuất báo cáo | Menu "⋯ Thêm" (Lịch sử và Xuất báo cáo cũng có icon riêng ở thanh dưới sau khi áp dụng) |
| Lưu preset / Nhập preset | Menu của combobox Preset |
| Sửa quy tắc | Bỏ nút; sửa trực tiếp trên thẻ quy tắc |

---

## 5. Thiết kế chi tiết từng khu vực

### 5.1 Thanh lệnh
- Nút chính **＋ Thêm** (accent) có SplitButton: Thêm tệp / Thêm thư mục / Dán đường dẫn.
- Hai tùy chọn quét đổi từ checkbox sang **ToggleSwitch nhỏ** hoặc chip bật/tắt, kèm tooltip giải thích.
- Phím tắt: `Ctrl+O` thêm tệp, `Ctrl+Shift+O` thêm thư mục, `Del` xóa dòng chọn, `Ctrl+Shift+Del` xóa danh sách.

### 5.2 Rule Builder (cột trái) — thay hộp thoại "Sửa quy tắc"
- Đầu cột: combobox **Preset** (có icon, nhóm "Của tôi" / "Có sẵn"), menu "…" gồm Lưu thành preset mới, Nhập, Xuất, Quản lý.
- Dưới: danh sách **thẻ quy tắc** có thể kéo để đổi thứ tự (Find/Replace, Regex, Numbering, Trim, Ngày tạo…).
  - Mỗi thẻ: tay cầm kéo ⠿ · tên + tóm tắt 1 dòng ("Đánh số bắt đầu 1, 3 chữ số") · công tắc bật/tắt · nút xóa.
  - Bấm thẻ → **mở rộng tại chỗ** (accordion) hiện form tham số; không mở cửa sổ mới.
  - Thẻ Regex: ô nhập có tô cú pháp, báo lỗi regex ngay dưới ô, nút "Mẫu thường dùng", ô thử nhanh trên tên chọn.
  - Thẻ Numbering: Bắt đầu từ · Bước nhảy · Số chữ số · Vị trí (đầu/cuối/thay tên) · **Sắp xếp theo** (Tên / Ngày tạo / Ngày sửa / Kích thước / Thứ tự hiện tại) · "Đánh số lại theo từng thư mục".
- Nút **＋ Thêm quy tắc** cuối danh sách → flyout lưới icon các loại quy tắc.
- Empty state: "Chưa có quy tắc. Chọn một preset hoặc thêm quy tắc đầu tiên."

### 5.3 Bảng xem trước (cột phải)
- Cột: ☐ chọn · icon loại tệp · **Tên cũ** · ➜ · **Tên mới** · Trạng thái (icon + chữ) · Thư mục (ẩn mặc định, bật được).
- **Diff màu** trong "Tên mới": phần thêm = nền xanh nhạt, phần bị xóa trong "Tên cũ" = nền đỏ nhạt gạch ngang (không chỉ dựa vào màu: có thêm underline/strike để đạt a11y).
- Trạng thái dùng icon + chip: ✔ Sẽ đổi · ═ Không đổi · ⚠ Xung đột · ✖ Lỗi. Hàng lỗi/xung đột tô viền trái đỏ/cam và **ghim lên đầu** khi bật bộ lọc "Chỉ vấn đề".
- Cột "Chi tiết" bỏ; nội dung chuyển vào tooltip + khung Rule Trace.
- Hỗ trợ: sắp xếp theo cột, tìm/lọc nhanh (`Ctrl+F`), chọn nhiều, bỏ chọn tệp để loại khỏi lần áp dụng, kéo-thả đổi thứ tự thủ công (ảnh hưởng đánh số), chuột phải: Mở thư mục chứa, Bỏ khỏi danh sách, Đặt tên thủ công.
- Ảo hóa hàng (virtualization), mục tiêu 100.000 dòng cuộn 60 fps.
- Kéo-thả tệp/thư mục từ Explorer vào bất cứ đâu trong cửa sổ → overlay "Thả để thêm".

### 5.4 Rule Trace
- Thay khung monospace cuối cửa sổ bằng **ngăn Chi tiết** thu/mở được dưới bảng: hiển thị từng bước theo dạng timeline
  `Tên gốc → [Find/Replace] → … → [Numbering] → Tên cuối`, mỗi bước 1 dòng, phần đổi được tô màu.
- Chọn dòng nào, ngăn cập nhật theo dòng đó. Mặc định thu gọn, có chỉ báo "Chọn một dòng để xem vết".

### 5.5 Thanh hành động dưới
- Trái: **chip trạng thái bấm để lọc** (Tất cả 14 · Sẽ đổi 14 · Không đổi 0 · Lỗi 0). Lỗi >0 chip đỏ.
- Phải: **Hoàn tác** (secondary, vô hiệu khi không có journal) và **Áp dụng 14 tệp** (accent, nêu số tệp thực sự sẽ đổi).
- Vô hiệu hóa Áp dụng khi: không có tệp đổi, còn xung đột/lỗi chưa xử lý (kèm tooltip lý do và nút "Xem lỗi").
- Sau khi áp dụng: **InfoBar thành công** "Đã đổi tên 14 tệp · [Hoàn tác] · [Xem lịch sử] · [Xuất báo cáo]" (tự ẩn sau 10 s, không chặn).
- Đổi trên ≥ 200 tệp hoặc đổi phần mở rộng → hộp xác nhận ngắn tóm tắt số tệp và ví dụ 3 dòng.
- Tiến trình: thanh progress + nút Hủy khi lô lớn, có thể hủy giữa chừng (journal đảm bảo Hoàn tác phần đã làm).

### 5.6 Các màn phụ
- **Lịch sử**: danh sách các lần áp dụng (thời gian, số tệp, thư mục), mở rộng xem chi tiết, nút Hoàn tác từng lần.
- **Thư viện preset**: lưới thẻ có tên, mô tả, ví dụ trước/sau; tìm kiếm; Dùng ngay / Sửa / Xóa / Xuất.
- **Theo dõi thư mục (Watch Folder)**: danh sách thư mục đang theo dõi, công tắc bật/tắt, preset gán cho mỗi thư mục, nhật ký sự kiện gần nhất, cảnh báo khi thư mục nằm trên OneDrive/ổ mạng.
- **Menu chuột phải Explorer**: trang bật/tắt tích hợp với mô tả rõ, trạng thái hiện tại.
- **Xuất báo cáo**: hộp thoại chọn định dạng (CSV/HTML), phạm vi (lô hiện tại/lịch sử), nút mở thư mục sau xuất.
- **Cài đặt**: ngôn ngữ (Việt/Anh/…, đổi tức thì), giao diện, cập nhật, giấy phép (Free/Pro, nút mua đi tới Store), quyền riêng tư (liên kết PRIVACY_POLICY).

### 5.7 Onboarding & empty state
- Mở app lần đầu, danh sách rỗng: vùng giữa hiện hình minh họa + chữ **"Kéo thả tệp hoặc thư mục vào đây"** + 2 nút (Thêm tệp / Thêm thư mục) + 3 preset gợi ý bấm một lần (Đánh số theo thứ tự, Xóa khoảng trắng thừa, Thêm ngày vào tên).
- Teaching tip 3 bước (có thể bỏ qua, chỉ hiện 1 lần): Thêm tệp → Chọn quy tắc → Áp dụng.

### 5.8 Giới hạn bản Free / gợi ý nâng cấp
- Không chặn cứng trong luồng chính; khi chạm giới hạn Free hiện InfoBar nhẹ "Tính năng này thuộc bản Pro" kèm nút xem gói. Không popup chặn.

---

## 6. Hệ thống thị giác

| Hạng mục | Quy định |
|----------|----------|
| Nền tảng | WinUI 3 (Windows App SDK) ưu tiên; nếu giữ WPF thì dùng thư viện Fluent (WPF UI/ModernWpf). **Quyết định ở Giai đoạn 0** |
| Vật liệu | Mica cho nền cửa sổ, Acrylic cho flyout |
| Font | Segoe UI Variable; cỡ 12 (nội dung bảng), 14 (thân), 20 (tiêu đề trang); font monospace chỉ dùng cho ô regex |
| Màu | Dùng accent hệ thống; màu ngữ nghĩa: thành công xanh lá, cảnh báo cam, lỗi đỏ, thêm xanh dương nhạt, xóa đỏ nhạt (đều có biến thể sáng/tối) |
| Khoảng cách | Lưới 4/8 px, padding vùng 16 px, chiều cao hàng bảng 32 px (mật độ chặt: 28 px, tùy chọn) |
| Bo góc | 8 px cho thẻ/panel, 4 px cho control |
| Icon | Segoe Fluent Icons, 16 px trong bảng, 20 px trong thanh lệnh |
| Chuyển động | 150–200 ms, tôn trọng "Hiệu ứng hoạt hình" tắt của Windows |
| Theme | Sáng / Tối / Theo hệ thống, có High Contrast |

Thiết kế token trong ResourceDictionary duy nhất (`Theme.xaml`), cấm hard-code màu trong view.

---

## 7. Kỹ thuật triển khai (UI)

- **Mẫu MVVM**: View → ViewModel → `BFR.Core`. Không logic đổi tên trong code-behind.
- ViewModel chính: `MainViewModel`, `RuleBuilderViewModel`, `RuleCardViewModel` (một lớp cho mỗi loại quy tắc), `PreviewGridViewModel`, `ApplyBarViewModel`, `HistoryViewModel`, `PresetLibraryViewModel`, `WatchFolderViewModel`, `SettingsViewModel`.
- Xem trước: chạy `RenamePlanner` trên luồng nền, hủy lượt cũ khi tham số đổi (CancellationToken), cập nhật UI theo lô để không treo.
- Thu thập tệp, quét thư mục con, đọc EXIF: bất đồng bộ, có progress.
- Danh sách: `ItemsRepeater`/`ListView` ảo hóa (WinUI) hoặc `VirtualizingStackPanel` (WPF).
- Localization: mọi chuỗi nằm trong `.resw`/`.resx`, **không** hard-code; kiểm tra tràn chữ với tiếng Anh/Việt/chuỗi dài.
- Lưu trạng thái cửa sổ, độ rộng cột, theme, ngôn ngữ, preset gần nhất.
- Trợ năng: `AutomationProperties.Name` cho mọi control, thứ tự Tab, focus visual rõ, tương phản ≥ 4.5:1, kiểm tra bằng Narrator và Accessibility Insights.
- Kiểm thử UI: unit test ViewModel, snapshot test các trạng thái chính, smoke test tự động (FlaUI/WinAppDriver) cho luồng Thêm → Preview → Áp dụng → Hoàn tác.

---

## 8. Lộ trình & công việc

| Giai đoạn | Thời lượng | Công việc | Tiêu chí nghiệm thu |
|-----------|-----------|-----------|---------------------|
| **U0 — Chốt thiết kế** | 3–4 ngày | Chốt WinUI 3 hay WPF-Fluent; wireframe Figma 6 màn (Chính, Rỗng, Có lỗi, Lịch sử, Preset, Cài đặt); design tokens; danh sách chuỗi cần dịch | Wireframe được duyệt; quyết định công nghệ ghi trong `docs/` |
| **U1 — Khung & Theme** | 4–5 ngày | Cửa sổ Mica, TitleBar, CommandBar 1 hàng, bố cục 2 cột, Theme.xaml, Cài đặt (ngôn ngữ/theme), gom nút nâng cao vào menu ⋯ | Không còn thanh công cụ 2 hàng; dark/light chạy đúng; đổi ngôn ngữ tức thì |
| **U2 — Rule Builder** | 6–7 ngày | Thẻ quy tắc kéo-thả, accordion, form cho Find/Replace, Regex (kiểm lỗi trực tiếp), Numbering (kèm "Sắp xếp theo"), Trim, Ngày tạo; combobox preset + menu | Không còn hộp thoại "Sửa quy tắc"; đổi tham số → Preview cập nhật < 200 ms với 1.000 tệp |
| **U3 — Bảng xem trước** | 6–7 ngày | Cột mới, diff màu, chip trạng thái, lọc/tìm/sắp xếp, ảo hóa, kéo-thả thêm tệp, chuột phải, ngăn Rule Trace dạng timeline | 100.000 dòng cuộn mượt; lỗi/xung đột nhìn thấy ngay; a11y không dựa vào màu |
| **U4 — Áp dụng & Hoàn tác** | 4 ngày | Thanh hành động dưới, vô hiệu hóa thông minh, xác nhận lô lớn, tiến trình + Hủy, InfoBar thành công, Hoàn tác | Luồng 5 bước chạy; hủy giữa chừng vẫn Hoàn tác được |
| **U5 — Màn phụ** | 6 ngày | Lịch sử, Thư viện preset, Watch Folder, Menu chuột phải, Xuất báo cáo, trang Giấy phép Pro | Tất cả tính năng cũ truy cập được từ vị trí mới, không mất chức năng |
| **U6 — Onboarding & hoàn thiện** | 4 ngày | Empty state, teaching tip, phím tắt, tối ưu mật độ, animation, bản địa hóa đủ, kiểm tra High Contrast/Narrator | Người dùng thử mới hoàn thành tác vụ đầu tiên < 30 s (test với 5 người) |
| **U7 — QA & Store** | 4–5 ngày | Test hồi quy INV-1..INV-6, test UI tự động, ảnh chụp Store mới (sáng + tối), cập nhật mô tả, phát hành v2.0 | Qua kiểm tra WACK; ảnh Store và mô tả khớp UI mới |

Tổng ước tính: **~5–6 tuần** cho một lập trình viên (đã gồm dự phòng 20–30%).

---

## 9. Số đo thành công

- Thời gian hoàn thành lần đổi tên đầu tiên của người mới: **< 30 s** (hiện chưa đo — đo ở U0 làm mốc).
- Số click từ mở app đến áp dụng: **≤ 5**.
- Độ trễ cập nhật xem trước: **< 200 ms** (1.000 tệp), **< 1 s** (10.000 tệp).
- Không có control nào truy cập được chỉ bằng chuột; điểm Accessibility Insights: 0 lỗi nghiêm trọng.
- Đánh giá Store và phản hồi: theo dõi sao trung bình và số báo lỗi giao diện sau 30 ngày.

---

## 10. Rủi ro & cách giảm

| Rủi ro | Cách giảm |
|--------|-----------|
| Đổi framework (WPF → WinUI 3) tốn công đóng gói/MSIX | Quyết định ở U0 bằng prototype 1 ngày; nếu rủi ro cao, giữ WPF + Fluent library |
| Người dùng cũ quen bố cục cũ | Giữ tên nút/khái niệm cũ, ghi chú "Có gì mới" ở lần mở đầu tiên |
| Diff màu chậm với danh sách lớn | Chỉ tính diff cho hàng đang hiển thị (lazy), cache theo hàng |
| Chuỗi tiếng Việt dài làm vỡ layout | Test với bản dịch dài nhất; dùng bố cục co giãn, ellipsis + tooltip |
| Đưa tính năng vào menu ⋯ làm chúng "biến mất" | Thêm lối tắt trong Command Palette `Ctrl+K` (tìm mọi hành động bằng tên) |

---

## 11. Prompt giao cho coding agent (U0 → U1)

```
Bạn đang làm việc trên repo Batch File Renamer (BatchFileRenamer.sln, BFR.Core + UI).
Mục tiêu: rebuild UI/UX theo file BFR_UIUX_REBUILD_PLAN.md. KHÔNG sửa logic trong BFR.Core;
mọi thay đổi phải giữ nguyên kết quả test INV-1..INV-6.

Bước 1 (U0): Đọc cấu trúc hiện tại của project UI (framework, các View/ViewModel, cách localization).
  - Báo cáo: framework đang dùng, danh sách màn hình/dialog hiện có, nơi chứa chuỗi.
  - Tạo docs/ui-rebuild-decision.md đề xuất WinUI 3 vs giữ WPF + thư viện Fluent, kèm lý do và rủi ro MSIX.
Bước 2 (U1): Sau khi tôi duyệt quyết định:
  - Tạo Theme.xaml (design tokens ở mục 6 của plan), bật Mica, dark/light theo hệ thống.
  - Dựng cửa sổ chính: TitleBar (badge Free/Pro, nút Cài đặt), CommandBar 1 hàng, bố cục 2 cột kéo được,
    thanh hành động dưới. Gom các nút nâng cao vào menu "⋯ Thêm" theo bảng ở mục 4.
  - Chuyển Free/Nâng cấp Pro, ngôn ngữ, Kiểm tra cập nhật sang trang Cài đặt.
  - Mọi chuỗi qua resource, không hard-code. Thêm AutomationProperties.Name cho mọi control.
Yêu cầu: commit nhỏ theo từng khu vực; chạy toàn bộ test sau mỗi bước; chụp ảnh màn hình sáng + tối;
cuối mỗi bước liệt kê việc còn lại và điểm chưa chắc chắn.
```
