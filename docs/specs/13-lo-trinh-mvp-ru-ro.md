# 13. LỘ TRÌNH PHÁT TRIỂN, PHẠM VI MVP VÀ QUẢN TRỊ RỦI RO

Nguồn: `docs/batch_file_renamer_spec_v1.0.md` — Phần 14.

---

## 13.1 Nguyên tắc

Xây **lõi đúng đắn và an toàn trước (Core + test)**, giao diện sau. Vì mọi logic nằm trong `BFR.Core`, có thể kiểm chứng toàn bộ hành vi bằng test trước khi có giao diện.  
Ước tính cho **một lập trình viên**, đã gồm kiểm thử; nhân thêm 20–30% dự phòng nếu vừa học vừa làm.

---

## 13.2 Lộ trình

| Giai đoạn | Thời lượng | Phạm vi | Cổng nghiệm thu |
| :--- | :---: | :--- | :--- |
| **0. Prototype lõi** | 1 tuần | `FileItem`, `RulePipeline` với 3 quy tắc (Find/Replace, Numbering, Trim), `NameValidator`, `ConflictDetector`, `RenamePlanner` (chuỗi + vòng), `RenameExecutor` + journal, `IFileSystem` giả; prototype giao diện tối thiểu; benchmark 10.000/100.000 tệp; thử đọc EXIF; thử khung giao diện (WinUI 3 và WPF) với bảng ảo hóa 100.000 dòng | Qua property test INV-1..INV-6 ở mức prototype; có số đo hiệu năng thật; chốt khung giao diện |
| **1. MVP (Free)** | 4 tuần | Đủ quy tắc Free (Tìm/thay, Regex + Tester, Số thứ tự, Ngày tạo/sửa, Cắt tỉa, Chèn, Hoa/thường, Bỏ dấu, Mẫu tên), Preview thời gian thực, Rule Trace, xung đột, thực thi + journal + khôi phục, Undo 1 bước, kéo thả, preset dựng sẵn, tiếng Việt/Anh | Qua QA-01..08, 10, 11, 13; dogfooding 2 tuần |
| **2. Phát hành Free lên Store** | 2 tuần | Đóng gói MSIX, menu chuột phải (cũ và mới), Privacy Policy, ảnh/video Store, nộp duyệt | Qua kiểm duyệt; qua QA-12, 16, 17 |
| **3. v1.1 (bật Pro)** | 3–4 tuần | EXIF/media, đệ quy không giới hạn, Undo đa bước, preset không giới hạn, xuất báo cáo, CLI, tích hợp IAP | Qua QA-09, 14, 15; IAP chạy đúng |
| **4. v1.2** | 3–4 tuần | Watch Folder, preset chia sẻ/nhập, nhiều ngôn ngữ, tối ưu hiệu năng và tự động cập nhật bản portable | Qua rà soát hiệu năng và độ ổn định |

Tổng khoảng **13–15 tuần**; **bản có thể phát hành đầu tiên sau khoảng 7 tuần** (Giai đoạn 0 đến 2).

---

## 13.3 Sổ rủi ro

| Mã | Rủi ro | Xác suất | Tác động | Giảm thiểu |
| :-: | :--- | :---: | :---: | :--- |
| R-01 | Mất/ghi đè tệp người dùng do lỗi logic | Thấp | Rất cao | Property test, fault injection, journal, không bao giờ ghi đè, dogfooding trên bản sao dữ liệu |
| R-02 | Người dùng cho rằng "đã có PowerRename miễn phí" | Cao | Trung bình | Bản Free đủ mạnh; khác biệt ở độ tin cậy, Rule Trace, tiếng Việt, Undo; truyền thông trung thực |
| R-03 | Tích hợp menu chuột phải Windows 11 phức tạp/dễ gãy | Trung bình | Trung bình | Prototype sớm; giữ phương án menu cũ dự phòng; kiểm tra theo tài liệu hiện hành |
| R-04 | Hiệu năng kém với số lượng lớn tệp | Trung bình | Trung bình | Ảo hóa danh sách, tính toán nền, benchmark trong CI |
| R-05 | Regex gây treo | Trung bình | Trung bình | `matchTimeout`, chạy nền, hủy được |
| R-06 | Tên sản phẩm trùng/khó phân biệt trên Store | Trung bình | Trung bình | Kiểm tra trademark, cân nhắc thương hiệu riêng + phụ đề mô tả |
| R-07 | Doanh thu thấp | Cao | Trung bình | Coi doanh thu là phụ; ưu tiên học tập và portfolio; cập nhật kế hoạch theo số liệu thật |
| R-08 | Quá tải phạm vi (nhiều dự án song song) | Cao | Cao | Phát hành tuần tự; tái dùng hạ infrastructure chung; đạt cổng nghiệm thu mới sang giai đoạn sau |
| R-09 | Đọc EXIF làm tải tệp OneDrive ngoài ý muốn | Thấp đến trung bình | Trung bình | Phát hiện placeholder, không đọc nội dung khi không cần, cảnh báo |

---

## 13.4 Ý tưởng mở rộng sau v1.2 (chưa cam kết)

- Gợi ý tên bằng mô hình ngôn ngữ chạy **cục bộ** (ví dụ gợi ý tên theo nội dung tài liệu/ảnh) với kiểm soát quyền riêng tư nghiêm ngặt và tắt mặc định.
- Quy tắc theo thẻ siêu dữ liệu âm nhạc (ID3: nghệ sĩ, album, số bài) và PDF (tiêu đề).
- Mẫu Regex cộng đồng, thư viện preset trực tuyến (cần máy chủ, chỉ cân nhắc khi có người dùng).
- Tích hợp PowerToys Run/Terminal qua CLI và URI.
