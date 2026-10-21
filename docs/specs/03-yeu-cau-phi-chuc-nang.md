# 03. YÊU CẦU PHI CHỨC NĂNG (NON-FUNCTIONAL REQUIREMENTS)

Nguồn: `docs/batch_file_renamer_spec_v1.0.md` — Phần 4.

Tất cả con số dưới đây là **mục tiêu thiết kế (target)**, chưa phải số đo xác minh; xác minh ở Phần 12 và Giai đoạn 0.

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
