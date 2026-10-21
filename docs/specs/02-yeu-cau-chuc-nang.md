# 02. ĐẶC TẢ TÍNH NĂNG CHI TIẾT (FUNCTIONAL REQUIREMENTS)

Nguồn: `docs/batch_file_renamer_spec_v1.0.md` — Phần 3.

---

## 2.1 Module A — Nạp tệp và bộ lọc

| Mã | Yêu cầu | Chi tiết và tiêu chí chấp nhận |
| :-: | :--- | :--- |
| FR-A1 | Nạp tệp bằng kéo thả, nút "Thêm tệp/Thêm thư mục", dán đường dẫn, hoặc từ menu chuột phải Explorer | Chấp nhận nhiều nguồn cùng lúc; loại trùng lặp. Nạp 10.000 tệp dưới 2 giây trên SSD (không tính EXIF). |
| FR-A2 | Quét thư mục con (đệ quy) | Tùy chọn mức sâu tối đa; có nút hủy khi quét lớn. |
| FR-A3 | Bao gồm/loại trừ | Bộ lọc theo phần mở rộng, mẫu glob (`*.jpg`), Regex, kích thước, ngày tạo/sửa. Mặc định **loại trừ tệp ẩn và tệp hệ thống**. |
| FR-A4 | Đổi tên thư mục | Công tắc "Gồm cả thư mục" (mặc định tắt). Khi bật, xử lý thứ tự từ trong ra ngoài để đường dẫn con không bị hỏng. |
| FR-A5 | Sắp xếp danh sách nạp | Theo tên (natural sort), ngày tạo, ngày sửa, kích thước, ngày EXIF, thứ tự nạp, thứ tự chọn thủ công (kéo thả để đổi vị trí). Thứ tự này quyết định số thứ tự được đánh. |
| FR-A6 | Bỏ chọn từng tệp | Có thể bỏ tích từng dòng để loại khỏi lần đổi tên mà không xóa khỏi danh sách. |

---

## 2.2 Module B — Chuỗi quy tắc (Rule Chain)

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

---

## 2.3 Hệ thống Token dùng trong chuỗi thay thế và Mẫu tên

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

**Quy ước:** token không hợp lệ hoặc thiếu dữ liệu (ví dụ không có EXIF) **không âm thầm biến mất**; preview hiển thị trạng thái "Thiếu dữ liệu: {taken}" và áp dụng chuỗi dự phòng đã chọn.

---

## 2.4 Module C — Preview, áp dụng và hoàn tác

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

---

## 2.5 Module D — Preset, tích hợp và tự động hóa

| Mã | Yêu cầu | Chi tiết |
| :-: | :--- | :--- |
| FR-D1 | Preset | Lưu chuỗi quy tắc thành preset có tên; thư viện preset dựng sẵn; nhập/xuất tệp `.bfrpreset` (JSON). |
| FR-D2 | Menu chuột phải Explorer | Mục "Đổi tên hàng loạt với BFR" cho tệp/thư mục đã chọn; hỗ trợ cả menu mới của Windows 11. |
| FR-D3 | CLI | `bfr rename <đường dẫn...> --preset <tên> [--dry-run] [--recursive] [--yes]`. |
| FR-D4 | Watch Folder (Pro, v1.2) | Theo dõi một thư mục và tự áp dụng preset cho tệp mới (có độ trễ chờ tệp ghi xong, hạn chế thao tác lặp, nhật ký). |
| FR-D5 | Đa ngôn ngữ | Tiếng Việt và tiếng Anh ở v1.0. |
| FR-D6 | Tự cập nhật | Qua Microsoft Store (bản MSIX); bản portable hiển thị thông báo có bản mới (opt-in). |

---

## 2.6 Phân tách Free và Pro

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

**Lý do thiết kế:** giữ chức năng cốt lõi và độ an toàn ở Free để tạo lòng tin và đánh giá tốt; thu phí ở nhu cầu chuyên sâu (EXIF, chuỗi dài, tự động hóa). Không khóa tính năng an toàn (journal, khôi phục) sau thanh toán.
