# 09. EDGE CASES, BẢO MẬT, QUYỀN RIÊNG TƯ VÀ TƯƠNG THÍCH

Nguồn: `docs/batch_file_renamer_spec_v1.0.md` — Phần 10.

---

## 9.1 Ma trận 18 trường hợp khó

| # | Tình huống | Hệ quả nếu bỏ qua | Cách xử lý |
| :-: | :--- | :--- | :--- |
| 1 | Hai tệp có cùng tên mới (khác hoa/thường) | Một tệp thất bại hoặc bị ghi đè | `ConflictDetector` so sánh không phân biệt hoa/thường; đánh dấu Xung đột trước khi áp dụng |
| 2 | Hoán đổi tên (`a↔b`) hoặc chuỗi (`a→b→c`) | Lỗi "tên đã tồn tại" giữa chừng | `RenamePlanner` dùng thứ tự topo + tên tạm |
| 3 | Chỉ đổi hoa/thường (`foo`→`Foo`) | Bị báo trùng với chính nó | So sánh theo file ID, không chỉ theo chuỗi |
| 4 | Tên dành riêng (`CON`, `NUL`, `COM1`…) | Đổi tên thất bại hoặc tạo tệp khó xóa | `NameValidator` chặn |
| 5 | Tên kết thúc bằng dấu chấm/khoảng trắng | Windows tự cắt hoặc từ chối, gây lệch preview | Chặn hoặc tự cắt có hiển thị rõ ràng trong preview |
| 6 | Đường dẫn dài hơn 260 ký tự | Lỗi khi đổi tên | Kiểm tra giới hạn hiện hành, dùng tiền tố `\\?\` khi cho phép; báo `PathTooLong` |
| 7 | Tệp đang được ứng dụng khác mở/khóa | Bước đổi tên thất bại | Bắt lỗi từng tệp, ghi `failed`, cho phép tiếp tục hoặc dừng/hoàn tác chuỗi |
| 8 | Tệp chỉ đọc, tệp ẩn/hệ thống | Hành vi bất ngờ | Mặc định loại tệp ẩn/hệ thống khỏi lô; tệp chỉ đọc: đổi tên được nhưng hiển thị biểu tượng nhắc |
| 9 | Regex nguy hiểm (catastrophic backtracking) | Treo ứng dụng | `matchTimeout`, chạy nền, hủy được |
| 10 | Tên Unicode "dấu rời" (NFD) từ macOS/iOS | Cùng một chữ nhưng hai dạng mã hóa, tìm/thay sai | Quy tắc chuẩn hóa NFC; cảnh báo khi phát hiện tên NFD |
| 11 | Đuôi tệp bị đổi nhầm | Tệp không mở được | Cờ cho phép đổi đuôi, mặc định tắt; cảnh báo khi áp dụng |
| 12 | Quá nhiều tệp chọn từ Explorer (hàng nghìn) | Dòng lệnh vượt giới hạn độ dài (khoảng 32.767 ký tự) | Truyền danh sách qua tệp tạm hoặc named pipe, không qua tham số |
| 13 | Menu chuột phải gọi lặp lại cho từng tệp (kiểu động từ cũ) | Mở hàng trăm cửa sổ | Mô hình "người thu thập": instance đầu chờ 300–500 ms để gom đủ đường dẫn, các instance sau chuyển tiếp rồi thoát (single instance) |
| 14 | Tệp OneDrive/đám mây ở trạng thái "chỉ trực tuyến" (placeholder) | Đọc EXIF làm tải tệp về ngoài ý muốn | Phát hiện thuộc tính placeholder/recall; **không đọc nội dung** khi không cần, cảnh báo khi quy tắc cần EXIF |
| 15 | Ổ đĩa mạng/USB chậm hoặc ngắt kết nối giữa chừng | Treo hoặc dở dang | Hết giờ theo bước, bắt lỗi, journal đảm bảo khôi phục; hiển thị trạng thái rõ |
| 16 | Thư mục đang đổi tên là thư mục cha của tệp khác trong lô | Đường dẫn con bị hỏng | Xử lý từ lá lên gốc |
| 17 | Ngày EXIF thiếu hoặc sai (ví dụ `0000:00:00`) | Tên chứa giá trị vô nghĩa | Kiểm tra hợp lệ, dùng chuỗi dự phòng và ghi chú nguồn trong trạng thái dòng |
| 18 | Chọn nhầm thư mục hệ thống (ví dụ `C:\Windows`, `Program Files`) | Hỏng hệ điều hành/ứng dụng | Danh sách thư mục được bảo vệ; chặn hoặc yêu cầu xác nhận mạnh; mặc định chặn |

---

## 9.2 Bảo mật và quyền riêng tư

- **Hoàn toàn cục bộ:** không gửi tên tệp, đường dẫn, nội dung hay metadata ra ngoài thiết bị. Telemetry (nếu có) là opt-in, chỉ gồm thông tin ẩn danh (phiên bản, bản Windows, mã lỗi) và có công tắc rõ ràng.
- **Không yêu cầu quyền Administrator.** Không đổi tên tệp mà người dùng không có quyền; không cố nâng quyền.
- **Chống truyền dữ liệu qua IPC:** kênh named pipe giữa các instance chỉ chấp nhận kết nối từ cùng người dùng (ACL) và kiểm tra dữ liệu đầu vào.
- **Bộ lọc đầu vào:** đường dẫn được chuẩn hóa và kiểm tra để tránh các trường hợp đường dẫn gây hiểu nhầm (ví dụ chứa `..`, ký tự điều khiển).
- **Chính sách quyền riêng tư** bắt buộc khi nộp Store, nêu rõ ứng dụng đọc metadata tệp (EXIF) chỉ để dựng tên và không rời khỏi máy.

---

## 9.3 Tương thích

| Hạng mục | Windows 10 (19041+) | Windows 11 |
| :--- | :--- | :--- |
| Menu chuột phải kiểu cũ (động từ registry) | Có | Có (trong "Hiện thêm tùy chọn") |
| Menu chuột phải kiểu mới (IExplorerCommand + MSIX) | Không áp dụng | Có |
| Đường dẫn dài | Cần bật chính sách hệ thống và manifest `longPathAware` | Tương tự |
| Giao diện WinUI 3 | Có (cần runtime phù hợp) | Có |
