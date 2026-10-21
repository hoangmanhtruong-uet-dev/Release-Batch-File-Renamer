# BỘ PROMPT GIAI ĐOẠN 0 — BATCH FILE RENAMER (BFR)

Cách dùng: mở Claude Code trong thư mục gốc repo (có `docs/specs/`). Dán **Prompt 0** trước (một lần mỗi phiên), rồi dán lần lượt **T1 → T9**. Chỉ sang task sau khi task trước **xanh hết test** và bạn đã đọc diff.

Mục tiêu Giai đoạn 0 (spec mục 14.2): chứng minh lõi đúng và an toàn, có số đo hiệu năng thật, chốt khung giao diện. Chưa làm UI hoàn chỉnh, menu chuột phải, Store, Pro.

---

## PROMPT 0 — Bối cảnh và luật làm việc (dán đầu phiên)

```
Bạn là kỹ sư .NET chính của dự án Batch File Renamer (BFR), công cụ đổi tên hàng loạt tệp cho Windows.

ĐỌC TRƯỚC KHI LÀM BẤT CỨ GÌ (theo thứ tự):
1. docs/specs/00-index.md
2. docs/specs/04-kien-truc-he-thong.md
3. docs/specs/05-pipeline-quy-tac-doi-ten.md
4. docs/specs/06-preview-xung-dot-thuc-thi-undo.md
Các file khác chỉ đọc khi task nhắc tới. Spec là nguồn sự thật; nếu bạn thấy spec sai hoặc mơ hồ, DỪNG và nêu câu hỏi, không tự đoán.

LUẬT BẤT BIẾN:
- Mọi logic nằm trong BFR.Core (net8.0, thuần .NET, KHÔNG phụ thuộc UI, KHÔNG gọi System.IO trực tiếp: mọi thao tác tệp đi qua IFileSystem).
- Mọi IRule là hàm thuần. Dữ liệu ngoài (now, hạt giống ngẫu nhiên, EXIF) chỉ đi qua RuleContext đã đóng băng một lần mỗi lượt preview.
- Không bao giờ ghi đè tệp có sẵn. Không có đường code nào gọi Move với overwrite=true.
- Mọi test dùng IFileSystem giả (in-memory) hoặc thư mục tạm duy nhất mỗi lần chạy. Cấm test chạm vào thư mục người dùng.
- Regex luôn có matchTimeout; mọi so sánh chuỗi dùng culture rõ ràng (Invariant/Ordinal), không dùng culture hiện hành.
- C# 12, nullable enabled, warnings as errors, .editorconfig, đặt tên tiếng Anh trong code, comment tiếng Việt khi giải thích lý do thiết kế.

QUY TRÌNH MỖI TASK:
1. Nhắc lại task bằng 3 dòng và liệt kê file sẽ tạo/sửa.
2. Viết test TRƯỚC (hoặc song song), chạy thấy đỏ nếu hợp lý.
3. Cài đặt tối thiểu để test xanh, rồi refactor.
4. Chạy `dotnet build` và `dotnet test` toàn solution; dán kết quả cuối cùng.
5. Commit nhỏ, message dạng `feat(core): ...` / `test(core): ...`.
6. Báo cáo: đã làm gì, chưa làm gì, điểm nào lệch spec và vì sao, rủi ro còn lại.
Không làm thêm tính năng ngoài task. Không sang task khác khi chưa được yêu cầu.
```

---

## T1 — Dựng solution và khung kiểm thử

```
TASK T1: Dựng khung solution cho Giai đoạn 0.

Tạo (theo mục 15.6 của docs/specs/14-phu-luc.md, chỉ phần cần cho Giai đoạn 0):
- src/BFR.Core (net8.0 classlib)
- src/BFR.Core.Windows (net8.0, để trống, chỉ dùng cho P/Invoke sau này)
- tests/BFR.Core.Tests (xUnit), tests/BFR.Core.PropertyTests (xUnit + FsCheck.Xunit), tests/BFR.Core.FaultTests (xUnit), tests/BFR.Benchmarks (BenchmarkDotNet)
- Directory.Build.props: Nullable enable, TreatWarningsAsErrors, LangVersion 12, AnalysisLevel latest
- .editorconfig, .gitignore, global.json (pin SDK 8.x)
- GitHub Actions: build + test trên windows-latest và ubuntu-latest (Core phải chạy được trên cả hai)

Trong BFR.Core tạo giao diện IFileSystem tối thiểu:
  Exists(path), IsDirectory(path), EnumerateEntries(dir), Move(from,to) (KHÔNG overwrite, ném IOException nếu đích tồn tại), GetFileId(path), OpenAppend(path) cho journal, FlushToDisk(stream).
Và InMemoryFileSystem (có thể inject lỗi: ném IOException/UnauthorizedAccessException tại lần gọi thứ N, mô phỏng "mất điện" bằng cách ném một CrashException đặc biệt tại bước N). So sánh tên KHÔNG phân biệt hoa/thường như NTFS, nhưng giữ nguyên dạng hoa/thường đã lưu; GetFileId ổn định khi đổi tên.

Tiêu chí xong: solution build sạch, 1 test mẫu cho InMemoryFileSystem (đổi tên chỉ hoa/thường giữ nguyên FileId; Move vào tên đã tồn tại thì ném lỗi; không có overwrite), CI xanh.
```

---

## T2 — Mô hình dữ liệu, RuleContext và TokenEngine

```
TASK T2: Mô hình lõi và TokenEngine (spec 05, mục 6.1–6.2 và 3.3 token).

Cài đặt trong BFR.Core:
- record FileItem, RenameState, PreviewItem, PreviewResult (như spec 05; thêm Status enum: Ok, Unchanged, Conflict, InvalidName, TooLong, Locked, MissingData).
- interface IRule { Id; Enabled; RenameState Apply(FileItem, RenameState, RuleContext) } và RuleContext bất biến chứa: Now (đóng băng), RandomSeed, resolver metadata (delegate trả ngày/EXIF đã đọc sẵn, trong T2 chỉ cần giao diện), tổng số tệp, hàm tra GroupIndex.
- TokenEngine: phân tích và thay các token {name} {ext} {orig} {n} {n:3} {parent} {created:fmt} {modified:fmt} {now:fmt} {size} {rand:N}. Token sai cú pháp hoặc thiếu dữ liệu KHÔNG được âm thầm biến mất: trả về lỗi có cấu trúc để preview hiển thị trạng thái MissingData. {rand:N} dùng RandomSeed nên cùng ngữ cảnh cho cùng kết quả. Ký tự không hợp lệ trong định dạng ngày (ví dụ ':' trong HH:mm) phải được phát hiện và báo.
- RulePipeline.Run(files, rules, context) -> PreviewResult, ghi Trace sau từng quy tắc cho mỗi tệp.

Test: bảng dữ liệu cho từng token; tính tất định (chạy 2 lần cùng context ra cùng kết quả); token lỗi; Trace đúng thứ tự.
Tiêu chí xong: test xanh, không có chỗ nào gọi DateTime.Now/Random không seed trong BFR.Core (thêm một test tĩnh hoặc analyzer ban các API đó).
```

---

## T3 — Ba quy tắc đầu: Find/Replace + Regex, Numbering, Trim

```
TASK T3: Cài đặt 3 quy tắc theo spec 05, mục 6.3.1, 6.3.2, 6.3.4 và 6.4.

1) FindReplaceRule: văn bản thuần (tự escape) hoặc Regex .NET; $1, ${ten}, token BFR xử lý SAU khi thay Regex; bỏ qua hoa/thường; lần đầu/tất cả; phạm vi Tên/Đuôi/Cả hai. Regex tạo một lần mỗi lượt với matchTimeout 250 ms; hết giờ thì dòng đó nhận Status riêng "Regex quá chậm" thay vì ném lỗi làm sập pipeline. Mẫu sai cú pháp trả lỗi có vị trí.
2) NumberingRule: start, step (cho phép âm), width (cố định hoặc Auto theo tổng số tệp), position (đầu/cuối/vị trí N/thay toàn bộ), separator, resetMode (None/ByFolder/ByTokenChange), kind Decimal và Alpha. value = start + step * GroupIndex.
3) TrimRule: xóa tập ký tự, xóa N ký tự đầu/cuối, xóa khoảng [a,b), gộp khoảng trắng, cắt hai đầu, bỏ dấu chấm/khoảng trắng cuối. Vị trí tính theo text element (System.Globalization.StringInfo) để không cắt ngang ký tự ghép.
4) NaturalComparer: so sánh tự nhiên (Bai 2 < Bai 10) khớp thứ tự Explorer. Trong BFR.Core cài bản thuần managed; để chỗ cắm cho StrCmpLogicalW ở BFR.Core.Windows. Có test đối chiếu bộ ví dụ cố định (số đệm 0, chữ hoa/thường, dấu cách, Unicode).

Test bắt buộc: tên tiếng Việt có dấu, NFC và NFD của cùng một chữ, chuỗi rỗng, đuôi rỗng, tên chỉ gồm dấu chấm, emoji, regex backtracking nặng (phải timeout, không treo), step âm, resetMode ByTokenChange đánh lại từ 1 khi token đổi.
Tiêu chí xong: test xanh; một test chứng minh regex độc hại như (a+)+$ trên chuỗi dài không chặn quá ~1 giây.
```

---

## T4 — NameValidator và ConflictDetector

```
TASK T4: Kiểm tra tên hợp lệ và phát hiện xung đột (spec 06, mục 7.1–7.2).

NameValidator: 6 điều kiện (rỗng, ký tự cấm kể cả ký tự điều khiển 0–31, kết thúc bằng '.' hoặc khoảng trắng, tên thiết bị dành riêng CON/PRN/AUX/NUL/COM1-9/LPT1-9 KỂ CẢ khi có đuôi như CON.txt, tên dài hơn 255 UTF-16, đường dẫn dài). Trả NameIssue enum. Giới hạn đường dẫn lấy từ một tham số (mặc định 260, có chế độ longPathAware).

ConflictDetector (đầu vào: danh sách cặp cũ→mới theo từng thư mục, tập tệp đang tồn tại qua IFileSystem, tập tệp bị bỏ chọn):
- Loại 1: trùng trong lô (so sánh không phân biệt hoa/thường).
- Loại 2: va chạm với tệp NGOÀI lô hoặc tệp đã bị bỏ chọn.
- Loại 3 (chuỗi/vòng a→b, b→a): KHÔNG phải lỗi, để RenamePlanner xử lý.
- Trường hợp chỉ đổi hoa/thường (foo.txt -> Foo.txt): so sánh bằng GetFileId, cùng danh tính thì là chính nó, không phải xung đột.
Kết quả gắn Status vào từng PreviewItem và cờ "chặn áp dụng" ở PreviewResult.

Test: ma trận đủ các trường hợp trên + thư mục khác nhau cùng tên mới (không xung đột) + tên mới trùng tên tệp bỏ chọn.
Tiêu chí xong: test xanh; tài liệu một bảng quyết định (comment đầu file) liệt kê từng ca và kết quả mong đợi.
```

---

## T5 — RenamePlanner (phần khó nhất)

```
TASK T5: RenamePlanner (spec 06, mục 7.3). Đây là thuật toán quan trọng nhất về an toàn dữ liệu, làm cẩn thận và KHÔNG sao chép nguyên văn mã mẫu ở docs/specs/14-phu-luc.md mục 15.4 (mã đó chưa hoàn chỉnh: thiếu khai báo và xử lý thư mục). Tự thiết kế lại và giải thích.

Yêu cầu:
- Đầu vào: tập cặp (đường dẫn cũ -> đường dẫn mới) đã qua ConflictDetector, không có xung đột loại 1–2.
- Đầu ra: danh sách Step { From, To, Kind: Direct | ToTemp | FromTemp } theo thứ tự thực thi an toàn.
- Chuỗi (a→b, b→c, c→d): thứ tự topo ngược. Vòng (a↔b, vòng 3 tệp): đưa MỘT phần tử sang tên tạm duy nhất (.bfr-{guid}.tmp, cùng thư mục, kiểm tra chưa tồn tại), hoàn tất vòng, rồi FromTemp.
- Thư mục: xử lý từ lá lên gốc, hoặc tính lại đường dẫn sau mỗi bước, để đường dẫn tệp con không bị vô hiệu khi đổi tên thư mục cha cùng lô.
- Tất định: cùng đầu vào cho cùng danh sách bước (tên tạm sinh từ một hàm inject được để test).
- Không dùng đệ quy sâu có thể tràn stack với lô 100.000 phần tử (dùng duyệt lặp).

Property test (FsCheck) bắt buộc, chạy trên InMemoryFileSystem:
- Sinh ngẫu nhiên tập tệp + ánh xạ tên mới (có cố ý tạo chuỗi, vòng, hoán đổi, đổi hoa/thường).
- INV-1: số tệp không đổi. INV-2: nội dung (băm) từng tệp không đổi. INV-3: không bước nào ghi đè (Move không bao giờ chạm tên đã tồn tại). INV-4: tập tên cuối đúng bằng tập tên mới. INV-5 sẽ kiểm ở T6.
- Ca cố định: a↔b, a→b→c, vòng 3, vòng 2 chồng chuỗi, thư mục cha+con cùng đổi.
Tiêu chí xong: property test chạy tối thiểu 1.000 ca mỗi tính chất và xanh; không có tệp tạm sót lại sau mọi ca.
```

---

## T6 — RenameExecutor, journal, khôi phục, Undo, fault injection

```
TASK T6: Thực thi an toàn (spec 06, mục 7.4–7.7).

Cài đặt:
- Journal JSON Lines (header, step pending/done/failed, footer) ghi TRƯỚC khi thực hiện mỗi bước và flush xuống đĩa (qua IFileSystem.FlushToDisk). Định dạng đúng như spec.
- RenameExecutor: chạy danh sách Step, báo tiến trình theo lô, hỗ trợ CancellationToken (dừng sau bước đang chạy, footer cancelled), lỗi từng tệp (IOException, UnauthorizedAccessException, FileNotFound) ghi failed. Chế độ "bỏ qua lỗi" và "dừng + hoàn tác phần đã làm"; lỗi trong một chuỗi/vòng thì dừng và hoàn tác riêng chuỗi đó.
- Recovery: khi khởi động quét journal không có footer. Với bước pending không có done, kiểm tra thực tế (tệp đang ở From hay To) để biết bước đã xảy ra chưa. Hai lựa chọn: Hoàn tất hoặc Khôi phục về ban đầu.
- UndoService: chạy journal theo thứ tự ngược; kiểm tra trước từng bước (To còn tồn tại, From đang trống; tùy chọn so FileId); hoàn tác từng phần kèm lý do nếu người dùng đã tự đổi/xóa tệp.
- Dry-run: toàn bộ quy trình nhưng không gọi Move.

Fault test (BFR.Core.FaultTests):
- Giả lập CrashException tại MỌI chỉ số bước của lô 200 tệp có chuỗi/vòng, sau đó chạy Recovery cả hai hướng: hệ thống tệp phải về trạng thái hợp lệ (INV-1..4), không tệp nào mất/ghi đè, không tên tạm sót lại.
- Lỗi khóa tệp ở bước giữa chuỗi.
- INV-5: áp dụng rồi Undo ngay đưa hệ thống tệp về đúng ban đầu (property test).
- Undo khi người dùng đã đổi tên một tệp trong số đó.
Tiêu chí xong: toàn bộ fault test xanh; báo cáo một bảng "điểm dừng giả lập -> kết quả khôi phục".
```

---

## T7 — Benchmark hiệu năng và thử đọc EXIF

```
TASK T7: Đo số liệu thật cho NFR-1..NFR-5 (spec 03) và thử EXIF.

1) BenchmarkDotNet, bộ dữ liệu sinh ngẫu nhiên có seed cố định: 10.000 và 100.000 tên tệp trong cây thư mục nông và sâu. Đo: RulePipeline.Run với chuỗi 3 quy tắc, ConflictDetector, RenamePlanner (cả ca nhiều vòng). Ghi thời gian và bộ nhớ cấp phát.
2) Thêm test hiệu năng "chặn hồi quy" trong CI với ngưỡng rộng (ví dụ preview 10.000 tệp dưới 1 giây trên runner) để bắt hồi quy lớn, không dùng ngưỡng sát.
3) Spike EXIF: dùng thư viện MetadataExtractor (kiểm tra giấy phép và ghi lại). Đọc DateTimeOriginal, dự phòng DateTimeDigitized, xử lý EXIF thiếu hoặc "0000:00:00". Đo 1.000 ảnh mẫu (tự sinh ảnh nhỏ có và không có EXIF). Ghi chú: EXIF thường không có múi giờ.
4) Tạo docs/benchmarks.md ghi: máy chạy (CPU, RAM, ổ đĩa), phiên bản .NET, bảng số đo, so sánh với mục tiêu NFR, kết luận đạt/không đạt. Không làm đẹp số liệu; nếu không đạt thì nói thẳng và đề xuất hướng tối ưu.
```

---

## T8 — Spike khung giao diện: WinUI 3 và WPF (chạy trên máy Windows)

```
TASK T8: Spike giao diện để CHỐT khung UI (spec 04, mục 5.1). Không làm app hoàn chỉnh.

Tạo hai dự án thử nghiệm riêng trong thư mục spikes/ (không nằm trong solution chính):
- spikes/Ui.WinUI3 và spikes/Ui.Wpf.
Mỗi dự án chỉ cần: một bảng 3 cột (Tên cũ, Tên mới, Trạng thái) ảo hóa, nạp 100.000 dòng giả từ BFR.Core (chạy RulePipeline thật trên dữ liệu sinh), một ô nhập Regex và một ô thay thế, preview cập nhật sau debounce 150 ms trên luồng nền có CancellationToken, thanh trượt cuộn mượt.
Đo và ghi lại cho CẢ HAI: thời gian nạp 100.000 dòng, độ mượt khi cuộn (cảm nhận + số khung hình nếu đo được), RAM Working Set, thời gian khởi động lạnh, độ khó khi đóng gói MSIX thử, độ phức tạp tích hợp menu chuột phải sau này (chỉ ghi nhận, chưa làm).
Kết luận bằng một bảng so sánh ngắn và MỘT khuyến nghị có lý do. Nếu không chạy được trên môi trường hiện tại (không có Windows), nói rõ và dừng ở phần chuẩn bị mã, không bịa số đo.
```

---

## T9 — Báo cáo cổng nghiệm thu Giai đoạn 0

```
TASK T9: Báo cáo nghiệm thu Giai đoạn 0, ghi vào docs/phase0-report.md.

Nội dung:
1. Danh sách INV-1..INV-6: mỗi bất biến có test nào chứng minh, số ca đã chạy, kết quả.
2. Bảng số đo hiệu năng thật so với NFR (lấy từ docs/benchmarks.md).
3. Kết quả spike giao diện và khung UI được chọn kèm lý do.
4. Danh sách điểm lệch so với spec (nếu có) và đề xuất sửa spec.
5. Rủi ro còn lại, việc chưa làm, thứ tự đề xuất cho Giai đoạn 1 (MVP Free).
6. Một dòng kết luận: ĐẠT hay CHƯA ĐẠT cổng Giai đoạn 0, theo tiêu chí ở spec 13, mục 14.2.
Không viết tích cực quá mức. Điều gì chưa kiểm chứng thì ghi "chưa kiểm chứng".
```

---

## Ghi chú cho bạn

- **Thứ tự cứng:** T5 và T6 là phần quyết định độ an toàn. Đừng bỏ qua property test và fault test dù muốn đi nhanh.
- **Môi trường:** T1–T7 chạy được trên Linux hoặc Windows. T8 (WinUI 3, WPF) cần máy Windows.
- **Review diff:** sau T5 và T6, tự đọc kỹ mã `RenamePlanner` và `RenameExecutor`. Đây là hai chỗ lỗi nhỏ cũng có thể làm mất tệp người dùng.
- **Sao lưu:** trước khi thử trên tệp thật ở bất kỳ giai đoạn nào, hãy sao lưu thư mục đó.
