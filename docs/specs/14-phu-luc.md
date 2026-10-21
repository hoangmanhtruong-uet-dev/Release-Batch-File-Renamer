# 14. PHỤ LỤC

Nguồn: `docs/batch_file_renamer_spec_v1.0.md` — Phần 15.

---

## 14.1 Thuật ngữ

| Thuật ngữ | Ý nghĩa |
| :--- | :--- |
| Rule Chain | Danh sách quy tắc áp dụng tuần tự |
| Rule Trace | Vết kết quả sau từng quy tắc để gỡ lỗi |
| Preview | Bảng kết quả dự kiến trước khi áp dụng |
| Write-Ahead Journal | Nhật ký ghi trước khi thực hiện thao tác, dùng để khôi phục |
| Natural sort | Sắp xếp "tự nhiên", coi `2` nhỏ hơn `10` |
| NFC/NFD | Dạng chuẩn hóa Unicode ghép sẵn / phân rã |
| Property-based test | Kiểm thử bằng dữ liệu ngẫu nhiên theo các tính chất bất biến |
| File ID | Mã định danh tệp trên NTFS, không đổi khi đổi tên |
| IExplorerCommand | Giao diện COM để thêm mục vào menu chuột phải kiểu mới của Explorer |

---

## 14.2 API và thư viện chính

| Nhóm | Thành phần |
| :--- | :--- |
| Tệp | `File.Move`, `Directory.Move`, `MoveFileEx`, `GetFileInformationByHandle(Ex)`, `FileSystemWatcher` (Watch Folder) |
| Văn bản | `Regex` (có `matchTimeout`), `string.Normalize`, `CharUnicodeInfo`, `StrCmpLogicalW` |
| Metadata | Thư viện đọc EXIF/media (ví dụ MetadataExtractor); Windows Property System cho ngày tạo media (kiểm chứng khi triển khai) |
| Tích hợp | `IExplorerCommand`, `IShellItemArray`, named pipe, Mutex |
| Cửa hàng | `Windows.Services.Store` |
| Kiểm thử | xUnit/NUnit, FsCheck (property test), BenchmarkDotNet |

---

## 14.3 Mã mẫu: kiểm tra tên hợp lệ

```csharp
static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
{
    "CON","PRN","AUX","NUL",
    "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
    "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
};

static NameIssue Validate(string fullName)
{
    if (string.IsNullOrWhiteSpace(fullName)) return NameIssue.Empty;
    if (fullName.Length > 255) return NameIssue.NameTooLong;
    foreach (var ch in fullName)
        if (ch < 32 || "<>:\"/\\|?*".IndexOf(ch) >= 0) return NameIssue.InvalidChar;
    if (fullName.EndsWith('.') || fullName.EndsWith(' ')) return NameIssue.TrailingDotSpace;
    var stem = fullName.Split('.')[0];          // "CON.txt" cũng bị tính là tên dành riêng
    if (Reserved.Contains(stem.TrimEnd(' '))) return NameIssue.ReservedName;
    return NameIssue.None;
}
```

---

## 14.4 Mã mẫu: sắp thứ tự thực thi và tên tạm (ý tưởng)

```csharp
// pairs: (From, To). Trả về danh sách bước có thứ tự an toàn.
static List<Step> Plan(IReadOnlyList<(string From, string To)> pairs)
{
    var byFrom = pairs.ToDictionary(p => p.From, StringComparer.OrdinalIgnoreCase);
    var steps = new List<Step>();
    var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 0=chưa, 1=đang, 2=xong

    void Visit((string From, string To) p)
    {
        state.TryGetValue(p.From, out var s);
        if (s == 2) return;
        if (s == 1)  // phát hiện vòng: đưa p về tên tạm trước
        {
            var temp = MakeTempName(p.From);
            steps.Add(new Step(p.From, temp, StepKind.ToTemp));
            pendingFromTemp.Add((temp, p.To));
            state[p.From] = 2;
            return;
        }
        state[p.From] = 1;
        // Nếu tên đích đang bị một tệp khác (cũng đang đổi) chiếm, xử lý tệp đó trước
        if (byFrom.TryGetValue(p.To, out var blocker)) Visit(blocker);
        if (state[p.From] != 2)
        {
            steps.Add(new Step(p.From, p.To, StepKind.Direct));
            state[p.From] = 2;
        }
    }

    foreach (var p in pairs) Visit(p);
    foreach (var (temp, to) in pendingFromTemp) steps.Add(new Step(temp, to, StepKind.FromTemp));
    return steps;
}
```

> Mã trên minh họa ý tưởng, cần hoàn thiện (khai báo `pendingFromTemp`, `MakeTempName`, xử lý đường dẫn theo thư mục) và được bao phủ bởi property test trước khi dùng thật.

---

## 14.5 Checklist trước khi bắt đầu code

- [ ] Làm Giai đoạn 0: prototype `BFR.Core` + test thuộc tính, ghi lại số đo hiệu năng thật.
- [ ] Chốt khung giao diện (WinUI 3 hay WPF) sau khi thử bảng 100.000 dòng.
- [ ] Xác minh tích hợp menu chuột phải Windows 11 (`IExplorerCommand` trong MSIX) bằng ví dụ nhỏ trên máy thật.
- [ ] Xác minh `StrCmpLogicalW` hoặc cài đặt natural sort tương đương cho thứ tự giống Explorer.
- [ ] Xác minh lại tính năng, giá và giấy phép của đối thủ (PowerRename và các công cụ khác); ghi lại nguồn.
- [ ] Chọn tên thương hiệu, kiểm tra trademark, đặt trước tên trên Partner Center.
- [ ] Soạn Chính sách quyền riêng tư và trang hỗ trợ.
- [ ] Chuẩn bị bộ dữ liệu kiểm thử (ảnh có/không có EXIF, tên tiếng Việt, tên "dấu rời", đường dẫn dài, tệp bị khóa).
- [ ] Quyết định có mở mã nguồn `BFR.Core` hay không.
- [ ] Dựng repo chung hạ infrastructure với QAS/WPD (Settings, Licensing, CI).

---

## 14.6 Cấu trúc thư mục mã nguồn đề xuất

```
BatchFileRenamer/
├─ src/
│  ├─ BFR.Core/              (pipeline, quy tắc, token, xung đột, planner, executor, undo)
│  ├─ BFR.Core.Windows/      (triển khai IFileSystem thật, file ID, StrCmpLogicalW)
│  ├─ BFR.App/               (giao diện WinUI 3 hoặc WPF)
│  ├─ BFR.Cli/               (bfr.exe)
│  ├─ BFR.ShellExt/          (COM server IExplorerCommand cho menu Windows 11)
│  └─ BFR.Package/           (dự án đóng gói MSIX)
├─ tests/
│  ├─ BFR.Core.Tests/        (unit test từng quy tắc)
│  ├─ BFR.Core.PropertyTests/(FsCheck: INV-1..INV-6)
│  ├─ BFR.Core.FaultTests/   (IFileSystem giả: lỗi khóa, mất điện)
│  └─ BFR.Benchmarks/        (BenchmarkDotNet)
├─ docs/
│  ├─ privacy-policy.md
│  ├─ presets/               (preset dựng sẵn)
│  └─ benchmarks.md          (số đo thật)
└─ .github/workflows/
```
