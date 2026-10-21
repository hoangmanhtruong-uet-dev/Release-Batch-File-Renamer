# 07. THIẾT KẾ GIAO DIỆN VÀ TRẢI NGHIỆM (UI/UX)

Nguồn: `docs/batch_file_renamer_spec_v1.0.md` — Phần 8.

---

## 7.1 Nguyên tắc

- Một cửa sổ chính, bố cục 3 vùng: **Danh sách/Preview** (giữa), **Chuỗi quy tắc** (trái hoặc phải), **Trạng thái và nút Áp dụng** (dưới).
- Giao diện Fluent theo chủ đề sáng/tối hệ thống; phông `Segoe UI Variable`; đầy đủ phím tắt và hỗ trợ trình đọc màn hình.
- Người mới: màn hình rỗng có vùng kéo thả lớn và 3 preset gợi ý. Người nâng cao: chuỗi quy tắc, Regex tester, CLI.
- Mọi thao tác phá hủy dữ liệu (áp dụng lô lớn) có xác nhận tóm tắt.

---

## 7.2 Màn hình chính (wireframe)

```text
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
```

---

## 7.3 Hộp thoại Regex Tester

```text
+--------------------------------------------------------------+
| Mẫu Regex:      ^(\d+)_(.+)\.log$              [x] Bỏ qua hoa/thường |
| Thay bằng:      $2_$1.log                                       |
| Chuỗi thử:      20240501_server.log                              |
|--------------------------------------------------------------|
| Khớp:   [20240501]_[server].log   Nhóm 1: 20240501  Nhóm 2: server |
| Kết quả: server_20240501.log                                    |
| [Dùng cho tất cả tệp]    [Thư viện mẫu v]                        |
+--------------------------------------------------------------+
```

---

## 7.4 Hộp thoại xác nhận áp dụng

```text
+---------------------------------------------------------+
|  Áp dụng đổi tên 118 tệp?                               |
|  - 3 thư mục sẽ bị tác động                              |
|  - 2 tệp đổi phần mở rộng (jpeg -> jpg)                  |
|  - Có thể hoàn tác trong mục "Hoàn tác"                  |
|             [Xem lại]            [Áp dụng]               |
+---------------------------------------------------------+
```

---

## 7.5 Màn hình Lịch sử và Hoàn tác

Danh sách các lần áp dụng (thời điểm, số tệp, preset đã dùng, trạng thái), nút Hoàn tác từng lần, xem chi tiết từng bước và lý do nếu hoàn tác từng phần.

---

## 7.6 Hiệu năng giao diện

- Bảng Preview **ảo hóa** (chỉ dựng dòng đang hiển thị), cập nhật theo lô; hỗ trợ 100.000 dòng.
- Hiển thị thanh tiến trình khi quét lớn hoặc đọc EXIF; có nút Hủy.
- Không khóa giao diện khi tính Preview; hiển thị trạng thái "Đang tính…" mờ ở các dòng chưa xong.
