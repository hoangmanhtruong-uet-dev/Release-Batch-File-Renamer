# CHỈ MỤC ĐẶC TẢ — BATCH FILE RENAMER (BFR)

Nguồn gốc: `docs/batch_file_renamer_spec_v1.0.md` (Master Blueprint A–Z, phiên bản 1.0.0).  
Dự án: **Batch File Renamer** — công cụ đổi tên hàng loạt tệp/thư mục trên Windows 10/11.  
Kênh chính: Microsoft Store (MSIX); kênh phụ: GitHub Releases (portable + CLI).  
Mô hình: Freemium (lõi miễn phí + mua chữa trọn đời bản Pro qua MSIX IAP).

| File | Nội dung | Nguồn trong blueprint |
| :--- | :--- | :--- |
| `01-tong-quan-tam-nhin-dinh-vi.md` | Tổng quan, tầm nhìn, phân tích thị trường, người dùng, đối thủ, định vị | Phần 1, Phần 2 |
| `02-yeu-cau-chuc-nang.md` | Đặc tả tính năng chi tiết (FR-A..D, phân tách Free/Pro) | Phần 3 |
| `03-yeu-cau-phi-chuc-nang.md` | Yêu cầu phi chức năng (NFR-1..10) | Phần 4 |
| `04-kien-truc-he-thong.md` | Kiến trúc tổng thể, công nghệ, thành phần, luồng xử lý | Phần 5 |
| `05-pipeline-quy-tac-doi-ten.md` | Lõi 1 — Rule Pipeline, mô hình dữ liệu, từng quy tắc, natural sort | Phần 6 |
| `06-preview-xung-dot-thuc-thi-undo.md` | Lõi 2 — Preview, phát hiện xung đột, thực thi an toàn, journal, Undo, khôi phục | Phần 7 |
| `07-thiet-ke-giao-dien-ux.md` | Thiết kế giao diện và trải nghiệm (wireframe, Regex Tester, xác nhận áp dụng) | Phần 8 |
| `08-mo-hinh-du-lieu-preset-cli.md` | Mô hình dữ liệu, Preset (`.bfrpreset`), cấu hình, CLI | Phần 9 |
| `09-edge-case-bao-mat-rieng-tu.md` | Edge cases (18 trường hợp), bảo mật, quyền riêng tư, tương thích | Phần 10 |
| `10-packaging-msix-menu-chu-phai-store-ci-cd.md` | Đóng gói MSIX, menu chuột phải, chứng nhận Store, CI/CD | Phần 11 |
| `11-kien-thuong-qa.md` | Chiến lược kiểm thử, ma trận QA (QA-01..17) | Phần 12 |
| `12-thuong-mai-hoa-gtm.md` | Thương mại hóa, ASO, Go-To-Market, KPI | Phần 13 |
| `13-lo-trinh-mvp-ru-ro.md` | Lộ trình phát triển, phạm vi MVP, sổ rủi ro, ý tưởng mở rộng | Phần 14 |
| `14-phu-luc.md` | Thuật ngữ, API/thư viện chính, mã mẫu, checklist trước khi code, cấu trúc thư mục | Phần 15 |

## Nguyên tắc dùng tài liệu
- Mọi con số hiệu năng và kinh doanh là **mục tiêu thiết kế (target)**, chưa phải số đo xác minh.
- Logic nghiệp vụ nằm trong `BFR.Core` (thuần .NET, không phụ thuộc giao diện) → giao diện/CLI đều gọi chung Core, hành vi giống hệt nhau.
- An toàn dữ liệu là tối ưu cao nhất: 0 tệp bị mất/ghi đòi, preview = áp dụng (chung một hàm tính toán, ngữ cảnh đóng băng), journal ghi trước + Undo.
