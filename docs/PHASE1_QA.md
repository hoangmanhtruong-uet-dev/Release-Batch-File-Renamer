# BÁO CÁO QA — GIAI ĐOẠN 1 (MVP FREE)

Ngày: 2026-10-20  
Phạm vi: `docs/specs/13-lo-trinh-mvp-ru-ro.md` — Giai đoạn 1.  
Môi trường: Windows, .NET 8, `dotnet test -c Release`.

## Kết quả tự động

| Hạng mục | Kết quả |
| :--- | :--- |
| Build toàn solution | 0 lỗi, 0 cảnh báo |
| `BFR.Core.Tests` | 74/74 pass |
| `BFR.Core.PropertyTests` | 7/7 pass (INV-1..INV-6 + QA-01 1.000 ca) |
| Tổng | **81/81 pass** |
| Ứng dụng WPF | Khởi động ổn định, không lỗi XAML/resource |
| CLI | `preset list`, `rename --dry-run --format json` chạy đúng; JSON sạch trên stdout |

### Kiểm thử thủ công nhanh (đã chạy)

| Ca | Kết quả |
| :--- | :--- |
| Rename `a.txt → b.txt` khi `b.txt` đã tồn tại (ngoài lô) | Bị chặn, exit code 2, thông báo `Tên mới trùng tệp/thư mục ngoài lô` |
| `rename --dry-run --format json` | stdout chỉ có JSON, thông báo dry-run ra stderr |
| Khởi động WPF | Cửa sổ mở, không lỗi XAML/resource |

## Ma trận QA

| Mã | Nội dung | Trạng thái | Bằng chứng |
| :--- | :--- | :--- | :--- |
| QA-01 | Preview khớp áp dụng | **Đạt (tự động)** | `QA01_Preview_matches_apply_over_1000_random_cases`, `QA01_preview_matches_apply_for_40_files` |
| QA-02 | Natural sort + đánh số | **Đạt (tự động)** | `QA02_natural_sort_numbering` |
| QA-03 | Bỏ dấu tiếng Việt | **Đạt (tự động)** | `Slugify_removes_diacritics_and_dashes`, `Dd_maps_correctly` |
| QA-04 | Hoán đổi/chuỗi/vòng | **Đạt (tự động)** | `QA04_swap_two_files`, `QA04_chain_three_files`, `QA04_cycle_three_files` |
| QA-05 | Mất điện giả lập | **Đạt (tự động)** | `QA05_complete_and_rollback_after_crash_at_every_step` (200 tệp × mọi điểm dừng) |
| QA-06 | Hoàn tác | **Đạt (tự động)** | `Undo_restores_original_state`, `QA06_partial_undo_reports_reason_for_changed_file` |
| QA-07 | Xung đột | **Đạt (tự động)** | `QA07_conflicts_are_classified`, `QA07_same_new_name_in_different_directories_is_not_a_conflict`, `QA07_outside_batch_index_detects_existing_file_in_same_directory`, `QA07_outside_batch_index_detects_existing_directory_for_folder_batch`; UI hiển thị cột Chi tiết |
| QA-08 | Regex an toàn | **Đạt (tự động)** | `QA08_invalid_regex_does_not_crash`, `QA08_catastrophic_regex_times_out_without_crashing` |
| QA-09 | EXIF/ngày | **Đạt một phần** | `QA09_metadata_requirement_is_detected`, `QA09_exif_reader_returns_empty_for_non_image`; cần thử bộ ảnh thật |
| QA-10 | Quy mô lớn | **Đạt tầng Core** | Benchmark NFR-1: 1.000 tệp 1,94 ms; 10.000 tệp 34,37 ms; 100.000 tệp 548 ms. Cần đo UI trên máy thật |
| QA-11 | Tệp bị khóa/chỉ đọc/ẩn | **Đạt tầng Core** | `QA11_locked_file_fails_individually_and_others_continue`; cần thử khóa tệp thật |
| QA-13 | Đường dẫn dài/Unicode | **Đạt tầng Core** | `QA13_unicode_and_emoji_are_valid`, `QA13_path_over_260_is_blocked`; cần thử NTFS thật |
| QA-15 | CLI | **Đạt một phần** | `--dry-run`, `--format json` sạch, mã thoát 0/1/2/3/4; cần bổ sung test tự động cho CLI |

## Ngoài phạm vi Giai đoạn 1

- QA-12 (menu chuột phải), QA-14 (đám mây/ổ mạng), QA-16 (MSIX), QA-17 (trợ năng): thuộc Giai đoạn 2.
- Undo đa bước, preset không giới hạn, báo cáo, IAP: thuộc Pro.

## Việc cần làm trước khi đóng cổng Giai đoạn 1

1. Chạy bộ ảnh thật cho QA-09 (có EXIF, không EXIF, EXIF sai, ảnh chỉnh sửa).
2. Đo QA-10 trên UI với 100.000 tệp trong cây sâu; xác nhận hủy được.
3. Thử QA-11/QA-13 trên NTFS thật (tệp bị khóa, đường dẫn >260, emoji/CJK).
4. Dogfooding 2 tuần trên bản sao dữ liệu thật.
5. Bổ sung test tự động cho CLI (QA-15).
