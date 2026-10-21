# 04. KIẾN TRÚC HỆ THỐNG TỔNG THỂ

Nguồn: `docs/batch_file_renamer_spec_v1.0.md` — Phần 5.

---

## 4.1 Lựa chọn công nghệ

Khác QAS/WPD, BFR **không phải app chạy nền thường trú**: người dùng mở khi cần, nên RAM nền ít quan trọng; ưu tiên **độ đúng đắn, kiểm thử được và giao diện tốt**.

| Phương án | Ưu điểm | Nhược điểm |
| :--- | :--- | :--- |
| **A. .NET 8 + WinUI 3 (giao diện) + thư viện Core thuần .NET + CLI** | Giao diện Fluent chuẩn Windows 11, hợp MSIX, tích hợp menu chuột phải mới thuận lợi, tái dùng C# đang có | Chỉ Windows; WinUI 3 có đường cong học và một số hạn chế về công cụ |
| B. .NET 8 + WPF + thư viện WPF-UI | Ổn định, cộng đồng lớn, bảng dữ liệu lớn dễ ảo hóa | Giao diện kém "hiện đại" hơn nếu không tùy biến |
| C. Avalonia UI + Core thuần .NET | Đa nền tảng (Windows/macOS/Linux) | Tích hợp menu chuột phải phải làm riêng từng hệ điều hành; giao diện không "bản địa" |

**Quyết định đề xuất:** Phương án A hoặc B, **với kiến trúc tách lớp bắt buộc**: toàn bộ logic nằm trong `BFR.Core` (không phụ thuộc giao diện), nên đổi khung giao diện sau này (hoặc sang Avalonia) vẫn giữ nguyên lõi và bộ kiểm thử. Chốt A hay B sau Giai đoạn 0.

---

## 4.2 Sơ đồ kiến trúc

```text
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
```

---

## 4.3 Trách nhiệm từng thành phần

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

---

## 4.4 Mô hình luồng (Threading)

- Giao diện chỉ hiển thị và điều phối; **mọi tính toán Preview chạy nền** bằng `Task` có `CancellationToken`.
- Mỗi thay đổi quy tắc hủy tính toán đang chạy và bắt đầu tính toán mới (debounce 150 ms).
- Kết quả Preview trả về theo lô và cập nhật `ObservableCollection` ảo hóa.
- Thực thi đổi tên chạy trên một luồng riêng, tuần tự theo kế hoạch (đổi tên là thao tác metadata nhanh; song song hóa không mang lại nhiều lợi ích và làm phức tạp việc khôi復 phục).
