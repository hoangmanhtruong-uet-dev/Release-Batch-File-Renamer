# CHÍNH SÁCH QUYỀN RIÊNG TƯ / PRIVACY POLICY

**Ngày có hiệu lực / Effective Date:** 20/10/2026  
**Ứng dụng / Application:** Batch File Renamer (BFR)  
**Nhà phát triển / Developer:** Batch File Renamer Team  

> Bản HTML công khai: `docs/privacy.html`. URL công khai (GitHub Pages) sẽ được cập nhật trong Store listing trước khi nộp duyệt.

---

## Tiếng Việt (Vietnamese)

### 1. Cam kết cốt lõi: Hoàn toàn Cục bộ (100% Offline & Local)
Batch File Renamer ("BFR") là công cụ tiện ích dành cho Windows được thiết kế với nguyên tắc **Bảo vệ quyền riêng tư tối đa (Privacy by Design)**:
- Ứng dụng **không thu thập, không truyền tải, không lưu trữ** bất kỳ thông tin cá nhân, tên tệp, đường dẫn, nội dung tệp hay siêu dữ liệu (metadata) của người dùng ra bên ngoài thiết bị.
- Mọi thao tác quét tệp, trích xuất dữ liệu ngày tháng/EXIF, tính toán quy tắc đổi tên, ghi nhật ký phục hồi (journal) và thực thi đổi tên đều diễn ra **hoàn toàn trên máy tính cá nhân của bạn**.
- Ứng dụng **không có máy chủ từ xa**, không có dịch vụ theo dõi hành vi và không bán dữ liệu cho bất kỳ bên thứ ba nào.

### 2. Thu thập và Xử lý Dữ liệu Hệ thống Tệp
- **Danh sách tệp và đường dẫn:** Chỉ được nạp vào bộ nhớ tạm của ứng dụng khi bạn chủ động chọn tệp/thư mục, kéo thả vào giao diện hoặc mở qua menu ngữ cảnh Windows Explorer.
- **Siêu dữ liệu EXIF / Media:** Ứng dụng chỉ đọc các trường ngày giờ (như `DateTimeOriginal`, `DateTimeDigitized`) từ ảnh hoặc tài liệu do bạn chỉ định khi bạn áp dụng quy tắc đổi tên theo ngày tháng (`DateRule` hoặc token `{taken}`). Dữ liệu này chỉ dùng duy nhất cho mục đích tạo tên tệp mới và không bao giờ được gửi đi bất cứ đâu.
- **Nhật ký phục hồi (Journal):** Lưu cục bộ tại `%LocalAppData%\BatchFileRenamer\journal` để phục vụ tính năng Hoàn tác (Undo) và Khôi phục sau sự cố mất điện/tắt máy đột ngột.
- **Danh sách tệp tạm từ Explorer:** Khi chọn nhiều tệp, COM server cục bộ ghi danh sách vào `%TEMP%\BatchFileRenamer\shell`; ứng dụng đọc và xóa tệp này ngay sau khi xử lý. Tệp không được gửi qua mạng.
- **Không có telemetry/analytics:** Ứng dụng không gửi số liệu sử dụng, lỗi hoặc thông tin thiết bị tới máy chủ nào.

### 3. Giao tiếp giữa các tiến trình (IPC)
- Ứng dụng sử dụng Windows Named Pipe (`BatchFileRenamer_IPC_Pipe`) và Mutex cục bộ chỉ để chuyển tiếp danh sách đường dẫn khi bạn mở thêm tệp từ Explorer vào cửa sổ ứng dụng đang mở.
- Kênh giao tiếp này hoàn toàn nội bộ trên máy cục bộ, giới hạn trong phiên đăng nhập của người dùng hiện tại và không thể truy cập từ mạng ngoài.

### 4. Quyền thực thi (Permissions)
- Ứng dụng sử dụng năng lực `runFullTrust` trong gói MSIX (Desktop Bridge) vì đây là yêu cầu kỹ thuật bắt buộc của Windows để một phần mềm tiện ích có thể đổi tên tệp tin trên đĩa và tích hợp menu ngữ cảnh với Windows Explorer.
- Ứng dụng chạy ở mức quyền người dùng thông thường (`asInvoker`), **hoàn toàn không yêu cầu quyền Quản trị viên (Administrator)**.

### 5. Mua trong ứng dụng (Microsoft Store IAP)
- Khi người dùng kiểm tra hoặc mua gói **Pro (mua đứt trọn đời)**, ứng dụng dùng API `Windows.Services.Store` của hệ điều hành để **kiểm tra giấy phép và thực hiện giao dịch mua trực tiếp với Microsoft Store**. Đây là kênh có kết nối mạng duy nhất của ứng dụng.
- Ứng dụng **không gửi** tên tệp, đường dẫn, nội dung tệp, siêu dữ liệu hay bất kỳ thông tin nội dung nào lên Store. Chỉ có **mã sản phẩm add-on** và phiên đăng nhập Store do chính hệ điều hành quản lý.
- Thanh toán, thuế và hoàn tiền do Microsoft Store xử lý; ứng dụng không nhận số thẻ hay thông tin thanh toán.
- Nếu máy đang ngoại tuyến, ứng dụng dùng giấy phép đã lưu cục bộ và giữ nguyên cấp hiện tại; không có dữ liệu nào được gửi đi.
- Bản phân phối dạng ZIP (portable) **không** dùng Store IAP và luôn ở cấp Free.

### 6. Kiểm tra cập nhật (bản portable, tuỳ chọn)
- Khi người dùng **bật** chức năng “Kiểm tra cập nhật” (mặc định **TẮT**) hoặc bấm nút kiểm tra, ứng dụng gọi **một URL công khai do người dùng cấu hình** (mặc định là API “latest release” của GitHub) để đọc số phiên bản mới nhất.
- Ứng dụng **không gửi** tên tệp, đường dẫn, nội dung tệp hay thông tin cá nhân; chỉ gửi một yêu cầu HTTP thông thường kèm User-Agent để máy chủ nhận diện ứng dụng.
- Nếu không bật hoặc máy ngoại tuyến, ứng dụng **không** thực hiện kết nối này. Bản cài từ Microsoft Store được Store cập nhật tự động nên không dùng chức năng này.

### 7. Liên hệ
Nếu bạn có bất kỳ câu hỏi nào về chính sách quyền riêng tư này, vui lòng mở issue trên kho lưu trữ mã nguồn của dự án trên GitHub.

---

## English

### 1. Core Commitment: 100% Local & Offline
Batch File Renamer ("BFR") is a Windows utility built with **Privacy by Design**:
- The application **does not collect, transmit, or store** any personal data, file names, file paths, file contents, or metadata outside of your device.
- All file scanning, date/EXIF metadata extraction, rule preview computation, rollback journals, and renaming executions take place **entirely on your local machine**.
- The app operates **without remote servers**, does not include third-party tracking or analytics SDKs, and never sells data.

### 2. File System Data Access
- **Files and Paths:** Only accessed in memory when you explicitly select files/folders, drag and drop them into the app window, or invoke the app via Windows File Explorer context menu.
- **EXIF / Media Metadata:** Read locally only when you use date-based renaming rules (such as `DateRule` or `{taken}` token). Metadata is used solely to construct the target file names and never leaves your computer.
- **Rollback Journals:** Stored locally in `%LocalAppData%\BatchFileRenamer\journal` exclusively to support Undo and crash recovery.
- **Temporary Explorer file lists:** When many files are selected, the local COM server writes the list to `%TEMP%\BatchFileRenamer\shell`; the app reads and deletes it immediately after processing. The file is never transmitted over a network.
- **No telemetry/analytics:** The app does not send usage data, error reports, or device information to any server.

### 3. Inter-Process Communication (IPC)
- Local Named Pipes and Mutexes are used only to pass file paths to the running instance when multiple files are opened from File Explorer.
- Communication is restricted to the local user session.

### 4. Permissions
- The app utilizes the `runFullTrust` capability in its MSIX package as required by Windows Desktop Bridge applications to perform file system rename operations and register File Explorer context menus.
- The app runs strictly as a standard user process (`asInvoker`) and **never requests Administrator privileges**.

### 5. In-app purchase (Microsoft Store IAP)
- When you check or buy the **Pro (lifetime) add-on**, the app uses the operating system's `Windows.Services.Store` API to **verify the license and complete the purchase directly with the Microsoft Store**. This is the only network connection the app makes.
- The app **never sends** file names, paths, file contents, metadata or any content data to the Store. Only the **add-on product ID** and the Store session managed by the operating system are involved.
- Payment, taxes and refunds are handled by the Microsoft Store; the app never sees your card details.
- When offline, the app uses the locally cached license and keeps your current tier; nothing is transmitted.
- The portable ZIP distribution **does not** use Store IAP and always stays on the Free tier.

### 6. Update check (portable build, opt-in)
- When you **enable** “Check for updates” (off by default) or press the check button, the app calls **a public URL configured by you** (by default the GitHub “latest release” API) to read the newest version number.
- The app **never sends** file names, paths, file contents or personal information; it only makes a normal HTTP request with a User-Agent so the server can identify the app.
- If the option is off or the machine is offline, the app makes **no** such connection. The Microsoft Store build updates automatically and does not use this feature.

### 7. Contact
For questions regarding this privacy policy, please open an issue on the official GitHub repository.
