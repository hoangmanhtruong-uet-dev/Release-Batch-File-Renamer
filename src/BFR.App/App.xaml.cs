namespace BFR.App;

using BFR.Core;
using BFR.Core.Licensing;
using BFR.Store;
using BFR.App.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

public partial class App : Application
{
    private SingleInstanceLock? _lock;
    private SingleInstanceServer? _server;
    private MainWindow? _mainWindow;
    private LicenseCoordinator? _license;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var payload = ParseArgs(e.Args);

        // Hỗ trợ lệnh đăng ký nhanh menu chuột phải từ phím tắt hoặc cài đặt
        if (payload.Paths.Contains("--register-context-menu", StringComparer.OrdinalIgnoreCase))
        {
            if (ShellIntegration.IsSupported)
            {
                var exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "BatchFileRenamer.exe");
                ShellIntegration.Register(
                    exePath,
                    comServerPath: ShellIntegration.ResolveComServerPath(exePath));
            }
            Shutdown(0);
            return;
        }

        if (payload.Paths.Contains("--unregister-context-menu", StringComparer.OrdinalIgnoreCase))
        {
            if (ShellIntegration.IsSupported)
            {
                ShellIntegration.Unregister();
            }
            Shutdown(0);
            return;
        }

        _license = CreateLicenseCoordinator();

        // Khôi phục giao diện (sáng/tối) và ngôn ngữ đã lưu trước khi tạo cửa sổ.
        var settings = SettingsStore.Load();
        var theme = Enum.TryParse<ThemeManager.AppTheme>(settings.Theme, out var parsedTheme)
            ? parsedTheme
            : ThemeManager.AppTheme.System;
        ThemeManager.Apply(theme);

        if (!string.IsNullOrWhiteSpace(settings.Language) && settings.Language != "vi")
        {
            Loc.Apply(settings.Language);
        }

        _lock = new SingleInstanceLock();
        if (!_lock.IsPrimary)
        {
            // Đã có phiên ứng dụng chạy trước đó. Gửi danh sách tệp qua Named Pipe rồi thoát.
            await SingleInstanceClient.TrySendAsync(payload, timeoutMs: 3000).ConfigureAwait(false);
            Dispatcher.Invoke(() => Shutdown(0));
            return;
        }

        _mainWindow = new MainWindow(payload, _license);
        MainWindow = _mainWindow;

        // Bắt đầu lắng nghe trước khi Show để không bỏ lỡ lần mở tệp thứ hai
        // xảy ra ngay khi cửa sổ đang khởi tạo.
        _server = new SingleInstanceServer(OnIpcPayloadReceived);
        _server.Start();
        _mainWindow.Show();

        // Đồng bộ giấy phép Store (chạy nền, không chặn khởi động).
        _ = RefreshLicenseAsync();
    }

    /// <summary>
    /// Tạo cơ chế cấp giấy phép. Bản đóng gói MSIX dùng Microsoft Store IAP;
    /// bản portable chỉ dùng được tính năng Free (Store IAP không hoạt động ngoài Store).
    /// </summary>
    private static LicenseCoordinator CreateLicenseCoordinator()
    {
        var fs = new RealFileSystem();
        var packaged = ShellIntegration.IsPackaged;
        var devTier = LicenseService.ResolveDevTierOverride(packaged);

        var license = new LicenseService(
            new LicenseStore(fs),
            tierOverride: devTier,
            trialEnabled: true);

        var provider = new MicrosoftStoreLicenseProvider();
        return new LicenseCoordinator(license, provider);
    }

    private async Task RefreshLicenseAsync()
    {
        if (_license is null) return;

        try
        {
            await _license.RefreshAsync().ConfigureAwait(true);
        }
        catch
        {
            // Lỗi mạng/Store không được chặn ứng dụng: giữ cấp Free.
        }

        _mainWindow?.UpdateLicenseState();
    }

    private void OnIpcPayloadReceived(IpcPayload payload)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (_mainWindow == null) return;

            var paths = new List<string>(payload.Paths);
            if (!string.IsNullOrWhiteSpace(payload.FileListPath))
            {
                paths.AddRange(FileListReader.ReadAndDelete(payload.FileListPath));
            }

            if (!string.IsNullOrWhiteSpace(payload.PresetName))
            {
                _mainWindow.ApplyPresetByName(payload.PresetName);
            }

            if (paths.Count > 0)
            {
                _mainWindow.EnqueuePaths(paths);
            }
            else
            {
                _mainWindow.ActivateAndForeground();
            }
        });
    }

    private static IpcPayload ParseArgs(string[] args)
    {
        var payload = new IpcPayload();
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (string.Equals(a, "--file-list", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    payload.FileListPath = args[++i];
                }
            }
            else if (string.Equals(a, "--preset", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    payload.PresetName = args[++i];
                }
            }
            else
            {
                payload.Paths.Add(a);
            }
        }
        return payload;
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_server != null)
        {
            try
            {
                await _server.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        }

        _lock?.Dispose();
        base.OnExit(e);
    }
}
