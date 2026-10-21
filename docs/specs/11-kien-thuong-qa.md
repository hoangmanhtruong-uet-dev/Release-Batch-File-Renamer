# 11. CHIẾN LƯỢC KIỂM THỬ VÀ MA TRẬN QA

Nguồn: `docs/batch_file_renamer_spec_v1.0.md` — Phần 12.

---

## 11.1 Chiến lược

- **Unit test** cho từng quy tắc (bảng dữ liệu vào/ra, gồm tiếng Việt, ký tự đặc biệt, Unicode, chuỗi rỗng).
- **Property-based test** (ví dụ FsCheck) cho các bất biến INV-1..INV-6: sinh ngẫu nhiên danh sách tên, chuỗi quy tắc, kiểm tra preview = áp dụng, hoán đổi/chuỗi/vòng, hoàn tác về trạng thái ban đầu.
- **Fault injection** qua `IFileSystem` giả: ném lỗi khóa tệp, từ chối quyền, "mất điện" tại từng điểm giữa các bước; kiểm tra khôi phục.
- **Benchmark** cho NFR-1..NFR-5 trong CI.
- **Dogfooding:** dùng cho ảnh/tài liệu thật của bản thân ít nhất 2 tuần trước khi nộp Store (sao lưu trước khi thử!).

---

## 11.2 Ma trận QA

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
