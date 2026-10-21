# BÁO CÁO NGHIỆM THU QA GIAI ĐOẠN 2 (PHASE 2 QA)

Nguồn: `docs/specs/11-kien-thuong-qa.md` và `docs/specs/13-lo-trinh-mvp-ru-ro.md`.  
Cổng nghiệm thu Giai đoạn 2: **QA-12, QA-16, QA-17 và sẵn sàng nộp duyệt Store**.

> **Kết luận nhanh:** phần kỹ thuật đã đạt 96/96 test tự động, build 0 warning/0 error, MSIX self-contained tạo và validate thành công, COM out-of-process hoạt động với `IShellItemArray` thật.  
> Tuy nhiên **cổng phát hành Store chưa thể đóng** vì còn thiếu chứng chỉ ký, Partner Center, URL privacy policy công khai và các bài kiểm thử thủ công trên Explorer Windows 10/11.

---

## 1. Môi trường và bằng chứng

| Hạng mục | Giá trị |
| :--- | :--- |
| Ngày kiểm thử | 20/10/2026 |
| Build | `Release`, x64, .NET 8 |
| Kết quả build | **0 warning / 0 error** |
| Unit tests | **89/89 pass** (`BFR.Core.Tests`) |
| Property tests | **7/7 pass** (`BFR.Core.PropertyTests`, 1.000 case mỗi property) |
| Tổng test | **96/96 pass** |
| Package | `artifacts/BatchFileRenamer-1.0.0.msix` (71.734.607 byte ≈ 71,7 MB, self-contained, chưa ký) |
| SHA-256 gói | `1F0BED612E1A7DA83CF930419395A638C47DB969E39FC95BDC6DA09A34254305` |
| Layout MSIX | 480 tệp, 169.320.661 byte (trước nén) |
| Portable | `artifacts/BatchFileRenamer-1.0.0-portable-win-x64.zip` (70.517.351 byte) |
| COM smoke | **PASS** trên layout self-contained |
| MakeAppx | Pack + manifest validation thành công |

---

## 2. QA-12: Menu chuột phải & quy mô tệp Explorer

| Tiêu chí | Kịch bản | Bằng chứng thực tế | Trạng thái |
| :--- | :--- | :--- | :---: |
| **COM activation** | `CoCreateInstance` CLSID `{6B0F3C2A-...}` qua `LocalServer32` | Tạo được `IExplorerCommand`; `GetTitle` trả “Batch Rename with BFR”; `GetIcon` trả đường dẫn app; `GetState`, `GetFlags`, `GetCanonicalName` đúng. | **ĐẠT (tự động)** |
| **Invoke với lựa chọn thật** | Gọi `Invoke` với `IShellItemArray` tạo từ `SHCreateShellItemArrayFromShellItem` | `S_OK`; file-list được tạo trong `%TEMP%\BatchFileRenamer\shell`; app self-contained được mở; app đọc và xóa file-list. | **ĐẠT (1 tệp)** |
| **Mở 1 tệp từ Explorer** | Chuột phải → chọn 1 tệp | Chưa chạy trực tiếp trong Explorer tương tác; luồng COM tương đương đã được kiểm chứng. | **CHỜ THỦ CÔNG** |
| **Mở 50 tệp** | Chọn 50 tệp trong Explorer | Chưa chạy trực tiếp; `MultiSelectModel=Player` đã cấu hình và IPC/collector có unit test. | **CHỜ THỦ CÔNG** |
| **Mở 5.000 tệp** | Ghi file-list và gọi `--file-list` | Unit test `LargeBatch_5000Files_QA12_PassesWithoutTruncation` pass; không phụ thuộc giới hạn 32.767 ký tự. | **ĐẠT (tự động)** |
| **Single instance / IPC** | Mở instance thứ hai khi app đang chạy | Unit test mutex, pipe, debounce và payload pass; secondary gửi dữ liệu rồi thoát. | **ĐẠT (tự động)** |
| **Dọn file-list** | Sau khi app đọc danh sách | `FileListReader.ReadAndDelete` xóa tệp; kiểm chứng thực tế sau `Invoke` thư mục tạm trống. | **ĐẠT** |

**Ghi chú kỹ thuật:** COM server chạy out-of-process (`BFR.Shell.exe`) để giữ được publish self-contained. `IExplorerCommand` đã được sửa đúng thứ tự vtable (`GetState`, `Invoke`, `GetFlags`, `EnumSubCommands`); lỗi thứ tự cũ sẽ khiến Explorer gọi nhầm phương thức.

---

## 3. QA-16: Đóng gói MSIX & tích hợp hệ điều hành

| Tiêu chí | Kết quả | Trạng thái |
| :--- | :--- | :---: |
| **Full Trust** | `<rescap:Capability Name="runFullTrust" />` có trong manifest. | **ĐẠT** |
| **COM server trong gói** | `com:ExeServer Executable="BFR.Shell.exe" Arguments="-Embedding"`; `com:Class` đúng CLSID. | **ĐẠT** |
| **Menu Windows 10 (classic)** | Bản portable ghi HKCU `ExplorerCommandHandler` cho `*`, `Directory`, `Directory\Background`; `MultiSelectModel=Player`. Bản MSIX dùng `desktop5` cho tệp/thư mục. | **ĐẠT (portable) / CHỜ CÀI MSIX** |
| **Menu Windows 11 (modern)** | `desktop4:FileExplorerContextMenus` + `desktop5` cho `*`/`Directory`; `desktop10` cho nền thư mục (22H2+). | **ĐẠT (manifest)** |
| **Menu “Show more options”** | Không khai báo `desktop9` vì yêu cầu `com:SurrogateServer` in-proc, xung đột với server out-of-process self-contained. | **GIỚI HẠN ĐÃ BIẾT** |
| **AppExecutionAlias** | `uap3:AppExecutionAlias` + `desktop:ExecutionAlias` cho `bfr.exe`. | **ĐẠT (manifest)** |
| **Assets** | Đủ 5 PNG kích thước chuẩn trong `packaging/msix/Assets/`. | **ĐẠT** |
| **Đóng gói tự động** | `build-msix.ps1` publish self-contained, kiểm tra file bắt buộc, tạo MSIX + ZIP. | **ĐẠT** |
| **Ký gói** | Hỗ trợ `BFR_PFX_PATH`/`BFR_PFX_PASSWORD`; chưa có chứng chỉ thật. | **CHỜ** |
| **Cài và chạy MSIX** | Chưa cài do gói chưa ký. | **CHỜ** |

---

## 4. QA-17: Trợ năng & trình đọc màn hình

| Tiêu chí | Kết quả kiểm tra mã nguồn | Trạng thái |
| :--- | :--- | :---: |
| **AutomationProperties** | Các điều khiển chính có `AutomationProperties.Name`; `StatusText` có `LiveSetting=Polite`. | **ĐẠT (code review)** |
| **Keyboard navigation** | Tab/Shift+Tab, access keys, `Ctrl+Z/O/S`, `Ctrl+Enter`, `F5`, `Esc`; dialog có `IsCancel`. | **ĐẠT (code review)** |
| **High Contrast** | `ApplyButton` và viền/chữ dùng `SystemColors` khi `SystemParameters.HighContrast`. | **ĐẠT (code review)** |
| **Narrator thật** | Chưa chạy Narrator trên Windows 10/11. | **CHỜ THỦ CÔNG** |
| **High Contrast thật** | Chưa bật chế độ tương phản cao để kiểm tra trực quan. | **CHỜ THỦ CÔNG** |

---

## 5. Lỗi đã phát hiện và khắc phục trong quá trình QA

1. **Sai thứ tự vtable `IExplorerCommand`:** `GetFlags` từng đứng trước `Invoke`, khiến Explorer gọi sai phương thức. Đã sửa cả interop, server và smoke test.
2. **`comhost` self-contained lỗi `0x80008093`:** chuyển sang COM local server out-of-process (`BFR.Shell.exe`), dùng CCW managed và `LocalServer32`; MSIX dùng `com:ExeServer`.
3. **Manifest `com:Class ProgId` không có `com:ProgId` tương ứng:** MakeAppx báo lỗi keyref; đã bỏ `ProgId` khỏi `com:Class` (ProgID vẫn được đăng ký ở bản portable qua HKCU).
4. **`desktop9` classic handler không tương thích `ExeServer`:** MakeAppx yêu cầu `SurrogateServer`; đã bỏ extension này và ghi nhận giới hạn.
5. **Thiếu copy runtimeconfig/deps của COM host:** không còn cần thiết sau khi chuyển sang exe server; đã bỏ target copy trong `BFR.App.csproj`.

---

## 6. Checklist nộp Microsoft Store

- [x] `asInvoker`, không yêu cầu Administrator.
- [x] Cửa sổ chính mở trực tiếp, không chạy nền/khay hệ thống.
- [x] `longPathAware=true`.
- [x] Notes for Certification giải thích `runFullTrust`.
- [x] Ảnh screenshot dùng dữ liệu mẫu, 1600×900.
- [x] CI build/test/package và COM smoke test.
- [ ] Chứng chỉ ký tin cậy + Publisher ID khớp manifest.
- [ ] Tài khoản Partner Center và submission.
- [ ] URL privacy policy công khai (GitHub Pages).
- [ ] Cài MSIX đã ký trên Windows 10 và Windows 11 sạch.
- [ ] Kiểm thử Explorer thật với 1/50/5.000 tệp.
- [ ] Narrator và High Contrast thật.
- [ ] Tùy chọn: thêm ảnh/video Store.

---

## 7. Tổng kết

- **Phần kỹ thuật Giai đoạn 2:** hoàn tất, 96/96 test pass, build sạch, COM self-contained hoạt động.
- **Cổng QA-12/16/17:** đạt ở mức tự động/code review; các mục thủ công và chứng chỉ là điều kiện bên ngoài chưa thực hiện.
- **Không nên gửi Store ở trạng thái hiện tại** vì gói chưa ký và chưa có URL privacy policy công khai.
