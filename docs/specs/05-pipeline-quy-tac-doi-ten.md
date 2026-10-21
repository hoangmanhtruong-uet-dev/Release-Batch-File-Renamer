# 05. LÕI 1 — PIPELINE QUY TẮC ĐỔI TÊN VÀ ĐẶC TẢ TỪNG QUY TẮC

Nguồn: `docs/batch_file_renamer_spec_v1.0.md` — Phần 6.

---

## 5.1 Mô hình dữ liệu cơ bản

```csharp
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
```

**Nguyên tắc vàng:** mọi quy tắc là **hàm thuần**. `RuleContext` chứa các giá trị được **đóng băng một lần cho mỗi lượt Preview** (thời điểm hiện tại `now`, hạt giống ngẫu nhiên, dữ liệu EXIF đã đọc). Nhờ đó Preview và Áp dụng cho ra kết quả giống hệt nhau. Khi bấm Áp dụng, dùng **chính ngữ cảnh đã đóng băng của lượt Preview đang hiển thị**, không tính toán lại với `now` mới.

---

## 5.2 Thứ tự xử lý của một lượt Preview

1. Lọc và sắp xếp danh sách `FileItem` (xác định `Index`, `GroupIndex`).
2. Đóng băng `RuleContext` (thời điểm, hạt giống, dữ liệu metadata cần dùng).
3. Với từng tệp: khởi tạo `RenameState` từ tên/đuôi hiện tại, chạy lần lượt các quy tắc đang bật, ghi vết.
4. Ghép tên cuối `Name + "." + Extension` (bỏ dấu chấm nếu đuôi rỗng), đưa vào `NameValidator` và `ConflictDetector`.
5. Trả về `PreviewResult` gồm: danh sách `PreviewItem` (tên cũ, tên mới, trạng thái, vết) và kết luận chung (có chặn áp dụng không).

---

## 5.3 Đặc tả chi tiết từng quy tắc

### 5.3.1 Tìm và thay / Regex

| Tham số | Giá trị |
| :--- | :--- |
| Mẫu (pattern) | Văn bản thuần (được thoát ký tự đặc biệt tự động) hoặc Regex .NET |
| Thay thế | Chuỗi gồm `$1`, `${ten}`, `$&` và token BFR (xử lý token **sau** khi thay Regex) |
| Tùy chọn | Bỏ qua hoa/thường; chỉ lần khớp đầu / mọi lần khớp; phạm vi (Tên / Đuôi / Cả hai) |
| Cài đặt an toàn | `Regex` được tạo một lần mỗi lượt Preview với `matchTimeout` mặc định 250 ms; hết giờ thì dòng đó báo "Regex quá chậm" thay vì treo ứng dụng; chặn mẫu không hợp lệ ngay khi gõ và hiển thị lỗi tại chỗ |
| Văn hóa | `CultureInfo.InvariantCulture` để kết quả không phụ thuộc cấu hình máy |

**Regex Tester** (giao diện): ô nhập chuỗi thử, tô sáng khớp và nhóm bắt, hiển thị chuỗi thay thế kết quả; nút "Dùng cho tất cả tệp".

### 5.3.2 Số thứ tự (Numbering)

| Tham số | Mô tả |
| :--- | :--- |
| `start`, `step` | Giá trị bắt đầu và bước nhảy (có thể âm) |
| `width` | Độ rộng đệm 0 (ví dụ 3 cho `007`); chế độ "Tự động" tính theo tổng số tệp (1.000 tệp thì đệm 4 chữ số) |
| `position` | Đầu tên, cuối tên, vị trí N, hoặc thay toàn bộ tên |
| `separator` | Ký tự ngăn giữa số và phần còn lại |
| `resetMode` | Không đặt lại / đặt lại theo từng thư mục / đặt lại khi giá trị của một token thay đổi (ví dụ theo `{created:yyyy-MM-dd}`, tức đánh số lại từ 1 mỗi ngày) |
| `sortBy` | Xem 5.5; mặc định là thứ tự hiển thị hiện tại của danh sách |
| `kind` | Thập phân (mặc định), chữ cái (a, b, …, z, aa), La Mã (Pro, tùy chọn) |

Giá trị số của tệp thứ `i` trong nhóm: `value = start + step * GroupIndex` (với `resetMode = theo thư mục` thì `GroupIndex` là chỉ số trong thư mục).

### 5.3.3 Ngày tháng

- **Nguồn:** `Created`, `Modified`, `Taken` (EXIF `DateTimeOriginal`, dự phòng `DateTimeDigitized`), `Media` (ngày tạo media, Pro), `Now`.
- **Chuỗi dự phòng:** người dùng chọn thứ tự, ví dụ `Taken → Created → Modified`. Trạng thái dòng ghi rõ nguồn đã dùng ("từ EXIF", "dự phòng: ngày tạo").
- **Múi giờ:** mặc định giờ địa phương; tùy chọn UTC. EXIF thường **không có múi giờ**, coi là giờ địa phương của máy chụp và ghi chú điều này trong giao diện.
- **Định dạng:** chuỗi định dạng ngày .NET (`yyyy-MM-dd_HHmmss`); xem trước định dạng ngay khi gõ; chặn ký tự không hợp lệ cho tên tệp (ví dụ `:` trong `HH:mm`) và gợi ý thay thế.
- **Lưu ý ngày tạo:** ngày tạo trên Windows có thể thay đổi khi sao chép/tải tệp (không phải ngày "gốc"); giao diện nên nhắc điều này khi người dùng chọn nguồn Created.

### 5.3.4 Cắt tỉa

Các thao tác (bật/tắt riêng, thực hiện theo thứ tự cố định trong quy tắc): (1) xóa ký tự thuộc tập chỉ định, (2) xóa N ký tự đầu/cuối, (3) xóa theo khoảng vị trí `[a, b)`, (4) gộp khoảng trắng liên tiếp, (5) cắt khoảng trắng đầu/cuối, (6) xóa dấu chấm/khoảng trắng ở cuối tên. Mọi vị trí tính theo **phần tử văn bản (text element)** để không cắt ngang một ký tự có dấu ghép.

### 5.3.5 Đổi hoa/thường

Dùng `CultureInfo.InvariantCulture` (hoặc văn hóa `vi-VN` khi chọn) cho tiêu đề; Title Case có danh sách từ ngoại lệ (không đổi `PDF`, `USB`, `iPhone`…). Cẩn thận ký tự đặc biệt (ví dụ chữ `İ` tiếng Thổ Nhĩ Kỳ) bằng cách không dùng văn hóa hiện hành mặc định.

### 5.3.6 Bỏ dấu và chuẩn hóa

```csharp
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
```

Ngoài ra: chuẩn hóa NFC (xử lý tên tệp "dấu rời" sinh ra từ macOS/iOS khiến cùng một chữ có hai dạng mã hóa khác nhau), thay ký tự không hợp lệ `< > : " / \ | ? *` và ký tự điều khiển bằng ký tự thay thế do người dùng chọn.

### 5.3.7 Phần mở rộng

Mặc định **bảo toàn** phần mở rộng; mọi quy tắc khác chỉ tác động lên phần Tên trừ khi người dùng chọn rõ phạm vi. Đổi đuôi sang loại khác (ví dụ `.jpeg` thành `.jpg`) cần bật cờ "Cho phép đổi phần mở rộng"; khi dùng, hiển thị cảnh báo một lần trong lượt áp dụng.

---

## 5.4 Sắp xếp khi đánh số: natural sort

Sắp xếp theo tên phải dùng **so sánh tự nhiên** (`Bai 2` đứng trước `Bai 10`). Dùng `StrCmpLogicalW` của Windows (hoặc cài đặt tương đương trong mã) để khớp với thứ tự của Explorer; tùy chọn so sánh theo thứ tự thường (ordinal) cho người dùng nâng cao.
