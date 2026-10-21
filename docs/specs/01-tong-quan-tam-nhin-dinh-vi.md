# 01. TỔNG QUAN, TẦM NHÌN VÀ ĐỊNH VỊ

Nguồn: `docs/batch_file_renamer_spec_v1.0.md` — Phần 1 (Tổng quan, tầm nhìn và định vị) và Phần 2 (Phân tích thị trường, người dùng và đối thủ).

---

## 1.1 Tuyên bố sản phẩm

**Batch File Renamer (BFR)** là công cụ đổi tên hàng loạt tệp và thư mục trên Windows, xây quanh **một chuỗi quy tắc xếp chồng (rule chain)** với **xem trước kết quả theo thời gian thực** trước khi áp dụng.

Luồng chuẩn: người dùng kéo thả tệp vào → xếp các quy tắc (Regex, số thứ tự, ngày tạo, cắt tỉa ký tự, đổi chữ hoa/thường, bỏ dấu tiếng Việt…) → nhìn bảng "Tên cũ → Tên mới" cập nhật tức thì → bấm Áp dụng.

### Ba lời hứa của sản phẩm
1. **Thấy trước khi làm:** Preview chính xác 100% so với kết quả thật (cùng một hàm tính toán, không có đường "ước lượng").
2. **Không bao giờ mất tệp:** Phát hiện xung đột trước khi chạy, thực thi theo nhật ký ghi trước (write-ahead journal), có khôi phục sau sự cố và Undo nhiều bước.
3. **Dùng được ngay, mạnh khi cần:** Người mới dùng được trong 30 giây; người dùng nâng cao có Regex đầy đủ, chuỗi quy tắc, preset và dòng lệnh.

---

## 1.2 Mục tiêu và Phi mục tiêu (Goals / Non-goals)

| Mục tiêu v1.x | Phi mục tiêu (không làm trong v1.x) |
| :--- | :--- |
| Đổi tên tệp/thư mục **trong cùng thư mục** theo quy tắc | Di chuyển tệp sang thư mục khác (dấu `\` trong mẫu tên bị chặn) |
| Preview, phát hiện xung đột, Undo | Trình quản lý tệp đầy đủ (copy, move, delete) |
| Regex .NET, số thứ tự, ngày tháng, cắt tỉa, đổi hoa/thường, bỏ dấu | Đổi tên theo nội dung tệp bằng AI/OCR (có thể xem xét sau) |
| Tích hợp menu chuột phải của Explorer, CLI | Đồng bộ đám mây, tài khoản người dùng |
| Chạy hoàn toàn cục bộ, không gửi dữ liệu | Hỗ trợ macOS/Linux ở v1.x |

---

## 1.3 Bài toán người dùng (Problem Statement)

1. **Tên tệp lộn xộn sau khi tải/xuất:** `IMG_20240101_123456 (1).jpg`, `Bao cao final FINAL v2 (copy).docx`, tên có khoảng trắng thừa, ký tự lạ, có dấu và không dấu lẫn lộn.
2. **Cần thứ tự chuẩn:** đánh số `001, 002, 003…` để sắp xếp đúng thứ tự trong video, ảnh, chương truyện, bài giảng.
3. **Cần tên theo ngày:** ảnh/tài liệu đặt theo `YYYY-MM-DD_...` dựa trên ngày chụp hoặc ngày tạo.
4. **Công cụ có sẵn chưa đủ:** Đổi tên nhiều tệp bằng F2 của Explorer chỉ tạo dạng `Tên (1)`, `Tên (2)`. Công cụ mạnh thì giao diện nặng nề, khó dùng với người mới, hoặc preview không đáng tin.
5. **Sợ rủi ro:** đổi nhầm hàng trăm tệp mà không khôi phục được; hai tệp bị trùng tên và một tệp bị ghi đè hoặc lỗi giữa chừng.

---

## 1.4 Kịch bản sử dụng tiêu biểu

| Mã | Người dùng | Kịch scenario | Cách BFR giải quyết |
| :-: | :--- | :--- | :--- |
| UC-1 | Sinh viên | Hàng chục file bài giảng `Bai 1.pdf`, `Bai 10.pdf`, `Bai 2.pdf` sắp xếp sai | Quy tắc Số thứ tự với natural sort, đệm 0: `Bai 01.pdf`… |
| UC-2 | Nhiếp ảnh/nội dung | Ảnh `DSC_0001.JPG` cần đặt theo ngày chụp | Quy tắc Ngày tháng lấy EXIF `DateTimeOriginal`, dự phòng ngày tạo: `2024-05-01_001.jpg` |
| UC-3 | Người dùng văn phòng | Tên tệp có `(1)`, `- Copy`, khoảng trắng thừa | Quy tắc Cắt tỉa và Regex preset "Dọn dẹp tên tệp" |
| UC-4 | Người Việt | Tên tệp tiếng Việt có dấu gây lỗi khi tải lên hệ thống khác | Quy tắc Bỏ dấu (xử lý đúng `đ/Đ`) + thay khoảng trắng bằng `-` |
| UC-5 | Lập trình viên | Đổi hàng loạt theo mẫu `^(\d+)_(.+)\.log$` thành `$2_$1.log` | Regex có nhóm bắt (capture group), tester trực tiếp |
| UC-6 | Người quản lý tệp định kỳ | Thư mục tải về mỗi tuần cần dọn tên tự động | Preset + Watch Folder (Pro, v1.2) |

---

## 1.5 Phân tích thị trường và người dùng

### Chân dung người dùng

| Nhóm | Đặc điểm | Nhu cầu chính | Khả năng chi trả |
| :--- | :--- | :--- | :--- |
| Sinh viên, nhân viên văn phòng | Không rành Regex | Preset sẵn, thao tác kéo thả, preview rõ | Thấp (cần bản Free đủ dùng) |
| Nhiếp ảnh, người làm nội dung | Hàng nghìn ảnh/video | Ngày EXIF, số thứ tự, tốc độ | Trung bình |
| Lập trình viên, quản trị dữ liệu | Rành Regex, dùng dòng lệnh | Regex đầy đủ, CLI, dry-run, script | Trung bình đến cao |

### Ma trận đối thủ (cần xác minh lại khi bắt đầu code)

| Giải pháp | Điểm mạnh | Điểm yếu cần lưu ý |
| :--- | :--- | :--- |
| **PowerToys PowerRename** | Miễn phí, mã nguồn mở, Regex, preview, tích hợp menu chuột phải | Đối thủ mạnh nhất về giá; ít tính năng số thứ tự/ngày nâng cao, ít preset, undo hạn chế |
| Bulk Rename Utility | Rất mạnh, đủ mọi tùy chọn | Giao diện dày đặc, khó với người mới |
| Advanced Renamer, ReNamer | Chuỗi phương thức (method chain), preset | Giao diện cũ hoặc giới hạn bản miễn phí |
| Total Commander (Multi-Rename Tool) | Quen thuộc với người dùng lâu năm | Phải dùng cả trình quản lý tệp |
| Explorer (F2 nhiều tệp) | Có sẵn | Chỉ đổi thành `Tên (1)`, `Tên (2)` |

### Nhận định thẳng thắn

Thị trường đã có công cụ miễn phí tốt. BFR **không thể cạnh tranh bằng "có Regex + preview"**. Khác biệt phải đến từ:

1. **Độ tin cậy có thể chứng minh:** preview = kết quả thật, phát hiện xung đột đầy đủ, journal + Undo nhiều bước + khôi phục sau sự cố.
2. **Chuỗi quy tắc trực quan có "Rule Trace"** (xem kết quả sau từng bước để gỡ lỗi), tốt hơn một ô Regex duy nhất.
3. **Tối ưu cho tiếng Việt:** bỏ dấu đúng (`đ/Đ`), chuẩn hóa Unicode NFC/NFD, preset tiếng Việt, giao diện tiếng Việt chất lượng.
4. **Số thứ tự và ngày tháng thông minh:** natural sort, đánh số lại theo từng thư mục, EXIF/ngày tạo với chuỗi dự phòng.
5. **Trải nghiệm hiện đại, nhẹ** (Fluent) và CLI cho người dùng nâng cao.

---

## 1.6 Định vị

> "Đổi tên hàng trăm tệp một cách tự tin: xem trước chính xác, không lo trùng tên, hoàn tác bất cứ lúc nào."

---

## 1.7 Giả định và quy ước

- **Giả định nền tảng:** Tài liệu giả định Windows (đồng bộ với Quick Audio Switcher (QAS) và Window Pin & Dimmer (WPD)). Nếu muốn đa nền tảng (macOS/Linux), xem phương án Avalonia ở mục 5.1; khi đó phần tích hợp menu chuột phải phải viết lại theo từng hệ điều hành.
- **Quy ước:** Mọi con số hiệu năng và kinh doanh là **mục tiêu thiết kế (target)**, chưa phải số đo; chúng được xác minh ở Phần 12 và Giai đoạn 0 của lộ trình. Thông tin về đối thủ cần xác minh lại khi bắt đầu code.
- BFR cùng họ tiện ích Windows với QAS và WPD; dùng chung thư viện hạ infrastructure (Settings, Licensing, Packaging, CI).
