# 06. LÕI 2 — PREVIEW, PHÁT HIỆN XUNG ĐỘT, THỰC THỰC AN TOÀN VÀ UNDO

Nguồn: `docs/batch_file_renamer_spec_v1.0.md` — Phần 7.

---

## 6.1 NameValidator: tên mới hợp lệ khi…

| # | Điều kiện | Mã trạng thái |
| :-: | :--- | :--- |
| 1 | Không rỗng, không chỉ gồm khoảng trắng | `Empty` |
| 2 | Không chứa ký tự cấm của Windows (`< > : " / \ | ? *` và ký tự điều khiển 0–31) | `InvalidChar` |
| 3 | Không kết thúc bằng dấu chấm hoặc khoảng trắng | `TrailingDotSpace` |
| 4 | Không phải tên dành riêng của thiết bị: `CON, PRN, AUX, NUL, COM1–COM9, LPT1–LPT9` (kể cả khi có đuôi, ví dụ `CON.txt`) | `ReservedName` |
| 5 | Độ dài tên (gồm đuôi) không quá 255 ký tự (UTF-16) | `NameTooLong` |
| 6 | Độ dài đường dẫn đầy đủ không vượt giới hạn: 260 ký tự nếu hệ thống chưa bật hỗ trợ đường dẫn dài; kiểm tra chế độ hiện hành và dùng tiền tố `\\?\` khi cần | `PathTooLong` |

---

## 6.2 ConflictDetector: ba loại xung đột

1. **Trùng trong lô:** hai tệp trong cùng thư mục có tên mới giống nhau. **So sánh không phân biệt hoa/thường** theo quy tắc của Windows (`OrdinalIgnoreCase`; lưu ý NTFS còn có thể bật phân biệt hoa/thường theo thư mục, nên đọc thuộc tính này nếu cần chính xác hơn).
2. **Va chạm với tệp ngoài lô:** tên mới đã thuộc về một tệp tồn tại trong thư mục nhưng **không** nằm trong lô đang đổi (hoặc đã bị bỏ chọn).
3. **Chuỗi/vòng phụ thuộc trong lô:** tên mới của tệp A là tên cũ của tệp B (cũng đang đổi). Đây **không phải lỗi**; được `RenamePlanner` xử lý.

**Trường hợp chỉ đổi hoa/thường (`foo.txt` thành `Foo.txt`):** tên mới "trùng" tên cũ nếu so sánh không phân biệt hoa/thường nhưng thực chất là **cùng một tệp**. Để phân biệt, so sánh danh tính tệp (file ID: `GetFileInformationByHandle` hoặc `GetFileInformationByHandleEx`) thay vì chỉ so sánh chuỗi; tệp cùng danh tính được xem là chính nó, không phải xung đột.

---

## 6.3 RenamePlanner: sắp thứ tự thực thi

Đầu vào: tập cặp `(đường dẫn cũ → đường dẫn mới)` đã hợp lệ và không xung đột loại 1–2.

Thuật toán:
1. Dựng đồ thị có hướng: cạnh `A → B` nếu `đường dẫn mới của A` bằng `đường dẫn cũ của B` (B phải nhường chỗ trước).
2. Với phần **không có vòng** (chuỗi, ví dụ `a→b`, `b→c`, `c→d`): thực thi theo **thứ tự topo ngược** (đổi `c→d` trước, rồi `b→c`, rồi `a→b`).
3. Với **vòng** (ví dụ hoán đổi `a→b`, `b→a`): đổi một tệp trong vòng sang **tên tạm duy nhất** (`.bfr-{guid}.tmp`) trước; làm xong các bước còn lại của vòng; cuối cùng đổi tên tạm về tên đích.
4. Với **thư mục** (khi đổi cả thư mục): xử lý từ **lá lên gốc** (đổi tên tệp trong thư mục trước, rồi mới đổi tên thư mục) hoặc tính lại đường dẫn tương ứng sau mỗi bước, để tránh đường dẫn con bị vô hiệu.
5. Kế hoạch cuối là **danh sách bước tuần tự** (`Step { From, To, Kind: Direct | ToTemp | FromTemp }`) được ghi vào journal trước khi chạy.

---

## 6.4 RenameExecutor: thực thi có nhật ký ghi trước (Write-Ahead Journal)

Định dạng journal: tệp JSON Lines tại `%LocalAppData%\BatchFileRenamer\journal\{yyyyMMdd-HHmmss}-{id}.jsonl`.

```json
{"type":"header","id":"b7f1...","startedUtc":"2026-10-19T09:30:00Z","totalSteps":1200,"app":"1.0.0"}
{"type":"step","seq":1,"from":"D:\\Anh\\IMG_001.jpg","to":"D:\\Anh\\2024-05-01_001.jpg","kind":"Direct","status":"pending"}
{"type":"step","seq":1,"status":"done"}
{"type":"step","seq":2,"from":"...","to":"...","kind":"ToTemp","status":"pending"}
{"type":"footer","status":"completed","doneUtc":"2026-10-19T09:30:07Z"}
```

Quy trình mỗi bước:
1. Ghi dòng `pending` cho bước, **flush xuống đĩa** (`FileStream.Flush(true)`).
2. Thực hiện đổi tên bằng `File.Move`/`Directory.Move` (hoặc `MoveFileEx`; **không** dùng cờ ghi đè).
3. Ghi dòng `done` (hoặc `failed` kèm mã lỗi) và flush.
4. Cập nhật tiến trình lên giao diện (theo lô, không cập nhật từng dòng để tránh chậm).

Xử lý lỗi từng tệp: lỗi **khóa tệp (đang được tiến trình khác mở), từ chối quyền, tệp không còn tồn tại** → ghi `failed`, **tiếp tục** các bước độc lập (nếu người dùng chọn "Bỏ qua lỗi") hoặc **dừng và đề nghị hoàn tác phần đã làm**. Lỗi ở bước thuộc một chuỗi/vòng phụ thuộc → dừng chuỗi đó và hoàn tác riêng chuỗi.

Hủy giữa chừng: dừng sau bước đang chạy, ghi `footer` trạng thái `cancelled`, hiển thị "Đã đổi N/M tệp. Hoàn tác phần đã làm?".

---

## 6.5 Khôi phục sau sự cố (Crash Recovery)

Khi khởi động: quét thư mục journal; journal không có `footer` là **dở dang**. Giao diện đề xuất:

- **Hoàn tất:** chạy tiếp các bước còn `pending` (sau khi kiểm tra trạng thái thực của hệ thống tệp).
- **Khôi phục về ban đầu:** hoàn tác các bước đã `done` theo thứ tự ngược.

Với bước ở trạng thái `pending` không có `done`: kiểm tra thực tế (tệp đang ở `from` hay `to`) để biết bước đã xảy ra chưa, vì có thể sự cố xảy ra giữa lúc đổi tên và lúc ghi `done`.

---

## 6.6 UndoService

- Mỗi lần áp dụng thành công tạo một **mục lịch sử** (liên kết tới journal).
- Hoàn tác = chạy journal theo **thứ tự ngược**, đổi `to` về `from`.
- **Kiểm tra trước khi hoàn tác:** với mỗi bước, `to` còn tồn tại và `from` đang trống. Nếu người dùng đã di chuyển/đổi tên/xóa tệp sau đó, bước đó được báo "Không thể hoàn tác" kèm lý do, các bước còn lại vẫn xử lý (Hoàn tác từng phần), kết quả hiển thị rõ.
- Tùy chọn kiểm tra danh tính tệp (file ID) để chắc tệp tại `to` đúng là tệp đã được đổi.
- Lịch sử giữ 30 ngày (Pro) hoặc 1 lần gần nhất (Free); dọn tự động khi khởi động.

---

## 6.7 Các bất biến an toàn (Invariants) bắt buộc kiểm thử

| Mã | Bất biến |
| :-: | :--- |
| INV-1 | Tổng số tệp trong các thư mục liên quan **không đổi** sau khi áp dụng (không tạo thêm, không mất) |
| INV-2 | Nội dung từng tệp **không đổi** (kiểm tra bằng băm trong test) |
| INV-3 | Không có thao tác nào ghi đè tệp đang tồn tại |
| INV-4 | Tập tên cuối cùng bằng đúng tập tên mới trong preview |
| INV-5 | Hoàn tác ngay sau áp dụng đưa hệ thống tệp về đúng trạng thái ban đầu |
| INV-6 | Ở mọi điểm dừng giả lập (crash/mất điện) giữa chừng, journal đủ thông tin để hoàn tất hoặc khôi phục |
