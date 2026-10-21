# GIAI ĐOẠN 2 — PHÁT HÀNH FREE LÊN STORE

Nguồn: `docs/specs/13-lo-trinh-mvp-ru-ro.md` — Giai đoạn 2 (2 tuần).  
Phạm vi: Đóng gói MSIX, menu chuột phải (cũ và mới), Application Manifest, Single Instance IPC, Privacy Policy, Store Listing & Notes for Certification, CI/CD GitHub Actions, nghiệm thu QA-12, QA-16, QA-17.

> **Trạng thái:** phần kỹ thuật của Giai đoạn 2 đã hoàn tất và đã được kiểm chứng tự động.  
> Gói MSIX hiện **chưa ký** và **chưa thể nộp Store** cho đến khi có chứng chỉ tin cậy, tài khoản Partner Center và URL privacy policy công khai. Các mục kiểm thử thủ công trên Explorer Windows 10/11 vẫn cần chạy trên máy đích trước khi phát hành.

---

## 1. Các thành phần đã triển khai

### Single Instance, IPC & Nạp tệp quy mô lớn (QA-12, Spec 10.3)
- `SingleInstanceLock` (`BFR.Core/SingleInstanceIpc.cs`): dùng `Mutex` phiên người dùng để đảm bảo chỉ có một instance chính mở giao diện. Trường hợp mutex bị bỏ rơi (`AbandonedMutexException`) được xử lý an toàn.
- `SingleInstanceServer` & `SingleInstanceClient`: giao tiếp qua Windows Named Pipe không đồng bộ (`BatchFileRenamer_IPC_Pipe`). Instance thứ hai gửi `IpcPayload` tới instance đầu rồi thoát ngay; instance đầu nhận tệp, gom lô và đưa cửa sổ lên foreground.
- **Bộ gom đường dẫn (Collector Debounce 350 ms):** gom các lượt gọi liên tiếp từ Explorer/CLI vào một danh sách trước khi tính Preview, tránh mở nhiều cửa sổ hoặc tính toán lặp.
- `FileListReader` (`BFR.Core/FileListReader.cs`): đọc danh sách từ tệp tạm qua `--file-list "<đường-dẫn>"`, vượt giới hạn 32.767 ký tự của dòng lệnh Windows. Ứng dụng đọc xong sẽ xóa tệp tạm.
- CLI `bfr rename` cũng hỗ trợ `--file-list <list.txt>`.

### Tích hợp Menu ngữ cảnh Explorer (QA-12, Spec 10.2)
- **COM local server out-of-process** trong `src/BFR.Shell`:
  - `BFR.Shell.exe` là một COM exe server tự đăng ký class object (`IClassFactory`) và expose `IExplorerCommand` qua CCW.
  - Server chạy **self-contained**, không phụ thuộc .NET 8 runtime cài sẵn trên máy người dùng.
  - Tự thoát sau 60 giây không có lời gọi; Explorer sẽ khởi động lại khi cần.
  - Đây là giải pháp thay cho `comhost` in-proc, vì `comhost` không hỗ trợ publish self-contained (lỗi `0x80008093`).
- **CLSID / ProgID:** `6B0F3C2A-7E4D-4A8B-9C1D-2F5E8A7B6C4D` / `BFR.ExplorerCommand`.
- **Bản portable:** `bfr context-menu register` ghi `HKCU\Software\Classes\CLSID\{...}\LocalServer32` trỏ tới `BFR.Shell.exe -Embedding`, cùng các khóa `ExplorerCommandHandler` cho `*`, `Directory`, `Directory\Background`.
- **Bản MSIX:** `AppxManifest.xml` khai báo `com:ExeServer` và extension `desktop4:FileExplorerContextMenus`:
  - `desktop5:ItemType Type="*"` và `Type="Directory"` cho Windows 10 1809+ / Windows 11.
  - `desktop10:ItemType Type="Directory\Background"` cho Windows 11 22H2+.
- **Multi-select:** COM dùng `MultiSelectModel="Player"`; fallback command-line dùng `MultiSelectModel="Document"` để bộ gom single-instance xử lý nhiều lần gọi.
- **GUI:** nút **Menu chuột phải** trên thanh công cụ cho phép bật/tắt ở bản portable; bản MSIX hiển thị thông báo do gói quản lý.
- **CLI:**
  - `bfr context-menu status`
  - `bfr context-menu register [--path <exe>] [--comserver <BFR.Shell.exe>] [--name <tên>]`
  - `bfr context-menu unregister`
  - `bfr context-menu export-reg <output.reg>`

### Application Manifest & Store Compliance (Spec 10.1, 10.4)
- `app.manifest` cho `BFR.App`, `BFR.Cli` và `BFR.Shell`:
  - `requestedExecutionLevel = asInvoker`, `uiAccess=false` — không yêu cầu Administrator.
  - `longPathAware=true` — hỗ trợ đường dẫn dài (QA-13).
  - `dpiAwareness=PerMonitorV2` — sắc nét trên màn hình DPI cao.
  - Khai báo tương thích Windows 10/11.

### Trợ năng (Accessibility - QA-17)
- `AutomationProperties.Name` cho các cửa sổ và điều khiển chính; `StatusText` có `LiveSetting="Polite"` để Narrator đọc trạng thái.
- Access keys và phím tắt: `Ctrl+Z`, `Ctrl+O`, `Ctrl+S`, `Ctrl+Enter`, `F5`, `Esc`.
- High Contrast dùng `SystemColors` cho nút Áp dụng và viền/chữ quan trọng.
- `HistoryWindow` có nút Đóng/Close với `IsCancel`.
- **Lưu ý:** kiểm thử bằng Narrator/High Contrast thật vẫn là mục thủ công trước khi nộp Store.

### Đóng gói MSIX & Portable ZIP (QA-16, Spec 10.1)
- `packaging/msix/AppxManifest.xml`:
  - `runFullTrust` cho desktop app.
  - `com:ExeServer` trỏ `BFR.Shell.exe`.
  - `desktop4:FileExplorerContextMenus` với `desktop5`/`desktop10`.
  - `uap3:AppExecutionAlias` + `desktop:ExecutionAlias` cho `bfr.exe`.
  - Đủ assets: StoreLogo, Square44x44, Square150x150, Wide310x150, SplashScreen.
- `packaging/msix/build-msix.ps1`:
  - Publish **self-contained** cho `BFR.App`, `BFR.Shell` và `BFR.Cli` (win-x64).
  - Kiểm tra file bắt buộc trước khi đóng gói; loại PDB khỏi layout.
  - Tạo `BatchFileRenamer-1.0.0.msix` bằng MakeAppx (Windows SDK hoặc NuGet BuildTools).
  - Tạo `BatchFileRenamer-1.0.0-portable-win-x64.zip`.
  - Ký tùy chọn qua `BFR_PFX_PATH` / `BFR_PFX_PASSWORD`.
- Kích thước gói hiện tại: **~71,7 MB** (self-contained, 480 tệp trong layout).

### Tài liệu Kiểm duyệt & Quyền riêng tư (Spec 10.4)
- `PRIVACY_POLICY.md`: chính sách song ngữ, cam kết 100% xử lý cục bộ.
- `docs/privacy.html`: bản HTML sẵn sàng host trên GitHub Pages; **URL công khai vẫn đang chờ**.
- `docs/STORE_LISTING.md`: mô tả Store song ngữ, từ khóa, Notes for Certification, checklist nộp duyệt (một số mục đánh dấu chờ).
- `docs/store-assets/screenshot-main.png`: ảnh 1600×900, dùng dữ liệu mẫu.

### CI/CD Tự động hóa (Spec 10.5)
- `.github/workflows/ci.yml` chạy trên `windows-latest`:
  - Restore, build Release, unit test, property test.
  - Đóng gói MSIX + Portable ZIP.
  - **COM local server smoke test:** đăng ký `BFR.Shell.exe` từ layout self-contained, gọi `GetTitle`/`GetIcon`/`GetState`/`GetFlags`/`GetCanonicalName`, rồi dọn registry.
  - Upload artifact (kể cả khi bước trước lỗi).

---

## 2. Kết quả kiểm thử tự động

- **BFR.Core.Tests:** **89 test** — pass 89/89.
- **BFR.Core.PropertyTests:** **7 test** — pass 7/7.
- **Tổng:** **96/96 test pass**, 0 fail.
- **Build:** Release, **0 warning / 0 error**.
- **COM smoke test:** pass trên layout self-contained (title, icon, state, flags, canonical name).
- **Kiểm chứng bổ sung (thủ công, ngoài bộ test):**
  - `Invoke` với `IShellItemArray` thật chứa 1 tệp: trả `S_OK`, tạo file-list, mở app từ layout self-contained, app đọc và xóa file-list.
  - `GetTitle`, `GetIcon`, `GetState`, `GetFlags`, `GetCanonicalName` qua COM out-of-proc trả đúng.
  - MakeAppx tạo và validate package thành công.

---

## 3. Lệnh kiểm tra và đóng gói

```powershell
# Chạy toàn bộ test
dotnet test BatchFileRenamer.sln -c Release

# Build Release (0 warning mục tiêu)
dotnet build BatchFileRenamer.sln -c Release

# Đóng gói MSIX + Portable ZIP
powershell -ExecutionPolicy Bypass -File packaging/msix/build-msix.ps1 -Configuration Release -SkipSign

# Kiểm tra trạng thái menu qua CLI (bản portable)
dotnet run --project src/BFR.Cli -c Release -- context-menu status

# COM smoke test trên layout đã publish
$layout = (Resolve-Path artifacts/msix-layout).Path
& "$layout/bfr.exe" context-menu register --path "$layout/BatchFileRenamer.exe"
dotnet run --project tests/BFR.Shell.SmokeTest -c Release --no-build
& "$layout/bfr.exe" context-menu unregister
```

---

## 4. Việc còn lại trước khi nộp Store

| Hạng mục | Trạng thái | Ghi chú |
| :--- | :--- | :--- |
| Chứng chỉ ký MSIX | **CHỜ** | Cần PFX/EV certificate và Publisher ID khớp `Identity Publisher`. |
| Tài khoản Partner Center | **CHỜ** | Chưa có để tạo submission, điền Store listing và tải gói. |
| URL privacy policy công khai | **CHỜ** | `docs/privacy.html` đã sẵn sàng; cần host GitHub Pages và thay URL thật. |
| Kiểm thử cài MSIX thật | **CHỜ** | Cần cài trên Windows 10 và Windows 11 sạch. |
| Kiểm thử menu Explorer thật | **CHỜ** | Cần chọn 1/50/5.000 tệp trực tiếp trong Explorer. |
| Narrator / High Contrast thật | **CHỜ** | Kiểm tra thủ công trên Windows 10/11. |
| Ảnh/video Store bổ sung | **TÙY CHỌN** | Hiện có 1 ảnh 1600×900; Store chấp nhận tối thiểu 1 ảnh. |

### Giới hạn kỹ thuật đã biết
- `desktop9:FileExplorerClassicContextMenuHandler` (menu “Show more options” của Windows 11) yêu cầu COM class trong `com:SurrogateServer` (in-proc). Server hiện tại là **out-of-process** để chạy self-contained, nên extension này không được khai báo. Menu hiện đại vẫn hoạt động; bản portable đăng ký menu cổ điển qua HKCU.
- Nếu cần menu cổ điển trong gói MSIX trên Windows 11, phải bổ sung native COM shim hoặc chuyển sang mô hình in-proc có runtime kèm theo; đây là thay đổi kiến trúc, không nằm trong bản hiện tại.

---

## 5. Giai đoạn tiếp theo

Giai đoạn 3 (v1.1 — bật Pro) đã được triển khai: phân tách Free/Pro, Microsoft Store IAP, EXIF/media, undo đa bước, preset không giới hạn, xuất báo cáo và CLI theo cấp giấy phép.  
Chi tiết: `PHASE3_README.md` và `docs/PHASE3_QA.md`. Gói hiện tại đã lên phiên bản **1.1.0** (`artifacts/BatchFileRenamer-1.1.0.msix`).
