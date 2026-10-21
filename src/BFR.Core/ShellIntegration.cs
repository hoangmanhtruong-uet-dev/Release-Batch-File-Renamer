namespace BFR.Core;

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32;

/// <summary>
/// Tích hợp Menu ngữ cảnh Explorer.
///
/// - Windows 11: gói MSIX đăng ký COM local server <c>BFR.Shell.exe</c> và
///   <c>desktop4/desktop10:FileExplorerContextMenus</c> trong AppxManifest.
/// - Windows 10 / menu "Show more options": đăng ký khóa HKCU với
///   <c>ExplorerCommandHandler</c> trỏ tới cùng COM class.
///
/// Server chạy out-of-process để có thể publish self-contained trong MSIX,
/// không phụ thuộc .NET runtime cài sẵn trên máy người dùng.
///
/// Cả hai tầng đều dùng <c>IExplorerCommand</c> để nhận toàn bộ lựa chọn trong
/// một <c>IShellItemArray</c>, sau đó chuyển danh sách qua tệp tạm bằng
/// <c>--file-list</c> (không bị giới hạn 32.767 ký tự của dòng lệnh).
/// </summary>
public static class ShellIntegration
{
    public const string DefaultVerbKey = "BatchFileRenamer";
    public const string DefaultDisplayName = "Đổi tên hàng loạt với BFR";
    public const string DefaultEnglishDisplayName = "Batch Rename with BFR";

    /// <summary>CLSID phải khớp với <c>[Guid]</c> trong BFR.Shell và AppxManifest.xml.</summary>
    public const string ExplorerCommandClsid = "6B0F3C2A-7E4D-4A8B-9C1D-2F5E8A7B6C4D";
    public const string ExplorerCommandProgId = "BFR.ExplorerCommand";
    public const string ComServerFileName = "BFR.Shell.exe";
    public const string ComServerArguments = "-Embedding";

    private const int AppModelErrorNoPackage = 15700;

    private static readonly string[] Targets = new[]
    {
        @"Software\Classes\*\shell",
        @"Software\Classes\Directory\shell",
        @"Software\Classes\Directory\Background\shell"
    };

    public static bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// True khi ứng dụng đang chạy từ gói MSIX. Khi đó COM class đã được đăng ký
    /// trong manifest, không cần ghi HKCU\Software\Classes\CLSID.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static bool IsPackaged
    {
        get
        {
            if (!IsSupported) return false;

            try
            {
                int length = 0;
                var result = GetCurrentPackageFullName(ref length, null);
                return result != AppModelErrorNoPackage;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    public static bool IsRegistered(string verbKey = DefaultVerbKey)
    {
        if (!IsSupported) return false;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\*\shell\{verbKey}");
            if (key == null) return false;

            // COM handler hoặc verb command-line cũ đều được coi là đã đăng ký.
            return key.GetValue("ExplorerCommandHandler") != null
                || key.OpenSubKey("command") != null;
        }
        catch
        {
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    public static bool IsComRegistered()
    {
        if (!IsSupported) return false;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                $@"Software\Classes\CLSID\{{{ExplorerCommandClsid}}}\LocalServer32");
            var path = key?.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(path)) return false;

            // Giá trị có thể có tham số -Embedding; chỉ cần file server tồn tại.
            path = path.Trim();
            if (path.StartsWith('"'))
            {
                var end = path.IndexOf('"', 1);
                if (end > 1) path = path[1..end];
            }
            else
            {
                var space = path.IndexOf(' ');
                if (space > 0) path = path[..space];
            }

            return File.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    public static string ResolveComServerPath(string appExePath)
    {
        var directory = Path.GetDirectoryName(appExePath);
        return string.IsNullOrWhiteSpace(directory)
            ? ComServerFileName
            : Path.Combine(directory, ComServerFileName);
    }

    [SupportedOSPlatform("windows")]
    public static bool Register(
        string appExePath,
        string displayName = DefaultDisplayName,
        string verbKey = DefaultVerbKey,
        string? comServerPath = null)
    {
        if (!IsSupported) return false;
        if (string.IsNullOrWhiteSpace(appExePath))
            throw new ArgumentException("Đường dẫn ứng dụng không được rỗng.", nameof(appExePath));

        try
        {
            comServerPath ??= ResolveComServerPath(appExePath);
            var useCom = !string.IsNullOrWhiteSpace(comServerPath) && File.Exists(comServerPath);

            // Gói MSIX đã đăng ký COM class trong AppxManifest. Bản portable
            // cần ghi HKCU\Software\Classes\CLSID để Explorer tìm thấy local server.
            if (useCom && !IsPackaged)
            {
                useCom = RegisterComClass(comServerPath!, displayName);
            }

            RegisterTarget(@"Software\Classes\*\shell", verbKey, displayName, appExePath,
                "\"%1\"", multiSelect: true, useCom);
            RegisterTarget(@"Software\Classes\Directory\shell", verbKey, displayName, appExePath,
                "\"%1\"", multiSelect: true, useCom);
            RegisterTarget(@"Software\Classes\Directory\Background\shell", verbKey, displayName, appExePath,
                "\"%V\"", multiSelect: false, useCom);

            return true;
        }
        catch
        {
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    public static bool Unregister(string verbKey = DefaultVerbKey)
    {
        if (!IsSupported) return false;

        bool success = true;
        foreach (var target in Targets)
        {
            try
            {
                using var parent = Registry.CurrentUser.OpenSubKey(target, writable: true);
                parent?.DeleteSubKeyTree(verbKey, throwOnMissingSubKey: false);
            }
            catch
            {
                success = false;
            }
        }

        try
        {
            using var classes = Registry.CurrentUser.OpenSubKey(@"Software\Classes", writable: true);
            classes?.DeleteSubKeyTree($@"CLSID\{{{ExplorerCommandClsid}}}", throwOnMissingSubKey: false);
            classes?.DeleteSubKeyTree(ExplorerCommandProgId, throwOnMissingSubKey: false);
        }
        catch
        {
            success = false;
        }

        return success;
    }

    [SupportedOSPlatform("windows")]
    private static bool RegisterComClass(string comServerPath, string displayName)
    {
        using var classes = Registry.CurrentUser.CreateSubKey(@"Software\Classes", writable: true);
        if (classes == null) return false;

        using var clsid = classes.CreateSubKey($@"CLSID\{{{ExplorerCommandClsid}}}", writable: true);
        if (clsid == null) return false;
        clsid.SetValue(null, displayName);

        // LocalServer32: COM sẽ khởi chạy BFR.Shell.exe (self-contained) và
        // gọi CoRegisterClassObject. Không dùng InprocServer32/comhost vì
        // comhost không hỗ trợ publish self-contained.
        using var localServer = clsid.CreateSubKey("LocalServer32", writable: true);
        if (localServer == null) return false;
        localServer.SetValue(null, $"\"{comServerPath}\" {ComServerArguments}");

        using var progIdKey = clsid.CreateSubKey("ProgID", writable: true);
        progIdKey?.SetValue(null, ExplorerCommandProgId);

        using var progId = classes.CreateSubKey(ExplorerCommandProgId, writable: true);
        if (progId != null)
        {
            progId.SetValue(null, "Batch File Renamer Explorer Command");
            using var progIdClsid = progId.CreateSubKey("CLSID", writable: true);
            progIdClsid?.SetValue(null, $"{{{ExplorerCommandClsid}}}");
        }

        return IsComRegistered();
    }

    [SupportedOSPlatform("windows")]
    private static void RegisterTarget(
        string parentPath,
        string verbKey,
        string displayName,
        string appExePath,
        string argTemplate,
        bool multiSelect,
        bool useCom)
    {
        using var parent = Registry.CurrentUser.CreateSubKey(parentPath, writable: true);
        if (parent == null) return;

        using var verb = parent.CreateSubKey(verbKey, writable: true);
        if (verb == null) return;

        verb.SetValue(null, displayName);
        verb.SetValue("Icon", $"\"{appExePath}\"");

        // Xóa khóa command cũ để Windows không chọn nhầm verb command-line
        // thay cho ExplorerCommandHandler sau khi nâng cấp.
        try { verb.DeleteSubKeyTree("command", throwOnMissingSubKey: false); } catch { /* ignore */ }

        if (useCom)
        {
            verb.SetValue("ExplorerCommandHandler", $"{{{ExplorerCommandClsid}}}");
            verb.SetValue("MultiSelectModel", "Player");
            verb.DeleteValue("command", throwOnMissingValue: false);
        }
        else
        {
            verb.DeleteValue("ExplorerCommandHandler", throwOnMissingValue: false);

            // Fallback không có COM: "Document" cho phép Explorer gọi lần lượt
            // từng tệp; single-instance + collector sẽ gom chúng lại thành một lô.
            verb.SetValue("MultiSelectModel", multiSelect ? "Document" : "Single");
            using var command = verb.CreateSubKey("command", writable: true);
            command?.SetValue(null, $"\"{appExePath}\" {argTemplate}");
        }
    }

    /// <summary>
    /// Tạo nội dung tệp .reg để người dùng có thể nhập thủ công nếu muốn.
    /// Khi tìm thấy BFR.Shell.exe cạnh exe, tệp sẽ đăng ký COM local server đầy đủ.
    /// </summary>
    public static string GenerateRegContent(
        string appExePath,
        string displayName = DefaultDisplayName,
        string verbKey = DefaultVerbKey,
        string? comServerPath = null)
    {
        comServerPath ??= ResolveComServerPath(appExePath);
        var useCom = !string.IsNullOrWhiteSpace(comServerPath) && File.Exists(comServerPath);

        var escapedApp = EscapeRegValue(appExePath);
        var sb = new StringBuilder();
        sb.AppendLine("Windows Registry Editor Version 5.00");
        sb.AppendLine();
        sb.AppendLine("; Batch File Renamer - Context Menu Registration");

        if (useCom)
        {
            var escapedCom = EscapeRegValue(comServerPath!);
            sb.AppendLine($"[HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{{{ExplorerCommandClsid}}}]");
            sb.AppendLine($"@=\"{EscapeRegValue(displayName)}\"");
            sb.AppendLine();
            sb.AppendLine($"[HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{{{ExplorerCommandClsid}}}\\LocalServer32]");
            sb.AppendLine($"@=\"\\\"{escapedCom}\\\" {ComServerArguments}\"");
            sb.AppendLine();
            sb.AppendLine($"[HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{{{ExplorerCommandClsid}}}\\ProgID]");
            sb.AppendLine($"@=\"{ExplorerCommandProgId}\"");
            sb.AppendLine();
            sb.AppendLine($"[HKEY_CURRENT_USER\\Software\\Classes\\{ExplorerCommandProgId}]");
            sb.AppendLine("@=\"Batch File Renamer Explorer Command\"");
            sb.AppendLine();
            sb.AppendLine($"[HKEY_CURRENT_USER\\Software\\Classes\\{ExplorerCommandProgId}\\CLSID]");
            sb.AppendLine($"@=\"{{{ExplorerCommandClsid}}}\"");
            sb.AppendLine();
        }

        foreach (var target in Targets)
        {
            var isBackground = target.EndsWith(@"\Background\shell", StringComparison.Ordinal);
            var registryPath = target.Replace(@"Software\Classes\", @"HKEY_CURRENT_USER\Software\Classes\");
            var multiSelectModel = useCom ? "Player" : (isBackground ? "Single" : "Document");

            sb.AppendLine($"[{registryPath}\\{verbKey}]");
            sb.AppendLine($"@=\"{EscapeRegValue(displayName)}\"");
            sb.AppendLine($"\"Icon\"=\"\\\"{escapedApp}\\\"\"");
            sb.AppendLine($"\"MultiSelectModel\"=\"{multiSelectModel}\"");

            if (useCom)
            {
                sb.AppendLine($"\"ExplorerCommandHandler\"=\"{{{ExplorerCommandClsid}}}\"");
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine($"[{registryPath}\\{verbKey}\\command]");
                sb.AppendLine(isBackground
                    ? $"@=\"\\\"{escapedApp}\\\" \\\"%V\\\"\""
                    : $"@=\"\\\"{escapedApp}\\\" \\\"%1\\\"\"");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string EscapeRegValue(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);
}
