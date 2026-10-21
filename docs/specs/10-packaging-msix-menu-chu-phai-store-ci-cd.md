# 10. ĐÓNG GÓI MSIX, TÍCH HỢP MENU CHUỘT PHẢI, CHỨNG NHẬN STORE VÀ CI/CD

Nguồn: `docs/batch_file_renamer_spec_v1.0.md` — Phần 11.

---

## 10.1 Đóng gói

- Gói MSIX (Desktop Bridge, `runFullTrust`) cho ứng dụng chính; CLI (`bfr.exe`) khai báo `AppExecutionAlias` để chạy từ Terminal.
- Khai báo `longPathAware` trong application manifest.
- Tùy chọn bản portable (ZIP) trên GitHub Releases: chỉ có tính năng Free (Store IAP không hoạt động ngoài Store).

---

## 10.2 Tích hợp menu chuột phải

Hai tầng:

1. **Windows 11 (menu mới):** gói MSIX khai báo tiện ích mở rộng menu ngữ cảnh Explorer, trỏ tới một **COM server cài đặt `IExplorerCommand`**; với nhiều tệp được chọn, `Invoke` nhận một `IShellItemArray` chứa toàn bộ lựa chọn trong một lần gọi. Phiên bản manifest và tên phần tử XML chính xác cần **kiểm tra theo tài liệu hiện hành của Microsoft** khi triển khai (không ghi cố định ở đây).
2. **Dự phòng/Windows 10 (menu cũ):** khóa registry theo người dùng (`HKCU\Software\Classes\*\shell\BFR`, `...\Directory\shell\BFR`) với `MultiSelectModel` phù hợp. Ở gói MSIX không ghi registry toàn cục; dùng cơ chế khai báo của gói hoặc tạo khi chạy lần đầu theo cách được phép.

---

## 10.3 Nhận danh sách tệp từ Explorer (quan trọng)

- Không truyền toàn bộ đường dẫn qua dòng lệnh (giới hạn khoảng 32.767 ký tự). Quy trình: `IExplorerCommand`/shell verb ghi danh sách vào **tệp tạm** (hoặc gửi qua **named pipe** tới instance đang chạy) rồi khởi chạy ứng dụng với tham số `--file-list "<đường dẫn tệp tạm>"`.
- **Single instance + người thu thập:** dùng Mutex; instance đầu tiên mở cửa sổ và nhận thêm đường dẫn qua pipe trong khoảng thời gian gom (300–500 ms) rồi mới dựng danh sách.

---

## 10.4 Checklist chứng nhận Store

1. `requestedExecutionLevel = asInvoker`; không đòi quyền Administrator.
2. Điền "Notes for Certification" giải thích `runFullTrust`: ứng dụng cần truy cập hệ thống tệp và tích hợp menu ngữ cảnh Explorer để đổi tên tệp do người dùng chọn.
3. Khởi chạy từ nút "Open" phải hiện cửa sổ chính (ứng dụng không chỉ nằm ở khay hệ thống).
4. Có Chính sách quyền riêng tư và mô tả trung thực; ảnh chụp màn hình không chứa tên tệp/dữ liệu cá nhân thật.
5. Kiểm tra trademark tên "Batch File Renamer" (tên mô tả chung dễ trùng), đặt trước tên trên Partner Center sớm; cân nhắc tên thương hiệu riêng kèm phụ đề mô tả.
6. Chuẩn bị tài khoản nhà phát triển, kiểm tra phí đăng ký, thuế và thanh toán hiện hành.

---

## 10.5 CI/CD

- GitHub Actions: build, unit test, property test, benchmark (so sánh với ngưỡng), đóng gói MSIX và ký thử, quét giấy phép thư viện bên thứ ba.
- Chạy test hệ thống tệp trên **ổ tạm** (thư mục tạm duy nhất mỗi lần chạy) và bản giả `IFileSystem` cho kịch bản lỗi/mất điện.
- Chạy trên ít nhất Windows Server (runner) cho test logic; giao diện/menu chuột phải kiểm thử thủ công trên máy thật Windows 10 và 11.
