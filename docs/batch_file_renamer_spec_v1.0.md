# TÀI LIỆU ĐẶC TẢ KIẾN TRÚC & KẾ HOẠCH PHÁT TRIỂN SẢN PHẨM (PAD)
## DỰ ÁN: BATCH FILE RENAMER (BFR) — WINDOWS 10/11
**Phiên bản tài liệu:** 1.0.0 (Master Blueprint A–Z)
**Nền tảng mục tiêu:** Windows 10 (Build 19041+) và Windows 11 (21H2 trở lên, gồm 24H2)
**Kênh phân phối chính:** Microsoft Store (MSIX); kênh phụ: GitHub Releases (bản portable + CLI)
**Mô hình kinh doanh:** Freemium (lõi miễn phí rộng rãi + mua đứt trọn đời bản Pro qua MS Store IAP)
**Họ sản phẩm:** Cùng họ tiện ích Windows với Quick Audio Switcher (QAS) và Window Pin & Dimmer (WPD); dùng chung thư viện hạ tầng (Settings, Licensing, Packaging, CI)

> **Giả định nền tảng:** Tài liệu giả định Windows (đồng bộ với QAS/WPD). Nếu muốn đa nền tảng (macOS/Linux), xem phương án Avalonia ở mục 5.1; khi đó phần tích hợp menu chuột phải (Phần 11) phải viết lại theo từng hệ điều hành.
>
> **Quy ước:** Mọi con số hiệu năng và kinh doanh là **mục tiêu thiết kế (target)**, chưa phải số đo; chúng được xác minh ở Phần 12 và Giai đoạn 0 của lộ trình. Thông tin về đối thủ cần xác minh lại khi bắt đầu code.

---

## MỤC LỤC
1. PHẦN 1: Tổng quan, tầm nhìn và định vị
2. PHẦN 2: Phân tích thị trường, người dùng và đối thủ
3. PHẦN 3: Đặc tả tính năng chi tiết (Functional Requirements)
4. PHẦN 4: Yêu cầu phi chức năng (NFR)
5. PHẦN 5: Kiến trúc hệ thống tổng thể
6. PHẦN 6: Lõi 1 — Pipeline quy tắc đổi tên (Rule Pipeline) và đặc tả từng quy tắc
7. PHẦN 7: Lõi 2 — Preview, phát hiện xung đột, thực thi an toàn và Undo
8. PHẦN 8: Thiết kế giao diện và trải nghiệm (UI/UX)
9. PHẦN 9: Mô hình dữ liệu, Preset, cấu hình và CLI
10. PHẦN 10: Edge cases, bảo mật, quyền riêng tư và tương thích
11. PHẦN 11: Đóng gói MSIX, tích hợp menu chuột phải, chứng nhận Store và CI/CD
12. PHẦN 12: Chiến lược kiểm thử và ma trận QA
13. PHẦN 13: Thương mại hóa, ASO và Go-To-Market
14. PHẦN 14: Lộ trình phát triển, phạm vi MVP và quản trị rủi ro
15. PHẦN 15: Phụ lục (thuật ngữ, API, mã mẫu, checklist trước khi code)

---

# PHẦN 1: TỔNG QUAN, TẦM NHÌN VÀ ĐỊNH VỊ

## 1.1. Tuyên bố sản phẩm
**Batch File Renamer (BFR)** là công cụ đổi tên hàng loạt tệp và thư mục trên Windows, xây quanh **một chuỗi quy tắc xếp chồng (rule chain)** với **xem trước kết quả theo thời gian thực** trước khi áp dụng. Người dùng kéo thả tệp vào, xếp các quy tắc (Regex, số thứ tự, ngày tạo, cắt tỉa ký tự, đổi chữ hoa/thường, bỏ dấu tiếng Việt...), nhìn bảng "Tên cũ → Tên mới" cập nhật tức thì, rồi mới bấm Áp dụng.

Ba lời hứa của sản phẩm:
1. **Thấy trước khi làm:** Preview chính xác 100% so với kết quả thật (cùng một hàm tính toán, không có đường "ước lượng").
2. **Không bao giờ mất tệp:** Phát hiện xung đột trước khi chạy, thực thi theo nhật ký ghi trước (write-ahead journal), có khôi phục sau sự cố và Undo nhiều bước.
3. **Dùng được ngay, mạnh khi cần:** Người mới dùng được trong 30 giây; người dùng nâng cao có Regex đầy đủ, chuỗi quy tắc, preset và dòng lệnh.

## 1.2. Mục tiêu và Phi mục tiêu (Goals / Non-goals)
| Mục tiêu v1.x | Phi mục tiêu (không làm trong v1.x) |
| :--- | :--- |
| Đổi tên tệp/thư mục **trong cùng thư mục** theo quy tắc | Di chuyển tệp sang thư mục khác (dấu `\` trong mẫu tên bị chặn) |
| Preview, phát hiện xung đột, Undo | Trình quản lý tệp đầy đủ (copy, move, delete) |
| Regex .NET, số thứ tự, ngày tháng, cắt tỉa, đổi hoa/thường, bỏ dấu | Đổi tên theo nội dung tệp bằng AI/OCR (có thể xem xét sau, xem 14.4) |
| Tích hợp menu chuột phải của Explorer, CLI | Đồng bộ đám mây, tài khoản người dùng |
| Chạy hoàn toàn cục bộ, không gửi dữ liệu | Hỗ trợ macOS/Linux ở v1.x |

## 1.3. Bài toán người dùng (Problem Statement)
1. **Tên tệp lộn xộn sau khi tải/xuất:** `IMG_20240101_123456 (1).jpg`, `Bao cao final FINAL v2 (copy).docx`, tên có khoảng trắng thừa, ký tự lạ, có dấu và không dấu lẫn lộn.
2. **Cần thứ tự chuẩn:** đánh số `001, 002, 003...` để sắp xếp đúng thứ tự trong video, ảnh, chương truyện, bài giảng.
3. **Cần tên theo ngày:** ảnh/tài liệu đặt theo `YYYY-MM-DD_...` dựa trên ngày chụp hoặc ngày tạo.
4. **Công cụ có sẵn chưa đủ:** Đổi tên nhiều tệp bằng F2 của Explorer chỉ tạo dạng `Tên (1)`, `Tên (2)`. Công cụ mạnh thì giao diện nặng nề, khó dùng với người mới, hoặc preview không đáng tin.
5. **Sợ rủi ro:** đổi nhầm hàng trăm tệp mà không khôi phục được; hai tệp bị trùng tên và một tệp bị ghi đè hoặc lỗi giữa chừng.

## 1.4. Kịch bản sử dụng tiêu biểu
| Mã | Người dùng | Kịch bản | Cách BFR giải quyết |
| :-: | :--- | :--- | :--- |
| UC-1 | Sinh viên | Hàng chục file bài giảng `Bai 1.pdf`, `Bai 10.pdf`, `Bai 2.pdf` sắp xếp sai | Quy tắc Số thứ tự với natural sort, đệm 0: `Bai 01.pdf`... |
| UC-2 | Nhiếp ảnh/nội dung | Ảnh `DSC_0001.JPG` cần đặt theo ngày chụp | Quy tắc Ngày tháng lấy EXIF `DateTimeOriginal`, dự phòng ngày tạo: `2024-05-01_001.jpg` |
| UC-3 | Người dùng văn phòng | Tên tệp có `(1)`, `- Copy`, khoảng trắng thừa | Quy tắc Cắt tỉa và Regex preset "Dọn dẹp tên tệp" |
| UC-4 | Người Việt | Tên tệp tiếng Việt có dấu gây lỗi khi tải lên hệ thống khác | Quy tắc Bỏ dấu (xử lý đúng `đ/Đ`) + thay khoảng trắng bằng `-` |
| UC-5 | Lập trình viên | Đổi hàng loạt theo mẫu `^(\d+)_(.+)\.log$` thành `$2_$1.log` | Regex có nhóm bắt (capture group), tester trực tiếp |
| UC-6 | Người quản lý tệp định kỳ | Thư mục tải về mỗi tuần cần dọn tên tự động | Preset + Watch Folder (Pro, v1.2) |

---

# PHẦN 2: PHÂN TÍCH THỊ TRƯỜNG, NGƯỜI DÙNG VÀ ĐỐI THỦ

## 2.1. Chân dung người dùng
| Nhóm | Đặc điểm | Nhu cầu chính | Khả năng chi trả |
| :--- | :--- | :--- | :--- |
| Sinh viên, nhân viên văn phòng | Không rành Regex | Preset sẵn, thao tác kéo thả, preview rõ | Thấp (cần bản Free đủ dùng) |
| Nhiếp ảnh, người làm nội dung | Hàng nghìn ảnh/video | Ngày EXIF, số thứ tự, tốc độ | Trung bình |
| Lập trình viên, quản trị dữ liệu | Rành Regex, dùng dòng lệnh | Regex đầy đủ, CLI, dry-run, script | Trung bình đến cao |

## 2.2. Ma trận đối thủ (cần xác minh lại)
| Giải pháp | Điểm mạnh | Điểm yếu cần lưu ý |
| :--- | :--- | :--- |
| **PowerToys PowerRename** | Miễn phí, mã nguồn mở, Regex, preview, tích hợp menu chuột phải | Là đối thủ mạnh nhất về giá; ít tính năng số thứ tự/ngày nâng cao, ít preset, undo hạn chế |
| Bulk Rename Utility | Rất mạnh, đủ mọi tùy chọn | Giao diện dày đặc, khó với người mới |
| Advanced Renamer, ReNamer | Chuỗi phương thức (method chain), preset | Giao diện cũ hoặc giới hạn bản miễn phí |
| Total Commander (Multi-Rename Tool) | Quen thuộc với người dùng lâu năm | Phải dùng cả trình quản lý tệp |
| Explorer (F2 nhiều tệp) | Có sẵn | Chỉ đổi thành `Tên (1)`, `Tên (2)` |

**Nhận định thẳng thắn:** Thị trường đã có công cụ miễn phí tốt. BFR **không thể cạnh tranh bằng "có Regex + preview"**. Khác biệt phải đến từ:
1. **Độ tin cậy có thể chứng minh:** preview = kết quả thật, phát hiện xung đột đầy đủ, journal + Undo nhiều bước + khôi phục sau sự cố.
2. **Chuỗi quy tắc trực quan có "Rule Trace"** (xem kết quả sau từng bước để gỡ lỗi), tốt hơn một ô Regex duy nhất.
3. **Tối ưu cho tiếng Việt:** bỏ dấu đúng (`đ/Đ`), chuẩn hóa Unicode NFC/NFD, preset tiếng Việt, giao diện tiếng Việt chất lượng.
4. **Số thứ tự và ngày tháng thông minh:** natural sort, đánh số lại theo từng thư mục, EXIF/ngày tạo với chuỗi dự phòng.
5. **Trải nghiệm hiện đại, nhẹ** (Fluent) và CLI cho người dùng nâng cao.

## 2.3. Định vị
> "Đổi tên hàng trăm tệp một cách tự tin: xem trước chính xác, không lo trùng tên, hoàn tác bất cứ lúc nào."

---

# PHẦN 3: ĐẶC TẢ TÍNH NĂNG CHI TIẾT (FUNCTIONAL REQUIREMENTS)

## 3.1. Module A — Nạp tệp và bộ lọc
| Mã | Yêu cầu | Chi tiết và tiêu chí chấp nhận |
| :-: | :--- | :--- |
| FR-A1 | Nạp tệp bằng kéo thả, nút "Thêm tệp/Thêm thư mục", dán đường dẫn, hoặc từ menu chuột phải Explorer | Chấp nhận nhiều nguồn cùng lúc; loại trùng lặp. Nạp 10.000 tệp dưới 2 giây trên SSD (không tính EXIF). |
| FR-A2 | Quét thư mục con (đệ quy) | Tùy chọn mức sâu tối đa; có nút hủy khi quét lớn. |
| FR-A3 | Bao gồm/loại trừ | Bộ lọc theo phần mở rộng, mẫu glob (`*.jpg`), Regex, kích thước, ngày tạo/sửa. Mặc định **loại trừ tệp ẩn và tệp hệ thống**. |
| FR-A4 | Đổi tên thư mục | Công tắc "Gồm cả thư mục" (mặc định tắt). Khi bật, xử lý thứ tự từ trong ra ngoài để đường dẫn con không bị hỏng. |
| FR-A5 | Sắp xếp danh sách nạp | Theo tên (natural sort), ngày tạo, ngày sửa, kích thước, ngày EXIF, thứ tự nạp, thứ tự chọn thủ công (kéo thả để đổi vị trí). Thứ tự này quyết định số thứ tự được đánh. |
| FR-A6 | Bỏ chọn từng tệp | Có thể bỏ tích từng dòng để loại khỏi lần đổi tên mà không xóa khỏi danh sách. |

## 3.2. Module B — Chuỗi quy tắc (Rule Chain)
Người dùng xếp một **danh sách quy tắc có thứ tự**; đầu ra của quy tắc trước là đầu vào của quy tắc sau.

| Mã | Quy tắc | Mô tả ngắn |
| :-: | :--- | :--- |
| FR-B1 | **Tìm và thay (Find/Replace)** | Văn bản thuần hoặc Regex; phân biệt/không phân biệt hoa thường; chỉ lần đầu/tất cả; áp dụng lên Tên, Phần mở rộng, hoặc cả hai. |
| FR-B2 | **Regex nâng cao** | Mẫu Regex .NET, chuỗi thay thế dùng `$1`, `${tên}`, token của BFR; có **Regex Tester** với tô sáng nhóm bắt; thời gian chờ mỗi lần khớp để chống treo. |
| FR-B3 | **Số thứ tự (Numbering)** | Bắt đầu từ, bước nhảy, độ rộng đệm 0, vị trí chèn (trước/sau/vị trí N/thay thế toàn bộ), tách dấu ngăn, đặt lại theo từng thư mục hoặc khi tiền tố thay đổi, kiểu số (thập phân, chữ cái a-z, La Mã tùy chọn). |
| FR-B4 | **Ngày tháng (Date)** | Nguồn: ngày tạo, ngày sửa, ngày chụp EXIF, ngày hiện tại, ngày tạo media (video/audio) (Pro). Định dạng tùy chỉnh (ví dụ `yyyy-MM-dd_HHmmss`), chuỗi dự phòng nguồn (EXIF → ngày tạo → ngày sửa), chọn múi giờ (cục bộ/UTC). |
| FR-B5 | **Cắt tỉa (Trim)** | Xóa khoảng trắng đầu/cuối, gộp nhiều khoảng trắng thành một, xóa N ký tự đầu/cuối, xóa ký tự trong khoảng vị trí, xóa ký tự thuộc một tập (ví dụ `()[]{}`), xóa dấu chấm/khoảng trắng cuối tên (vì Windows không cho phép). |
| FR-B6 | **Chèn (Insert)** | Chèn văn bản/token ở đầu, cuối hoặc vị trí N (đếm từ đầu hoặc từ cuối). |
| FR-B7 | **Đổi hoa/thường (Case)** | thường, HOA, Viết Hoa Từng Chữ (Title), Chỉ hoa chữ đầu câu; tùy chọn áp dụng cho Tên, Phần mở rộng hoặc cả hai; danh sách từ ngoại lệ (ví dụ `PDF`, `USB`). |
| FR-B8 | **Bỏ dấu và chuẩn hóa (Sanitize)** | Bỏ dấu tiếng Việt (gồm `đ` thành `d`, `Đ` thành `D`); chuẩn hóa Unicode NFC; thay ký tự không hợp lệ của Windows; thay khoảng trắng bằng `-`/`_`/không; kiểu Slug (chữ thường, chỉ `a-z0-9-`). |
| FR-B9 | **Phần mở rộng (Extension)** | Giữ nguyên (mặc định), chuyển thường, đổi thành giá trị cố định, xóa. **Đổi phần mở rộng cần bật cờ xác nhận rõ ràng** vì có thể làm hỏng liên kết mở tệp. |
| FR-B10 | **Mẫu tên (Template)** | Dựng tên mới từ mẫu bằng token, ví dụ `{date:yyyyMMdd}_{parent}_{n:3}`; thay thế hoàn toàn tên cũ. |
| FR-B11 | Quản lý chuỗi | Thêm, xóa, nhân bản, kéo đổi thứ tự, bật/tắt tạm thời từng quy tắc, đặt tên cho quy tắc. |
| FR-B12 | **Rule Trace** | Xem kết quả **sau từng quy tắc** của một tệp được chọn để biết quy tắc nào gây ra thay đổi hoặc lỗi. |

## 3.3. Hệ thống Token dùng trong chuỗi thay thế và Mẫu tên
| Token | Ý nghĩa | Ví dụ |
| :--- | :--- | :--- |
| `{name}` | Tên hiện tại (không gồm phần mở rộng) | `anh-1` |
| `{ext}` | Phần mở rộng (không gồm dấu chấm) | `jpg` |
| `{orig}` | Tên gốc ban đầu trước mọi quy tắc | `IMG_001` |
| `{n}` hoặc `{n:3}` | Số thứ tự (độ rộng đệm 0) | `007` |
| `{parent}` | Tên thư mục chứa tệp | `Du lich Da Lat` |
| `{created:fmt}` | Ngày tạo theo định dạng `fmt` | `{created:yyyy-MM-dd}` |
| `{modified:fmt}` | Ngày sửa | |
| `{taken:fmt}` | Ngày chụp EXIF (dự phòng theo chuỗi nguồn) | |
| `{now:fmt}` | Thời điểm áp dụng | |
| `{size}` / `{size:KB}` | Kích thước | `2048` |
| `{rand:6}` | Chuỗi ngẫu nhiên N ký tự (có hạt giống cố định trong một lần preview để kết quả ổn định) | `a8f3k2` |

Quy ước: token không hợp lệ hoặc thiếu dữ liệu (ví dụ không có EXIF) **không âm thầm biến mất**; preview hiển thị trạng thái "Thiếu dữ liệu: {taken}" và áp dụng chuỗi dự phòng đã chọn.

## 3.4. Module C — Preview, áp dụng và hoàn tác
| Mã | Yêu cầu | Chi tiết |
| :-: | :--- | :--- |
| FR-C1 | Preview thời gian thực | Mỗi lần đổi quy tắc, bảng "Tên cũ → Tên mới" cập nhật sau độ trễ 150 ms (debounce), có hủy tính toán cũ khi có thay đổi mới. Dòng đổi được tô sáng, dòng không đổi mờ đi. |
| FR-C2 | Trạng thái từng dòng | OK, Không đổi, Xung đột, Tên không hợp lệ, Quá dài, Đang bị khóa, Thiếu dữ liệu. Có bộ lọc xem theo trạng thái. |
| FR-C3 | Chặn áp dụng khi có lỗi chặn | Nút Áp dụng bị vô hiệu hóa nếu còn xung đột/tên không hợp lệ; có lựa chọn "Bỏ qua các dòng lỗi và đổi phần còn lại" (xác nhận rõ ràng). |
| FR-C4 | Áp dụng an toàn | Thực thi theo kế hoạch đã kiểm tra, có journal, thanh tiến trình, hủy giữa chừng (dừng sạch và cho phép hoàn tác phần đã làm). |
| FR-C5 | Hoàn tác nhiều bước (Undo) | Lịch sử các lần áp dụng (Free: 1 bước gần nhất; Pro: không giới hạn trong 30 ngày). Undo kiểm tra tệp còn ở vị trí mới và tên cũ còn trống. |
| FR-C6 | Khôi phục sau sự cố | Khi khởi động, nếu phát hiện journal dở dang (mất điện, crash), hiển thị hộp thoại "Hoàn tất" hoặc "Khôi phục về ban đầu". |
| FR-C7 | Xuất báo cáo | Xuất CSV/JSON bảng thay đổi (tên cũ, tên mới, thời điểm) để lưu hoặc kiểm toán. |
| FR-C8 | Chế độ Dry-run | Chạy toàn bộ quy trình và hiển thị kết quả nhưng **không ghi gì** (đặc biệt cho CLI và script). |

## 3.5. Module D — Preset, tích hợp và tự động hóa
| Mã | Yêu cầu | Chi tiết |
| :-: | :--- | :--- |
| FR-D1 | Preset | Lưu chuỗi quy tắc thành preset có tên; thư viện preset dựng sẵn (xem 9.3); nhập/xuất tệp `.bfrpreset` (JSON). |
| FR-D2 | Menu chuột phải Explorer | Mục "Đổi tên hàng loạt với BFR" cho tệp/thư mục đã chọn; hỗ trợ cả menu mới của Windows 11 (xem Phần 11). |
| FR-D3 | CLI | `bfr rename <đường dẫn...> --preset <tên> [--dry-run] [--recursive] [--yes]` (xem 9.4). |
| FR-D4 | Watch Folder (Pro, v1.2) | Theo dõi một thư mục và tự áp dụng preset cho tệp mới (có độ trễ chờ tệp ghi xong, hạn chế thao tác lặp, nhật ký). |
| FR-D5 | Đa ngôn ngữ | Tiếng Việt và tiếng Anh ở v1.0. |
| FR-D6 | Tự cập nhật | Qua Microsoft Store (bản MSIX); bản portable hiển thị thông báo có bản mới (opt-in). |

## 3.6. Phân tách Free và Pro
Vì PowerRename miễn phí, bản Free **phải đủ tốt để người dùng thông thường không cần trả tiền**, còn Pro bán thứ người dùng thường xuyên và chuyên nghiệp cần.

| Nhóm tính năng | Free | Pro (đề xuất 3,99–4,99 USD mua đứt, giá theo sức mua khu vực) |
| :--- | :---: | :---: |
| Tìm/thay, Regex, Số thứ tự, Ngày (tạo/sửa/hiện tại), Cắt tỉa, Chèn, Hoa/thường, Bỏ dấu, Mẫu tên | Có | Có |
| Preview thời gian thực + phát hiện xung đột + journal + khôi phục sự cố | Có | Có |
| Số quy tắc trong chuỗi | Tối đa 3 | Không giới hạn |
| Undo | 1 lần gần nhất | Lịch sử nhiều bước (30 ngày) |
| Menu chuột phải + kéo thả | Có | Có |
| Ngày chụp EXIF, ngày tạo media (video/audio) | Không (chỉ ngày tạo/sửa) | Có |
| Đệ quy thư mục con, đổi tên thư mục | Giới hạn 2 cấp | Không giới hạn |
| Preset | Dùng preset dựng sẵn, lưu 3 preset | Lưu không giới hạn, nhập/xuất |
| Rule Trace, xuất báo cáo CSV/JSON | Rule Trace có; báo cáo không | Cả hai |
| CLI, Watch Folder | Không | Có |

> **Lý do thiết kế:** giữ chức năng cốt lõi và độ an toàn ở Free để tạo lòng tin và đánh giá tốt; thu phí ở nhu cầu chuyên sâu (EXIF, chuỗi dài, tự động hóa). Không khóa tính năng an toàn (journal, khôi phục) sau thanh toán.

---

# PHẦN 4: YÊU CẦU PHI CHỨC NĂNG (NFR)

| Mã | Hạng mục | Mục tiêu (target) | Cách xác minh |
| :-: | :--- | :--- | :--- |
| NFR-1 | Tốc độ Preview (quy tắc không cần đọc tệp) | 10.000 tệp dưới 300 ms; 100.000 tệp dưới 3 giây | Benchmark tự động trong CI |
| NFR-2 | Tốc độ nạp và quét | 10.000 tệp dưới 2 giây (SSD) | Benchmark thư mục mẫu |
| NFR-3 | Đọc EXIF/media | Song song hóa, có bộ nhớ đệm; 1.000 ảnh dưới 5 giây (SSD) | Benchmark với bộ ảnh thật |
| NFR-4 | Tốc độ áp dụng | 10.000 lần đổi tên trong khoảng 10–30 giây (SSD, đổi tên là thao tác metadata) | Đo thực tế, đặt ngưỡng sau lần đo cơ sở |
| NFR-5 | RAM | Dưới 250 MB ở 100.000 tệp (UI ảo hóa danh sách) | Đo Working Set |
| NFR-6 | Độ chính xác | Kết quả áp dụng **giống hệt** preview ở mọi trường hợp (chung một hàm tính toán) | Test thuộc tính (property test), QA-01 |
| NFR-7 | An toàn dữ liệu | 0 tệp bị mất/ghi đè trong mọi kịch bản kiểm thử, gồm cả mất điện giả lập | QA-04, QA-05 |
| NFR-8 | Phản hồi UI | Không khóa giao diện; mọi tác vụ dài chạy nền và có thể hủy | Kiểm thử thủ công + UI automation |
| NFR-9 | Khởi động | Cửa sổ chính hiện dưới 1,5 giây (lạnh, SSD) | Đo thực tế |
| NFR-10 | Quyền riêng tư | Không gửi dữ liệu ra ngoài, không cần quyền Administrator | Review mã, manifest, Privacy Policy |

---

# PHẦN 5: KIẾN TRÚC HỆ THỐNG TỔNG THỂ

## 5.1. Lựa chọn công nghệ
Khác QAS/WPD, BFR **không phải app chạy nền thường trú**: người dùng mở khi cần, nên RAM nền ít quan trọng; ưu tiên **độ đúng đắn, kiểm thử được và giao diện tốt**.

| Phương án | Ưu điểm | Nhược điểm |
| :--- | :--- | :--- |
| **A. .NET 8 + WinUI 3 (giao diện) + thư viện Core thuần .NET + CLI** | Giao diện Fluent chuẩn Windows 11, hợp MSIX, tích hợp menu chuột phải mới thuận lợi, tái dùng C# đang có | Chỉ Windows; WinUI 3 có đường cong học và một số hạn chế về công cụ |
| B. .NET 8 + WPF + thư viện WPF-UI | Ổn định, cộng đồng lớn, bảng dữ liệu lớn dễ ảo hóa | Giao diện kém "hiện đại" hơn nếu không tùy biến |
| C. Avalonia UI + Core thuần .NET | Đa nền tảng (Windows/macOS/Linux) | Tích hợp menu chuột phải phải làm riêng từng hệ điều hành; giao diện không "bản địa" |

**Quyết định đề xuất:** Phương án A hoặc B, **với kiến trúc tách lớp bắt buộc**: toàn bộ logic nằm trong `BFR.Core` (không phụ thuộc giao diện), nên đổi khung giao diện sau này (hoặc sang Avalonia) vẫn giữ nguyên lõi và bộ kiểm thử. Chốt A hay B sau Giai đoạn 0 (xem 14.2).

## 5.2. Sơ đồ kiến trúc

~~~text
+---------------------------------------------------------------------------+
|  BFR.App (WinUI 3 / WPF)                      BFR.Cli (bfr.exe)           |
|  Danh sách tệp | Rule Chain | Preview Grid     Phân tích tham số, in kết quả|
|  Undo History  | Preset | Cài đặt                                          |
+---------------------------+-----------------------------+-----------------+
                            |  (cùng gọi API của Core)     |
+---------------------------v-----------------------------v-----------------+
|                              BFR.Core (thuần .NET)                         |
|                                                                           |
|  +---------------+   +------------------+   +--------------------------+  |
|  | FileScanner   |-->| RulePipeline     |-->| ConflictDetector         |  |
|  | + Filters     |   | (các IRule)      |   | + NameValidator          |  |
|  +-------+-------+   +--------+---------+   +-------------+------------+  |
|          |                    |                           |               |
|          v                    v                           v               |
|  +---------------+   +------------------+   +--------------------------+  |
|  | MetadataReader|   | TokenEngine      |   | RenamePlanner            |  |
|  | EXIF, media   |   | {name} {n} {date}|   | (thứ tự, vòng, tên tạm)  |  |
|  +---------------+   +------------------+   +-------------+------------+  |
|                                                           |               |
|  +---------------+   +------------------+   +-------------v------------+  |
|  | PresetStore   |   | HistoryStore     |<--| RenameExecutor           |  |
|  | JSON          |   | Journal (WAL)    |   | + UndoService + Recovery |  |
|  +---------------+   +------------------+   +--------------------------+  |
|                                                                           |
|                  IFileSystem (trừu tượng hóa để kiểm thử)                 |
+---------------------------------------------------------------------------+
                            |
+---------------------------v-----------------------------------------------+
|  Windows: NTFS, MoveFileEx, GetFileInformationByHandle, Shell (IExplorerCommand) |
+---------------------------------------------------------------------------+
~~~

## 5.3. Trách nhiệm từng thành phần
| Thành phần | Trách nhiệm |
| :--- | :--- |
| **FileScanner + Filters** | Quét tệp/thư mục, áp bộ lọc, tạo danh sách `FileItem` bất biến (immutable). |
| **MetadataReader** | Đọc EXIF/media **theo yêu cầu** (chỉ khi có quy tắc cần), song song và có bộ nhớ đệm theo (đường dẫn, thời gian sửa, kích thước). |
| **RulePipeline** | Áp chuỗi `IRule` lên từng `FileItem`, sinh `PreviewItem` kèm vết trung gian (Rule Trace). |
| **TokenEngine** | Phân tích và thay thế token; báo lỗi cú pháp rõ ràng. |
| **ConflictDetector + NameValidator** | Kiểm tra tên hợp lệ, trùng lặp trong lô, va chạm với tệp ngoài lô, độ dài, tên dành riêng. |
| **RenamePlanner** | Biến danh sách (cũ → mới) thành **kế hoạch thực thi có thứ tự**, xử lý chuỗi phụ thuộc và vòng hoán đổi bằng tên tạm. |
| **RenameExecutor** | Thực thi kế hoạch với journal ghi trước (write-ahead), tiến trình, hủy, báo lỗi từng tệp. |
| **UndoService + Recovery** | Hoàn tác theo journal; khôi phục journal dở dang khi khởi động. |
| **PresetStore / HistoryStore** | Lưu preset và lịch sử (ghi nguyên tử). |
| **IFileSystem** | Giao diện trừu tượng; có bản thật và bản giả (in-memory) để kiểm thử, kể cả giả lập lỗi/mất điện. |

## 5.4. Mô hình luồng (Threading)
- Giao diện chỉ hiển thị và điều phối; **mọi tính toán Preview chạy nền** bằng `Task` có `CancellationToken`.
- Mỗi thay đổi quy tắc hủy tính toán đang chạy và bắt đầu tính toán mới (debounce 150 ms).
- Kết quả Preview trả về theo lô và cập nhật `ObservableCollection` ảo hóa.
- Thực thi đổi tên chạy trên một luồng riêng, tuần tự theo kế hoạch (đổi tên là thao tác metadata nhanh; song song hóa không mang lại nhiều lợi ích và làm phức tạp việc khôi phục).

---

# PHẦN 6: LÕI 1 — PIPELINE QUY TẮC ĐỔI TÊN VÀ ĐẶC TẢ TỪNG QUY TẮC

## 6.1. Mô hình dữ liệu cơ bản
~~~csharp
// Bản chụp bất biến của một tệp tại thời điểm nạp
public sealed record FileItem(
    string FullPath,          // đường dẫn đầy đủ hiện tại
    string Directory,         // thư mục chứa
    string Name,              // tên không gồm phần mở rộng
    string Extension,         // phần mở rộng, không gồm dấu chấm; "" nếu không có
    bool IsDirectory,
    long Size,
    DateTime CreatedUtc,
    DateTime ModifiedUtc,
    int LoadOrder);           // thứ tự nạp, dùng khi sắp xếp "theo thứ tự nạp"

// Trạng thái làm việc của một tệp khi đi qua chuỗi quy tắc
public sealed record RenameState(
    string Name,              // tên đang xử lý (không gồm đuôi)
    string Extension,         // đuôi đang xử lý
    int Index,                // chỉ số trong danh sách đã sắp xếp (bắt đầu từ 0)
    int GroupIndex,           // chỉ số trong nhóm (ví dụ theo thư mục)
    IReadOnlyList<string> Trace);   // vết sau từng quy tắc (Rule Trace)

public interface IRule
{
    string Id { get; }
    bool Enabled { get; }
    // Hàm thuần (pure): cùng đầu vào, cùng đầu ra; không đọc đồng hồ hay tệp trực tiếp
    // Dữ liệu ngoài (ngày hiện tại, EXIF) được truyền qua ngữ cảnh đã đóng băng
    RenameState Apply(FileItem file, RenameState state, RuleContext ctx);
}
~~~

**Nguyên tắc vàng:** mọi quy tắc là **hàm thuần**. `RuleContext` chứa các giá trị được **đóng băng một lần cho mỗi lượt Preview** (thời điểm hiện tại `now`, hạt giống ngẫu nhiên, dữ liệu EXIF đã đọc). Nhờ đó Preview và Áp dụng cho ra kết quả giống hệt nhau (NFR-6). Khi bấm Áp dụng, dùng **chính ngữ cảnh đã đóng băng của lượt Preview đang hiển thị**, không tính toán lại với `now` mới.

## 6.2. Thứ tự xử lý của một lượt Preview
1. Lọc và sắp xếp danh sách `FileItem` (xác định `Index`, `GroupIndex`).
2. Đóng băng `RuleContext` (thời điểm, hạt giống, dữ liệu metadata cần dùng).
3. Với từng tệp: khởi tạo `RenameState` từ tên/đuôi hiện tại, chạy lần lượt các quy tắc đang bật, ghi vết.
4. Ghép tên cuối `Name + "." + Extension` (bỏ dấu chấm nếu đuôi rỗng), đưa vào `NameValidator` và `ConflictDetector`.
5. Trả về `PreviewResult` gồm: danh sách `PreviewItem` (tên cũ, tên mới, trạng thái, vết) và kết luận chung (có chặn áp dụng không).

## 6.3. Đặc tả chi tiết từng quy tắc

### 6.3.1. Tìm và thay / Regex
| Tham số | Giá trị |
| :--- | :--- |
| Mẫu (pattern) | Văn bản thuần (được thoát ký tự đặc biệt tự động) hoặc Regex .NET |
| Thay thế | Chuỗi gồm `$1`, `${ten}`, `$&` và token BFR (xử lý token **sau** khi thay Regex) |
| Tùy chọn | Bỏ qua hoa/thường; chỉ lần khớp đầu / mọi lần khớp; phạm vi (Tên / Đuôi / Cả hai) |
| Cài đặt an toàn | `Regex` được tạo một lần mỗi lượt Preview với `matchTimeout` mặc định 250 ms; hết giờ thì dòng đó báo "Regex quá chậm" thay vì treo ứng dụng; chặn mẫu không hợp lệ ngay khi gõ và hiển thị lỗi tại chỗ |
| Văn hóa | `CultureInfo.InvariantCulture` để kết quả không phụ thuộc cấu hình máy |

Regex Tester (giao diện): ô nhập chuỗi thử, tô sáng khớp và nhóm bắt, hiển thị chuỗi thay thế kết quả; nút "Dùng cho tất cả tệp".

### 6.3.2. Số thứ tự (Numbering)
| Tham số | Mô tả |
| :--- | :--- |
| `start`, `step` | Giá trị bắt đầu và bước nhảy (có thể âm) |
| `width` | Độ rộng đệm 0 (ví dụ 3 cho `007`); chế độ "Tự động" tính theo tổng số tệp (1.000 tệp thì đệm 4 chữ số) |
| `position` | Đầu tên, cuối tên, vị trí N, hoặc thay toàn bộ tên |
| `separator` | Ký tự ngăn giữa số và phần còn lại |
| `resetMode` | Không đặt lại / đặt lại theo từng thư mục / đặt lại khi giá trị của một token thay đổi (ví dụ theo `{created:yyyy-MM-dd}`, tức đánh số lại từ 1 mỗi ngày) |
| `sortBy` | Xem 6.4; mặc định là thứ tự hiển thị hiện tại của danh sách |
| `kind` | Thập phân (mặc định), chữ cái (a, b, ..., z, aa), La Mã (Pro, tùy chọn) |

Giá trị số của tệp thứ `i` trong nhóm: `value = start + step * GroupIndex` (với `resetMode = theo thư mục` thì `GroupIndex` là chỉ số trong thư mục).

### 6.3.3. Ngày tháng
- **Nguồn:** `Created`, `Modified`, `Taken` (EXIF `DateTimeOriginal`, dự phòng `DateTimeDigitized`), `Media` (ngày tạo media, Pro), `Now`.
- **Chuỗi dự phòng:** người dùng chọn thứ tự, ví dụ `Taken → Created → Modified`. Trạng thái dòng ghi rõ nguồn đã dùng ("từ EXIF", "dự phòng: ngày tạo").
- **Múi giờ:** mặc định giờ địa phương; tùy chọn UTC. EXIF thường **không có múi giờ**, coi là giờ địa phương của máy chụp và ghi chú điều này trong giao diện.
- **Định dạng:** chuỗi định dạng ngày .NET (`yyyy-MM-dd_HHmmss`); xem trước định dạng ngay khi gõ; chặn ký tự không hợp lệ cho tên tệp (ví dụ `:` trong `HH:mm`) và gợi ý thay thế.
- **Lưu ý ngày tạo:** ngày tạo trên Windows có thể thay đổi khi sao chép/tải tệp (không phải ngày "gốc"); giao diện nên nhắc điều này khi người dùng chọn nguồn Created.

### 6.3.4. Cắt tỉa
Các thao tác (bật/tắt riêng, thực hiện theo thứ tự cố định trong quy tắc): (1) xóa ký tự thuộc tập chỉ định, (2) xóa N ký tự đầu/cuối, (3) xóa theo khoảng vị trí `[a, b)`, (4) gộp khoảng trắng liên tiếp, (5) cắt khoảng trắng đầu/cuối, (6) xóa dấu chấm/khoảng trắng ở cuối tên. Mọi vị trí tính theo **phần tử văn bản (text element)** để không cắt ngang một ký tự có dấu ghép.

### 6.3.5. Hoa/thường
Dùng `CultureInfo.InvariantCulture` (hoặc văn hóa `vi-VN` khi chọn) cho tiêu đề; Title Case có danh sách từ ngoại lệ (không đổi `PDF`, `USB`, `iPhone`...). Cẩn thận ký tự đặc biệt (ví dụ chữ `İ` tiếng Thổ Nhĩ Kỳ) bằng cách không dùng văn hóa hiện hành mặc định.

### 6.3.6. Bỏ dấu và chuẩn hóa
~~~csharp
public static string RemoveVietnameseDiacritics(string s)
{
    // 1) Thay riêng đ/Đ vì Unicode không phân rã chúng thành d + dấu
    s = s.Replace('đ', 'd').Replace('Đ', 'D');
    // 2) Phân rã (NFD), bỏ ký tự dấu (NonSpacingMark), ghép lại (NFC)
    var formD = s.Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder(formD.Length);
    foreach (var ch in formD)
        if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            sb.Append(ch);
    return sb.ToString().Normalize(NormalizationForm.FormC);
}
~~~
Ngoài ra: chuẩn hóa NFC (xử lý tên tệp "dấu rời" sinh ra từ macOS/iOS khiến cùng một chữ có hai dạng mã hóa khác nhau), thay ký tự không hợp lệ `< > : " / \ | ? *` và ký tự điều khiển bằng ký tự thay thế do người dùng chọn.

### 6.3.7. Phần mở rộng
Mặc định **bảo toàn** phần mở rộng; mọi quy tắc khác chỉ tác động lên phần Tên trừ khi người dùng chọn rõ phạm vi. Đổi đuôi sang loại khác (ví dụ `.jpeg` thành `.jpg`) cần bật cờ "Cho phép đổi phần mở rộng"; khi dùng, hiển thị cảnh báo một lần trong lượt áp dụng.

## 6.4. Sắp xếp khi đánh số: natural sort
Sắp xếp theo tên phải dùng **so sánh tự nhiên** (`Bai 2` đứng trước `Bai 10`). Dùng `StrCmpLogicalW` của Windows (hoặc cài đặt tương đương trong mã) để khớp với thứ tự của Explorer; tùy chọn so sánh theo thứ tự thường (ordinal) cho người dùng nâng cao.

---

# PHẦN 7: LÕI 2 — PREVIEW, PHÁT HIỆN XUNG ĐỘT, THỰC THI AN TOÀN VÀ UNDO

## 7.1. NameValidator: tên mới hợp lệ khi...
| # | Điều kiện | Mã trạng thái |
| :-: | :--- | :--- |
| 1 | Không rỗng, không chỉ gồm khoảng trắng | `Empty` |
| 2 | Không chứa ký tự cấm của Windows (`< > : " / \ | ? *` và ký tự điều khiển 0–31) | `InvalidChar` |
| 3 | Không kết thúc bằng dấu chấm hoặc khoảng trắng | `TrailingDotSpace` |
| 4 | Không phải tên dành riêng của thiết bị: `CON, PRN, AUX, NUL, COM1–COM9, LPT1–LPT9` (kể cả khi có đuôi, ví dụ `CON.txt`) | `ReservedName` |
| 5 | Độ dài tên (gồm đuôi) không quá 255 ký tự (UTF-16) | `NameTooLong` |
| 6 | Độ dài đường dẫn đầy đủ không vượt giới hạn: 260 ký tự nếu hệ thống chưa bật hỗ trợ đường dẫn dài; kiểm tra chế độ hiện hành và dùng tiền tố `\\?\` khi cần | `PathTooLong` |

## 7.2. ConflictDetector: ba loại xung đột
1. **Trùng trong lô:** hai tệp trong cùng thư mục có tên mới giống nhau. **So sánh không phân biệt hoa/thường** theo quy tắc của Windows (`OrdinalIgnoreCase`; lưu ý NTFS còn có thể bật phân biệt hoa/thường theo thư mục, nên đọc thuộc tính này nếu cần chính xác hơn).
2. **Va chạm với tệp ngoài lô:** tên mới đã thuộc về một tệp tồn tại trong thư mục nhưng **không** nằm trong lô đang đổi (hoặc đã bị bỏ chọn).
3. **Chuỗi/vòng phụ thuộc trong lô:** tên mới của tệp A là tên cũ của tệp B (cũng đang đổi). Đây **không phải lỗi**; được `RenamePlanner` xử lý (7.3).

**Trường hợp chỉ đổi hoa/thường (`foo.txt` thành `Foo.txt`):** tên mới "trùng" tên cũ nếu so sánh không phân biệt hoa/thường nhưng thực chất là **cùng một tệp**. Để phân biệt, so sánh danh tính tệp (file ID: `GetFileInformationByHandle` hoặc `GetFileInformationByHandleEx`) thay vì chỉ so sánh chuỗi; tệp cùng danh tính được xem là chính nó, không phải xung đột.

## 7.3. RenamePlanner: sắp thứ tự thực thi
Đầu vào: tập cặp `(đường dẫn cũ → đường dẫn mới)` đã hợp lệ và không xung đột loại 1–2.

Thuật toán:
1. Dựng đồ thị có hướng: cạnh `A → B` nếu `đường dẫn mới của A` bằng `đường dẫn cũ của B` (B phải nhường chỗ trước).
2. Với phần **không có vòng** (chuỗi, ví dụ `a→b`, `b→c`, `c→d`): thực thi theo **thứ tự topo ngược** (đổi `c→d` trước, rồi `b→c`, rồi `a→b`).
3. Với **vòng** (ví dụ hoán đổi `a→b`, `b→a`): đổi một tệp trong vòng sang **tên tạm duy nhất** (`.bfr-{guid}.tmp`) trước; làm xong các bước còn lại của vòng; cuối cùng đổi tên tạm về tên đích.
4. Với **thư mục** (khi đổi cả thư mục): xử lý từ **lá lên gốc** (đổi tên tệp trong thư mục trước, rồi mới đổi tên thư mục) hoặc tính lại đường dẫn tương ứng sau mỗi bước, để tránh đường dẫn con bị vô hiệu.
5. Kế hoạch cuối là **danh sách bước tuần tự** (`Step { From, To, Kind: Direct | ToTemp | FromTemp }`) được ghi vào journal trước khi chạy.

## 7.4. RenameExecutor: thực thi có nhật ký ghi trước (Write-Ahead Journal)
Định dạng journal: tệp JSON Lines tại `%LocalAppData%\BatchFileRenamer\journal\{yyyyMMdd-HHmmss}-{id}.jsonl`.

~~~json
{"type":"header","id":"b7f1...","startedUtc":"2026-10-19T09:30:00Z","totalSteps":1200,"app":"1.0.0"}
{"type":"step","seq":1,"from":"D:\\Anh\\IMG_001.jpg","to":"D:\\Anh\\2024-05-01_001.jpg","kind":"Direct","status":"pending"}
{"type":"step","seq":1,"status":"done"}
{"type":"step","seq":2,"from":"...","to":"...","kind":"ToTemp","status":"pending"}
{"type":"footer","status":"completed","doneUtc":"2026-10-19T09:30:07Z"}
~~~

Quy trình mỗi bước:
1. Ghi dòng `pending` cho bước, **flush xuống đĩa** (`FileStream.Flush(true)`).
2. Thực hiện đổi tên bằng `File.Move`/`Directory.Move` (hoặc `MoveFileEx`; **không** dùng cờ ghi đè).
3. Ghi dòng `done` (hoặc `failed` kèm mã lỗi) và flush.
4. Cập nhật tiến trình lên giao diện (theo lô, không cập nhật từng dòng để tránh chậm).

Xử lý lỗi từng tệp: lỗi **khóa tệp (đang được tiến trình khác mở), từ chối quyền, tệp không còn tồn tại** → ghi `failed`, **tiếp tục** các bước độc lập (nếu người dùng chọn "Bỏ qua lỗi") hoặc **dừng và đề nghị hoàn tác phần đã làm**. Lỗi ở bước thuộc một chuỗi/vòng phụ thuộc → dừng chuỗi đó và hoàn tác riêng chuỗi.

Hủy giữa chừng: dừng sau bước đang chạy, ghi `footer` trạng thái `cancelled`, hiển thị "Đã đổi N/M tệp. Hoàn tác phần đã làm?".

## 7.5. Khôi phục sau sự cố (Crash Recovery)
Khi khởi động: quét thư mục journal; journal không có `footer` là **dở dang**. Giao diện đề xuất:
- **Hoàn tất:** chạy tiếp các bước còn `pending` (sau khi kiểm tra trạng thái thực của hệ thống tệp).
- **Khôi phục về ban đầu:** hoàn tác các bước đã `done` theo thứ tự ngược.
Với bước ở trạng thái `pending` không có `done`: kiểm tra thực tế (tệp đang ở `from` hay `to`) để biết bước đã xảy ra chưa, vì có thể sự cố xảy ra giữa lúc đổi tên và lúc ghi `done`.

## 7.6. UndoService
- Mỗi lần áp dụng thành công tạo một **mục lịch sử** (liên kết tới journal).
- Hoàn tác = chạy journal theo **thứ tự ngược**, đổi `to` về `from`.
- **Kiểm tra trước khi hoàn tác:** với mỗi bước, `to` còn tồn tại và `from` đang trống. Nếu người dùng đã di chuyển/đổi tên/xóa tệp sau đó, bước đó được báo "Không thể hoàn tác" kèm lý do, các bước còn lại vẫn xử lý (Hoàn tác từng phần), kết quả hiển thị rõ.
- Tùy chọn kiểm tra danh tính tệp (file ID) để chắc tệp tại `to` đúng là tệp đã được đổi.
- Lịch sử giữ 30 ngày (Pro) hoặc 1 lần gần nhất (Free); dọn tự động khi khởi động.

## 7.7. Các bất biến an toàn (Invariants) bắt buộc kiểm thử
| Mã | Bất biến |
| :-: | :--- |
| INV-1 | Tổng số tệp trong các thư mục liên quan **không đổi** sau khi áp dụng (không tạo thêm, không mất) |
| INV-2 | Nội dung từng tệp **không đổi** (kiểm tra bằng băm trong test) |
| INV-3 | Không có thao tác nào ghi đè tệp đang tồn tại |
| INV-4 | Tập tên cuối cùng bằng đúng tập tên mới trong preview |
| INV-5 | Hoàn tác ngay sau áp dụng đưa hệ thống tệp về đúng trạng thái ban đầu |
| INV-6 | Ở mọi điểm dừng giả lập (crash/mất điện) giữa chừng, journal đủ thông tin để hoàn tất hoặc khôi phục |

---

# PHẦN 8: THIẾT KẾ GIAO DIỆN VÀ TRẢI NGHIỆM (UI/UX)

## 8.1. Nguyên tắc
- Một cửa sổ chính, bố cục 3 vùng: **Danh sách/Preview** (giữa), **Chuỗi quy tắc** (trái hoặc phải), **Trạng thái và nút Áp dụng** (dưới).
- Giao diện Fluent theo chủ đề sáng/tối hệ thống; phông `Segoe UI Variable`; đầy đủ phím tắt và hỗ trợ trình đọc màn hình.
- Người mới: màn hình rỗng có vùng kéo thả lớn và 3 preset gợi ý. Người nâng cao: chuỗi quy tắc, Regex tester, CLI.
- Mọi thao tác phá hủy dữ liệu (áp dụng lô lớn) có xác nhận tóm tắt.

## 8.2. Màn hình chính (wireframe)
~~~text
+------------------------------------------------------------------------------------------+
| Batch File Renamer                                         [Preset v] [Hoàn tác v] [Cài đặt]|
+--------------------------+---------------------------------------------------------------+
| CHUỖI QUY TẮC     [+ Thêm]| [Thêm tệp] [Thêm thư mục]  [x] Quét thư mục con   Sắp xếp: [Tên v]|
|                          | ------------------------------------------------------------ |
| 1. [x] Cắt tỉa           |  [x]  Tên cũ                      Tên mới                Trạng thái|
|    Xóa "(1)", gộp khoảng |  [x]  IMG_001 (1).jpg      ->     2024-05-01_001.jpg      OK       |
|    trắng                 |  [x]  IMG_002 (1).jpg      ->     2024-05-01_002.jpg      OK       |
|                          |  [x]  IMG_003.jpg          ->     2024-05-02_001.jpg      OK       |
| 2. [x] Ngày tháng        |  [x]  Bai 10.pdf           ->     Bai 10.pdf             Không đổi |
|    EXIF > Ngày tạo       |  [ ]  notes.txt            ->     notes.txt              (bỏ chọn) |
|    yyyy-MM-dd            |  [x]  CON.txt              ->     CON.txt                 Tên dành riêng
|                          |  [x]  a.jpg                ->     b.jpg                   Xung đột  |
| 3. [x] Số thứ tự         |                                                               |
|    bắt đầu 1, đệm 3,     |  Lọc: [Tất cả v]  Tìm: [______]                                 |
|    đặt lại theo ngày     | ------------------------------------------------------------ |
|                          |  Rule Trace của dòng chọn:                                    |
| [Lưu thành preset]       |   gốc: IMG_001 (1) -> 1.Cắt tỉa: IMG_001 -> 2.Ngày: ... -> 3....|
+--------------------------+---------------------------------------------------------------+
| 120 tệp | Sẽ đổi 118 | Không đổi 1 | Lỗi 1 (chặn áp dụng)        [Bỏ qua lỗi]  [ÁP DỤNG]   |
+------------------------------------------------------------------------------------------+
~~~

## 8.3. Hộp thoại Regex Tester
~~~text
+--------------------------------------------------------------+
| Mẫu Regex:      ^(\d+)_(.+)\.log$              [x] Bỏ qua hoa/thường |
| Thay bằng:      $2_$1.log                                       |
| Chuỗi thử:      20240501_server.log                              |
|--------------------------------------------------------------|
| Khớp:   [20240501]_[server].log   Nhóm 1: 20240501  Nhóm 2: server |
| Kết quả: server_20240501.log                                    |
| [Dùng cho tất cả tệp]    [Thư viện mẫu v]                        |
+--------------------------------------------------------------+
~~~

## 8.4. Hộp thoại xác nhận áp dụng
~~~text
+---------------------------------------------------------+
|  Áp dụng đổi tên 118 tệp?                               |
|  - 3 thư mục sẽ bị tác động                              |
|  - 2 tệp đổi phần mở rộng (jpeg -> jpg)                  |
|  - Có thể hoàn tác trong mục "Hoàn tác"                  |
|             [Xem lại]            [Áp dụng]               |
+---------------------------------------------------------+
~~~

## 8.5. Màn hình Lịch sử và Hoàn tác
Danh sách các lần áp dụng (thời điểm, số tệp, preset đã dùng, trạng thái), nút Hoàn tác từng lần, xem chi tiết từng bước và lý do nếu hoàn tác từng phần.

## 8.6. Hiệu năng giao diện
- Bảng Preview **ảo hóa** (chỉ dựng dòng đang hiển thị), cập nhật theo lô; hỗ trợ 100.000 dòng.
- Hiển thị thanh tiến trình khi quét lớn hoặc đọc EXIF; có nút Hủy.
- Không khóa giao diện khi tính Preview; hiển thị trạng thái "Đang tính..." mờ ở các dòng chưa xong.

---

# PHẦN 9: MÔ HÌNH DỮ LIỆU, PRESET, CẤU HÌNH VÀ CLI

## 9.1. Lưu trữ
- Cài đặt: `%LocalAppData%\BatchFileRenamer\settings.json`.
- Preset: `%LocalAppData%\BatchFileRenamer\presets\*.bfrpreset` (mỗi tệp một preset, dễ chia sẻ).
- Lịch sử/journal: `%LocalAppData%\BatchFileRenamer\journal\` (xem 7.4).
- **Ghi nguyên tử** với `File.Replace` và tệp `.bak`; trường `schemaVersion` + migration giữa các phiên bản.

## 9.2. Schema Preset (`.bfrpreset`)
~~~json
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
~~~

## 9.3. Thư viện Preset dựng sẵn (đề xuất)
| Tên | Tác dụng |
| :--- | :--- |
| Dọn dẹp tên tệp | Bỏ `(1)`, `- Copy`, gộp khoảng trắng, cắt hai đầu |
| Bỏ dấu tiếng Việt + gạch nối | `Báo cáo tháng 5.docx` thành `bao-cao-thang-5.docx` |
| Đánh số theo thứ tự | `001`, `002`, ... theo natural sort |
| Ảnh theo ngày chụp | `YYYY-MM-DD_001.jpg` từ EXIF |
| Chữ thường toàn bộ | Chuyển tên và đuôi sang chữ thường |
| Thêm tiền tố thư mục cha | `{parent}_{name}` |
| Chuẩn hóa Unicode (NFC) | Sửa tên tệp "dấu rời" |

## 9.4. Đặc tả CLI (`bfr.exe`, Pro)
~~~text
bfr rename <paths...> [options]

  --preset <name|file.bfrpreset>   Dùng preset
  --find <pattern> --replace <s>   Tìm/thay nhanh (kết hợp --regex, --ignore-case)
  --number <start>[:step[:width]]  Đánh số nhanh
  --recursive                      Quét thư mục con
  --include <glob> / --exclude <glob>
  --dry-run                        Chỉ in kết quả, không ghi gì (mặc định cho script an toàn)
  --yes                            Bỏ qua câu hỏi xác nhận
  --format text|json|csv           Định dạng kết quả

bfr undo [--last | --id <id>]      Hoàn tác
bfr history                        Liệt kê lịch sử
bfr preset list|export|import      Quản lý preset
~~~
Mã thoát: `0` thành công; `1` lỗi tham số; `2` có xung đột/tên không hợp lệ (không thay đổi gì); `3` thực thi thất bại một phần (đã in danh sách); `4` lỗi hệ thống. CLI và giao diện dùng **chung `BFR.Core`**, nên hành vi giống hệt nhau.

---

# PHẦN 10: EDGE CASES, BẢO MẬT, QUYỀN RIÊNG TƯ VÀ TƯƠNG THÍCH

## 10.1. Ma trận 18 trường hợp khó
| # | Tình huống | Hệ quả nếu bỏ qua | Cách xử lý |
| :-: | :--- | :--- | :--- |
| 1 | Hai tệp có cùng tên mới (khác hoa/thường) | Một tệp thất bại hoặc bị ghi đè | `ConflictDetector` so sánh không phân biệt hoa/thường; đánh dấu Xung đột trước khi áp dụng |
| 2 | Hoán đổi tên (`a↔b`) hoặc chuỗi (`a→b→c`) | Lỗi "tên đã tồn tại" giữa chừng | `RenamePlanner` dùng thứ tự topo + tên tạm (7.3) |
| 3 | Chỉ đổi hoa/thường (`foo`→`Foo`) | Bị báo trùng với chính nó | So sánh theo file ID, không chỉ theo chuỗi (7.2) |
| 4 | Tên dành riêng (`CON`, `NUL`, `COM1`...) | Đổi tên thất bại hoặc tạo tệp khó xóa | `NameValidator` chặn (7.1) |
| 5 | Tên kết thúc bằng dấu chấm/khoảng trắng | Windows tự cắt hoặc từ chối, gây lệch preview | Chặn hoặc tự cắt có hiển thị rõ ràng trong preview |
| 6 | Đường dẫn dài hơn 260 ký tự | Lỗi khi đổi tên | Kiểm tra giới hạn hiện hành, dùng tiền tố `\\?\` khi cho phép; báo `PathTooLong` |
| 7 | Tệp đang được ứng dụng khác mở/khóa | Bước đổi tên thất bại | Bắt lỗi từng tệp, ghi `failed`, cho phép tiếp tục hoặc dừng/hoàn tác chuỗi |
| 8 | Tệp chỉ đọc, tệp ẩn/hệ thống | Hành vi bất ngờ | Mặc định loại tệp ẩn/hệ thống khỏi lô; tệp chỉ đọc: đổi tên được nhưng hiển thị biểu tượng nhắc |
| 9 | Regex nguy hiểm (catastrophic backtracking) | Treo ứng dụng | `matchTimeout`, chạy nền, hủy được (6.3.1) |
| 10 | Tên Unicode "dấu rời" (NFD) từ macOS/iOS | Cùng một chữ nhưng hai dạng mã hóa, tìm/thay sai | Quy tắc chuẩn hóa NFC; cảnh báo khi phát hiện tên NFD |
| 11 | Đuôi tệp bị đổi nhầm | Tệp không mở được | Cờ cho phép đổi đuôi, mặc định tắt; cảnh báo khi áp dụng |
| 12 | Quá nhiều tệp chọn từ Explorer (hàng nghìn) | Dòng lệnh vượt giới hạn độ dài (khoảng 32.767 ký tự) | Truyền danh sách qua tệp tạm hoặc named pipe, không qua tham số (11.3) |
| 13 | Menu chuột phải gọi lặp lại cho từng tệp (kiểu động từ cũ) | Mở hàng trăm cửa sổ | Mô hình "người thu thập": instance đầu chờ 300–500 ms để gom đủ đường dẫn, các instance sau chuyển tiếp rồi thoát (single instance) |
| 14 | Tệp OneDrive/đám mây ở trạng thái "chỉ trực tuyến" (placeholder) | Đọc EXIF làm tải tệp về ngoài ý muốn | Phát hiện thuộc tính placeholder/recall; **không đọc nội dung** khi không cần, cảnh báo khi quy tắc cần EXIF |
| 15 | Ổ đĩa mạng/USB chậm hoặc ngắt kết nối giữa chừng | Treo hoặc dở dang | Hết giờ theo bước, bắt lỗi, journal đảm bảo khôi phục; hiển thị trạng thái rõ |
| 16 | Thư mục đang đổi tên là thư mục cha của tệp khác trong lô | Đường dẫn con bị hỏng | Xử lý từ lá lên gốc (7.3, bước 4) |
| 17 | Ngày EXIF thiếu hoặc sai (ví dụ `0000:00:00`) | Tên chứa giá trị vô nghĩa | Kiểm tra hợp lệ, dùng chuỗi dự phòng và ghi chú nguồn trong trạng thái dòng |
| 18 | Chọn nhầm thư mục hệ thống (ví dụ `C:\Windows`, `Program Files`) | Hỏng hệ điều hành/ứng dụng | Danh sách thư mục được bảo vệ; chặn hoặc yêu cầu xác nhận mạnh; mặc định chặn |

## 10.2. Bảo mật và quyền riêng tư
- **Hoàn toàn cục bộ:** không gửi tên tệp, đường dẫn, nội dung hay metadata ra ngoài thiết bị. Telemetry (nếu có) là opt-in, chỉ gồm thông tin ẩn danh (phiên bản, bản Windows, mã lỗi) và có công tắc rõ ràng.
- **Không yêu cầu quyền Administrator.** Không đổi tên tệp mà người dùng không có quyền; không cố nâng quyền.
- **Chống truyền dữ liệu qua IPC:** kênh named pipe giữa các instance chỉ chấp nhận kết nối từ cùng người dùng (ACL) và kiểm tra dữ liệu đầu vào.
- **Bộ lọc đầu vào:** đường dẫn được chuẩn hóa và kiểm tra để tránh các trường hợp đường dẫn gây hiểu nhầm (ví dụ chứa `..`, ký tự điều khiển).
- **Chính sách quyền riêng tư** bắt buộc khi nộp Store, nêu rõ ứng dụng đọc metadata tệp (EXIF) chỉ để dựng tên và không rời khỏi máy.

## 10.3. Tương thích
| Hạng mục | Windows 10 (19041+) | Windows 11 |
| :--- | :--- | :--- |
| Menu chuột phải kiểu cũ (động từ registry) | Có | Có (trong "Hiện thêm tùy chọn") |
| Menu chuột phải kiểu mới (IExplorerCommand + MSIX) | Không áp dụng | Có |
| Đường dẫn dài | Cần bật chính sách hệ thống và manifest `longPathAware` | Tương tự |
| Giao diện WinUI 3 | Có (cần runtime phù hợp) | Có |

---

# PHẦN 11: ĐÓNG GÓI MSIX, TÍCH HỢP MENU CHUỘT PHẢI, CHỨNG NHẬN STORE VÀ CI/CD

## 11.1. Đóng gói
- Gói MSIX (Desktop Bridge, `runFullTrust`) cho ứng dụng chính; CLI (`bfr.exe`) khai báo `AppExecutionAlias` để chạy từ Terminal.
- Khai báo `longPathAware` trong application manifest.
- Tùy chọn bản portable (ZIP) trên GitHub Releases: chỉ có tính năng Free (Store IAP không hoạt động ngoài Store; xem 13.3).

## 11.2. Tích hợp menu chuột phải
Hai tầng:
1. **Windows 11 (menu mới):** gói MSIX khai báo tiện ích mở rộng menu ngữ cảnh Explorer, trỏ tới một **COM server cài đặt `IExplorerCommand`**; với nhiều tệp được chọn, `Invoke` nhận một `IShellItemArray` chứa toàn bộ lựa chọn trong một lần gọi. Phiên bản manifest và tên phần tử XML chính xác cần **kiểm tra theo tài liệu hiện hành của Microsoft** khi triển khai (không ghi cố định ở đây).
2. **Dự phòng/Windows 10 (menu cũ):** khóa registry theo người dùng (`HKCU\Software\Classes\*\shell\BFR`, `...\Directory\shell\BFR`) với `MultiSelectModel` phù hợp. Ở gói MSIX không ghi registry toàn cục; dùng cơ chế khai báo của gói hoặc tạo khi chạy lần đầu theo cách được phép.

## 11.3. Nhận danh sách tệp từ Explorer (quan trọng)
- Không truyền toàn bộ đường dẫn qua dòng lệnh (giới hạn khoảng 32.767 ký tự). Quy trình: `IExplorerCommand`/shell verb ghi danh sách vào **tệp tạm** (hoặc gửi qua **named pipe** tới instance đang chạy) rồi khởi chạy ứng dụng với tham số `--file-list "<đường dẫn tệp tạm>"`.
- **Single instance + người thu thập:** dùng Mutex; instance đầu tiên mở cửa sổ và nhận thêm đường dẫn qua pipe trong khoảng thời gian gom (300–500 ms) rồi mới dựng danh sách.

## 11.4. Checklist chứng nhận Store
1. `requestedExecutionLevel = asInvoker`; không đòi quyền Administrator.
2. Điền "Notes for Certification" giải thích `runFullTrust`: ứng dụng cần truy cập hệ thống tệp và tích hợp menu ngữ cảnh Explorer để đổi tên tệp do người dùng chọn.
3. Khởi chạy từ nút "Open" phải hiện cửa sổ chính (ứng dụng không chỉ nằm ở khay hệ thống).
4. Có Chính sách quyền riêng tư và mô tả trung thực; ảnh chụp màn hình không chứa tên tệp/dữ liệu cá nhân thật.
5. Kiểm tra trademark tên "Batch File Renamer" (tên mô tả chung dễ trùng), đặt trước tên trên Partner Center sớm; cân nhắc tên thương hiệu riêng kèm phụ đề mô tả.
6. Chuẩn bị tài khoản nhà phát triển, kiểm tra phí đăng ký, thuế và thanh toán hiện hành.

## 11.5. CI/CD
- GitHub Actions: build, unit test, property test, benchmark (so sánh với ngưỡng), đóng gói MSIX và ký thử, quét giấy phép thư viện bên thứ ba.
- Chạy test hệ thống tệp trên **ổ tạm** (thư mục tạm duy nhất mỗi lần chạy) và bản giả `IFileSystem` cho kịch bản lỗi/mất điện.
- Chạy trên ít nhất Windows Server (runner) cho test logic; giao diện/menu chuột phải kiểm thử thủ công trên máy thật Windows 10 và 11.

---

# PHẦN 12: CHIẾN LƯỢC KIỂM THỬ VÀ MA TRẬN QA

## 12.1. Chiến lược
- **Unit test** cho từng quy tắc (bảng dữ liệu vào/ra, gồm tiếng Việt, ký tự đặc biệt, Unicode, chuỗi rỗng).
- **Property-based test** (ví dụ FsCheck) cho các bất biến INV-1..INV-6: sinh ngẫu nhiên danh sách tên, chuỗi quy tắc, kiểm tra preview = áp dụng, hoán đổi/chuỗi/vòng, hoàn tác về trạng thái ban đầu.
- **Fault injection** qua `IFileSystem` giả: ném lỗi khóa tệp, từ chối quyền, "mất điện" tại từng điểm giữa các bước; kiểm tra khôi phục.
- **Benchmark** cho NFR-1..NFR-5 trong CI.
- **Dogfooding:** dùng cho ảnh/tài liệu thật của bản thân ít nhất 2 tuần trước khi nộp Store (sao lưu trước khi thử!).

## 12.2. Ma trận QA
| Mã | Hạng mục | Kịch bản | Kết quả kỳ vọng |
| :-: | :--- | :--- | :--- |
| QA-01 | Preview khớp áp dụng | 1.000 ca ngẫu nhiên, mỗi ca so tập tên sau áp dụng với preview | 100% khớp, gồm các token ngày/ngẫu nhiên (ngữ cảnh đóng băng) |
| QA-02 | Natural sort + đánh số | `Bai 1, Bai 2, Bai 10`, đệm 3, bắt đầu 1 | `Bai 001, Bai 002, Bai 003` theo đúng thứ tự tự nhiên |
| QA-03 | Bỏ dấu tiếng Việt | Bộ chuỗi đầy đủ các dấu thanh/mũ/móc, `đ/Đ`, chữ in hoa | Không còn dấu; `đ→d`, `Đ→D`; không làm hỏng chữ cái không dấu |
| QA-04 | Hoán đổi/chuỗi/vòng | `a↔b`, `a→b→c`, vòng 3 tệp | Hoàn tất đúng, không mất tệp, không để lại tên tạm |
| QA-05 | Mất điện giả lập | Dừng tại mọi bước của lô 200 tệp, rồi chạy Recovery | Có thể hoàn tất hoặc khôi phục; không tệp nào bị mất/ghi đè |
| QA-06 | Hoàn tác | Áp dụng rồi hoàn tác ngay; hoàn tác sau khi người dùng đã tự đổi tên một tệp | Trường hợp đầu về đúng ban đầu; trường hợp sau hoàn tác từng phần kèm lý do |
| QA-07 | Xung đột | Trùng tên khác hoa/thường; va chạm tệp ngoài lô; tên dành riêng; đuôi dấu chấm | Mỗi trường hợp có trạng thái đúng, nút Áp dụng bị chặn |
| QA-08 | Regex an toàn | Mẫu gây backtracking nặng; mẫu sai cú pháp | Hết giờ và báo lỗi thân thiện; giao diện không treo |
| QA-09 | EXIF/ngày | Ảnh có/không có EXIF, EXIF sai, ảnh từ điện thoại, ảnh đã chỉnh sửa | Dùng đúng chuỗi dự phòng, ghi rõ nguồn |
| QA-10 | Quy mô lớn | 100.000 tệp trong cây thư mục sâu | Đạt NFR-1/2/5; giao diện mượt, hủy được |
| QA-11 | Tệp bị khóa/chỉ đọc/ẩn | Mở tệp bằng ứng dụng khác khi áp dụng | Báo lỗi từng tệp, phần còn lại xử lý theo lựa chọn người dùng |
| QA-12 | Menu chuột phải | Chọn 1, 50, 5.000 tệp từ Explorer; cả menu mới và cũ | Chỉ một cửa sổ mở, nhận đủ tệp, không vượt giới hạn dòng lệnh |
| QA-13 | Đường dẫn dài/Unicode | Đường dẫn trên 260 ký tự, emoji, chữ CJK, tiếng Việt | Hoạt động đúng hoặc báo lỗi rõ ràng |
| QA-14 | Đám mây/ổ mạng | Tệp OneDrive chỉ trực tuyến; ổ mạng ngắt kết nối giữa chừng | Không tải tệp ngoài ý muốn; xử lý lỗi, journal đủ để khôi phục |
| QA-15 | CLI | `--dry-run`, mã thoát từng trường hợp, `--format json` | Hành vi và kết quả khớp với giao diện |
| QA-16 | Cài đặt MSIX | Cài, gỡ, nâng cấp; menu chuột phải xuất hiện/biến mất đúng | Không để lại rác; trạng thái nhất quán |
| QA-17 | Truy cập | Dùng hoàn toàn bằng bàn phím và Narrator, chế độ High Contrast | Thao tác chính dùng được đầy đủ |

---

# PHẦN 13: THƯƠNG MẠI HÓA, ASO VÀ GO-TO-MARKET

## 13.1. Định giá
- Pro mua đứt trọn đời: đề xuất **3,99 USD** (giá theo sức mua khu vực), Durable Add-on qua `Windows.Services.Store` (Product ID ví dụ `BFR_PRO_LIFETIME`). Tỷ lệ chia sẻ doanh thu cần kiểm tra theo chính sách Store hiện hành; bảng dưới dùng giả định 85%.
- Có thể dùng thử Pro 14 ngày một lần để người dùng trải nghiệm EXIF, Undo đa bước và CLI.

## 13.2. Dự phóng doanh thu (thận trọng)
Công thức, với `I` là lượt cài mới mỗi tháng, `r` là tỷ lệ người dùng còn hoạt động, `c` là tỷ lệ mua Pro, `P` là giá (USD), `s` là tỷ lệ thực nhận:

`R_thang = I * r * c * P * s`

| Kịch bản | I | r | c | P | s | Doanh thu ròng/tháng (xấp xỉ) |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| Bi quan | 600 | 50% | 1,0% | 3,99 | 0,85 | khoảng 10 USD |
| Thận trọng | 2.000 | 50% | 1,5% | 3,99 | 0,85 | khoảng 51 USD |
| Cơ sở | 6.000 | 55% | 2,5% | 3,99 | 0,85 | khoảng 280 USD |
| Thành công lớn | 20.000 | 55% | 3,0% | 3,99 | 0,85 | khoảng 1.119 USD |

> **Nhận định thẳng thắn:** Với đối thủ miễn phí mạnh (PowerRename), kịch bản bi quan/thận trọng là khả năng thực tế đáng kể; doanh thu ban đầu chỉ là thu nhập phụ nhỏ. Giá trị bền vững hơn của dự án nằm ở: (1) **chất lượng kỹ thuật chứng minh được** (bộ kiểm thử, property test, journal) rất phù hợp để đưa vào hồ sơ kỹ sư phần mềm, (2) kinh nghiệm phát hành sản phẩm thật lên Store, (3) tái sử dụng hạ tầng chung với QAS/WPD. Doanh thu thực tế là đường cong tăng dần rồi giảm sau đợt ra mắt; cập nhật bảng này bằng số liệu thật sau 2–3 tháng.

## 13.3. Chiến lược phân phối và license
- **Store (MSIX):** kênh chính, có IAP.
- **GitHub Releases (portable):** chỉ có tính năng Free (hoặc cơ chế license khóa riêng nếu muốn bán ngoài Store, vì IAP của Store không dùng được ngoài Store).
- **Tùy chọn mã nguồn mở `BFR.Core` (giấy phép MIT):** tăng uy tín và lượng sao trên GitHub, giúp người khác kiểm chứng độ an toàn; phần giao diện và tính năng Pro vẫn đóng. Cân nhắc kỹ trước khi quyết định (ảnh hưởng khả năng thương mại hóa).

## 13.4. ASO (tối ưu hiển thị trên Store)
- **Tên đề xuất:** `Batch File Renamer: Regex, Numbering & Preview` (hoặc tên thương hiệu riêng + phụ đề mô tả, để tránh trùng tên chung).
- **Mô tả ngắn:** Đổi tên hàng loạt tệp với Regex, số thứ tự, ngày tháng và cắt tỉa tên. Xem trước kết quả, phát hiện trùng tên và hoàn tác bất cứ lúc nào.
- **Từ khóa (mô tả chức năng, không dùng tên thương hiệu khác):** `batch rename`, `bulk rename files`, `rename multiple files`, `regex rename`, `file renamer`, `rename photos by date`, `add numbering to files`.
- **Bộ ảnh chụp màn hình (5 ảnh):**
  1. Ảnh chính: bảng "Tên cũ → Tên mới" trước/sau, chữ "Xem trước chính xác. Đổi tên an toàn."
  2. Chuỗi quy tắc và Rule Trace.
  3. Đánh số tự nhiên và ngày chụp EXIF.
  4. Phát hiện xung đột và hoàn tác nhiều bước.
  5. Preset tiếng Việt: bỏ dấu, dọn dẹp tên tệp.
- **Video demo 15–20 giây:** kéo 100 ảnh vào, chọn preset, xem preview, áp dụng, hoàn tác.

## 13.5. Go-To-Market (chi phí gần bằng 0)
1. Bài viết hướng dẫn tiếng Việt: "Cách đổi tên hàng loạt tệp, bỏ dấu tiếng Việt và đánh số thứ tự" (nhu cầu tìm kiếm thực tế, ít cạnh tranh nội dung tiếng Việt).
2. Chia sẻ GIF/video ngắn trên cộng đồng sinh viên, nhiếp ảnh, lập trình, tuân thủ nội quy từng nhóm và nêu rõ là sản phẩm của chính mình.
3. Giveaway mã Pro không ràng buộc đánh giá (không yêu cầu đánh giá 5 sao).
4. Đăng bài kỹ thuật về độ tin cậy (journal, property test, thuật toán hoán đổi tên) lên blog/GitHub để thu hút người dùng nâng cao và nhà tuyển dụng.
5. Liên kết chéo trong trang Giới thiệu với QAS và WPD.

## 13.6. KPI
| Chỉ số | Cách đo | Tham chiếu |
| :--- | :--- | :--- |
| Lượt cài mới/tháng | Partner Center | Theo kịch bản 13.2 |
| Tỷ lệ hoàn tất một lần đổi tên (mở app → áp dụng thành công) | Telemetry opt-in | Trên 60% |
| Tỷ lệ lỗi/hoàn tác bất thường | Telemetry opt-in + báo cáo | Dưới 1% lần áp dụng |
| Tỷ lệ chuyển đổi Pro | Partner Center | 1–3% |
| Điểm đánh giá Store | Partner Center | Từ 4,3 sao trở lên |

---

# PHẦN 14: LỘ TRÌNH PHÁT TRIỂN, PHẠM VI MVP VÀ QUẢN TRỊ RỦI RO

## 14.1. Nguyên tắc
Xây **lõi đúng đắn và an toàn trước (Core + test)**, giao diện sau. Vì mọi logic nằm trong `BFR.Core`, có thể kiểm chứng toàn bộ hành vi bằng test trước khi có giao diện. Ước tính cho **một lập trình viên**, đã gồm kiểm thử; nhân thêm 20–30% dự phòng nếu vừa học vừa làm.

## 14.2. Lộ trình
| Giai đoạn | Thời lượng | Phạm vi | Cổng nghiệm thu |
| :--- | :---: | :--- | :--- |
| **0. Prototype lõi** | 1 tuần | `FileItem`, `RulePipeline` với 3 quy tắc (Find/Replace, Numbering, Trim), `NameValidator`, `ConflictDetector`, `RenamePlanner` (chuỗi + vòng), `RenameExecutor` + journal, `IFileSystem` giả; prototype giao diện tối thiểu; benchmark 10.000/100.000 tệp; thử đọc EXIF; thử khung giao diện (WinUI 3 và WPF) với bảng ảo hóa 100.000 dòng | Qua property test INV-1..INV-6 ở mức prototype; có số đo hiệu năng thật; chốt khung giao diện |
| **1. MVP (Free)** | 4 tuần | Đủ quy tắc Free (Tìm/thay, Regex + Tester, Số thứ tự, Ngày tạo/sửa, Cắt tỉa, Chèn, Hoa/thường, Bỏ dấu, Mẫu tên), Preview thời gian thực, Rule Trace, xung đột, thực thi + journal + khôi phục, Undo 1 bước, kéo thả, preset dựng sẵn, tiếng Việt/Anh | Qua QA-01..08, 10, 11, 13; dogfooding 2 tuần |
| **2. Phát hành Free lên Store** | 2 tuần | Đóng gói MSIX, menu chuột phải (cũ và mới), Privacy Policy, ảnh/video Store, nộp duyệt | Qua kiểm duyệt; qua QA-12, 16, 17 |
| **3. v1.1 (bật Pro)** | 3–4 tuần | EXIF/media, đệ quy không giới hạn, Undo đa bước, preset không giới hạn, xuất báo cáo, CLI, tích hợp IAP | Qua QA-09, 14, 15; IAP chạy đúng |
| **4. v1.2** | 3–4 tuần | Watch Folder, preset chia sẻ/nhập, nhiều ngôn ngữ, tối ưu hiệu năng và tự động cập nhật bản portable | Qua rà soát hiệu năng và độ ổn định |

Tổng khoảng 13–15 tuần; **bản có thể phát hành đầu tiên sau khoảng 7 tuần** (Giai đoạn 0 đến 2).

## 14.3. Sổ rủi ro
| Mã | Rủi ro | Xác suất | Tác động | Giảm thiểu |
| :-: | :--- | :---: | :---: | :--- |
| R-01 | Mất/ghi đè tệp người dùng do lỗi logic | Thấp | Rất cao | Property test, fault injection, journal, không bao giờ ghi đè, dogfooding trên bản sao dữ liệu |
| R-02 | Người dùng cho rằng "đã có PowerRename miễn phí" | Cao | Trung bình | Bản Free đủ mạnh; khác biệt ở độ tin cậy, Rule Trace, tiếng Việt, Undo; truyền thông trung thực |
| R-03 | Tích hợp menu chuột phải Windows 11 phức tạp/dễ gãy | Trung bình | Trung bình | Prototype sớm; giữ phương án menu cũ dự phòng; kiểm tra theo tài liệu hiện hành |
| R-04 | Hiệu năng kém với số lượng lớn tệp | Trung bình | Trung bình | Ảo hóa danh sách, tính toán nền, benchmark trong CI |
| R-05 | Regex gây treo | Trung bình | Trung bình | `matchTimeout`, chạy nền, hủy được |
| R-06 | Tên sản phẩm trùng/khó phân biệt trên Store | Trung bình | Trung bình | Kiểm tra trademark, cân nhắc thương hiệu riêng + phụ đề mô tả |
| R-07 | Doanh thu thấp | Cao | Trung bình | Coi doanh thu là phụ; ưu tiên học tập và portfolio; cập nhật kế hoạch theo số liệu thật |
| R-08 | Quá tải phạm vi (nhiều dự án song song) | Cao | Cao | Phát hành tuần tự; tái dùng hạ tầng chung; đạt cổng nghiệm thu mới sang giai đoạn sau |
| R-09 | Đọc EXIF làm tải tệp OneDrive ngoài ý muốn | Thấp đến trung bình | Trung bình | Phát hiện placeholder, không đọc nội dung khi không cần, cảnh báo |

## 14.4. Ý tưởng mở rộng sau v1.2 (chưa cam kết)
- Gợi ý tên bằng mô hình ngôn ngữ chạy **cục bộ** (ví dụ gợi ý tên theo nội dung tài liệu/ảnh) với kiểm soát quyền riêng tư nghiêm ngặt và tắt mặc định.
- Quy tắc theo thẻ siêu dữ liệu âm nhạc (ID3: nghệ sĩ, album, số bài) và PDF (tiêu đề).
- Mẫu Regex cộng đồng, thư viện preset trực tuyến (cần máy chủ, chỉ cân nhắc khi có người dùng).
- Tích hợp PowerToys Run/Terminal qua CLI và URI.

---

# PHẦN 15: PHỤ LỤC

## 15.1. Thuật ngữ
| Thuật ngữ | Ý nghĩa |
| :--- | :--- |
| Rule Chain | Danh sách quy tắc áp dụng tuần tự |
| Rule Trace | Vết kết quả sau từng quy tắc để gỡ lỗi |
| Preview | Bảng kết quả dự kiến trước khi áp dụng |
| Write-Ahead Journal | Nhật ký ghi trước khi thực hiện thao tác, dùng để khôi phục |
| Natural sort | Sắp xếp "tự nhiên", coi `2` nhỏ hơn `10` |
| NFC/NFD | Dạng chuẩn hóa Unicode ghép sẵn / phân rã |
| Property-based test | Kiểm thử bằng dữ liệu ngẫu nhiên theo các tính chất bất biến |
| File ID | Mã định danh tệp trên NTFS, không đổi khi đổi tên |
| IExplorerCommand | Giao diện COM để thêm mục vào menu chuột phải kiểu mới của Explorer |

## 15.2. API và thư viện chính
| Nhóm | Thành phần |
| :--- | :--- |
| Tệp | `File.Move`, `Directory.Move`, `MoveFileEx`, `GetFileInformationByHandle(Ex)`, `FileSystemWatcher` (Watch Folder) |
| Văn bản | `Regex` (có `matchTimeout`), `string.Normalize`, `CharUnicodeInfo`, `StrCmpLogicalW` |
| Metadata | Thư viện đọc EXIF/media (ví dụ MetadataExtractor); Windows Property System cho ngày tạo media (kiểm chứng khi triển khai) |
| Tích hợp | `IExplorerCommand`, `IShellItemArray`, named pipe, Mutex |
| Cửa hàng | `Windows.Services.Store` |
| Kiểm thử | xUnit/NUnit, FsCheck (property test), BenchmarkDotNet |

## 15.3. Mã mẫu: kiểm tra tên hợp lệ
~~~csharp
static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
{
    "CON","PRN","AUX","NUL",
    "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
    "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
};

static NameIssue Validate(string fullName)
{
    if (string.IsNullOrWhiteSpace(fullName)) return NameIssue.Empty;
    if (fullName.Length > 255) return NameIssue.NameTooLong;
    foreach (var ch in fullName)
        if (ch < 32 || "<>:\"/\\|?*".IndexOf(ch) >= 0) return NameIssue.InvalidChar;
    if (fullName.EndsWith('.') || fullName.EndsWith(' ')) return NameIssue.TrailingDotSpace;
    var stem = fullName.Split('.')[0];          // "CON.txt" cũng bị tính là tên dành riêng
    if (Reserved.Contains(stem.TrimEnd(' '))) return NameIssue.ReservedName;
    return NameIssue.None;
}
~~~

## 15.4. Mã mẫu: sắp thứ tự thực thi và tên tạm (ý tưởng)
~~~csharp
// pairs: (From, To). Trả về danh sách bước có thứ tự an toàn.
static List<Step> Plan(IReadOnlyList<(string From, string To)> pairs)
{
    var byFrom = pairs.ToDictionary(p => p.From, StringComparer.OrdinalIgnoreCase);
    var steps = new List<Step>();
    var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 0=chưa, 1=đang, 2=xong

    void Visit((string From, string To) p)
    {
        state.TryGetValue(p.From, out var s);
        if (s == 2) return;
        if (s == 1)  // phát hiện vòng: đưa p về tên tạm trước
        {
            var temp = MakeTempName(p.From);
            steps.Add(new Step(p.From, temp, StepKind.ToTemp));
            pendingFromTemp.Add((temp, p.To));
            state[p.From] = 2;
            return;
        }
        state[p.From] = 1;
        // Nếu tên đích đang bị một tệp khác (cũng đang đổi) chiếm, xử lý tệp đó trước
        if (byFrom.TryGetValue(p.To, out var blocker)) Visit(blocker);
        if (state[p.From] != 2)
        {
            steps.Add(new Step(p.From, p.To, StepKind.Direct));
            state[p.From] = 2;
        }
    }

    foreach (var p in pairs) Visit(p);
    foreach (var (temp, to) in pendingFromTemp) steps.Add(new Step(temp, to, StepKind.FromTemp));
    return steps;
}
~~~
> Mã trên minh họa ý tưởng, cần hoàn thiện (khai báo `pendingFromTemp`, `MakeTempName`, xử lý đường dẫn theo thư mục) và được bao phủ bởi property test trước khi dùng thật.

## 15.5. Checklist trước khi bắt đầu code
- [ ] Làm Giai đoạn 0: prototype `BFR.Core` + test thuộc tính, ghi lại số đo hiệu năng thật.
- [ ] Chốt khung giao diện (WinUI 3 hay WPF) sau khi thử bảng 100.000 dòng.
- [ ] Xác minh tích hợp menu chuột phải Windows 11 (`IExplorerCommand` trong MSIX) bằng ví dụ nhỏ trên máy thật.
- [ ] Xác minh `StrCmpLogicalW` hoặc cài đặt natural sort tương đương cho thứ tự giống Explorer.
- [ ] Xác minh lại tính năng, giá và giấy phép của đối thủ (PowerRename và các công cụ khác); ghi lại nguồn.
- [ ] Chọn tên thương hiệu, kiểm tra trademark, đặt trước tên trên Partner Center.
- [ ] Soạn Chính sách quyền riêng tư và trang hỗ trợ.
- [ ] Chuẩn bị bộ dữ liệu kiểm thử (ảnh có/không có EXIF, tên tiếng Việt, tên "dấu rời", đường dẫn dài, tệp bị khóa).
- [ ] Quyết định có mở mã nguồn `BFR.Core` hay không.
- [ ] Dựng repo chung hạ tầng với QAS/WPD (Settings, Licensing, CI).

## 15.6. Cấu trúc thư mục mã nguồn đề xuất
~~~text
BatchFileRenamer/
├─ src/
│  ├─ BFR.Core/              (pipeline, quy tắc, token, xung đột, planner, executor, undo)
│  ├─ BFR.Core.Windows/      (triển khai IFileSystem thật, file ID, StrCmpLogicalW)
│  ├─ BFR.App/               (giao diện WinUI 3 hoặc WPF)
│  ├─ BFR.Cli/               (bfr.exe)
│  ├─ BFR.ShellExt/          (COM server IExplorerCommand cho menu Windows 11)
│  └─ BFR.Package/           (dự án đóng gói MSIX)
├─ tests/
│  ├─ BFR.Core.Tests/        (unit test từng quy tắc)
│  ├─ BFR.Core.PropertyTests/(FsCheck: INV-1..INV-6)
│  ├─ BFR.Core.FaultTests/   (IFileSystem giả: lỗi khóa, mất điện)
│  └─ BFR.Benchmarks/        (BenchmarkDotNet)
├─ docs/
│  ├─ privacy-policy.md
│  ├─ presets/               (preset dựng sẵn)
│  └─ benchmarks.md          (số đo thật)
└─ .github/workflows/
~~~
