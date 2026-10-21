namespace BFR.Shell.SmokeTest;

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Kiểm thử khói cho COM local server đã đóng gói. Chạy sau khi đăng ký
/// BFR.Shell.exe bằng <c>bfr context-menu register</c>.
/// </summary>
public static class Program
{
    private const string ExplorerCommandClsid = "6B0F3C2A-7E4D-4A8B-9C1D-2F5E8A7B6C4D";

    public static int Main()
    {
        try
        {
            var clsid = Guid.Parse(ExplorerCommandClsid);
            var type = Type.GetTypeFromCLSID(clsid, throwOnError: true);
            var instance = Activator.CreateInstance(type!);
            if (instance is not IExplorerCommand command)
            {
                Console.Error.WriteLine("Không tạo được IExplorerCommand.");
                return 1;
            }

            int hr = command.GetTitle(IntPtr.Zero, out var titlePtr);
            RequireHr("GetTitle", hr);
            var title = Marshal.PtrToStringUni(titlePtr);
            if (titlePtr != IntPtr.Zero) Marshal.FreeCoTaskMem(titlePtr);
            if (string.IsNullOrWhiteSpace(title))
            {
                Console.Error.WriteLine("GetTitle trả về chuỗi rỗng.");
                return 1;
            }

            hr = command.GetIcon(IntPtr.Zero, out var iconPtr);
            RequireHr("GetIcon", hr);
            var icon = Marshal.PtrToStringUni(iconPtr);
            if (iconPtr != IntPtr.Zero) Marshal.FreeCoTaskMem(iconPtr);
            if (string.IsNullOrWhiteSpace(icon))
            {
                Console.Error.WriteLine("GetIcon không tìm thấy BatchFileRenamer.exe cạnh BFR.Shell.exe.");
                return 1;
            }

            hr = command.GetState(IntPtr.Zero, 1, out var state);
            RequireHr("GetState", hr);
            if (state != 0)
            {
                Console.Error.WriteLine($"GetState trả về {state}, mong đợi 0 (ECS_ENABLED).");
                return 1;
            }

            hr = command.GetFlags(out var flags);
            RequireHr("GetFlags", hr);
            if (flags != 0)
            {
                Console.Error.WriteLine($"GetFlags trả về {flags}, mong đợi 0 (ECF_DEFAULT).");
                return 1;
            }

            hr = command.GetCanonicalName(out var canonical);
            RequireHr("GetCanonicalName", hr);
            if (canonical != clsid)
            {
                Console.Error.WriteLine($"GetCanonicalName trả về {canonical}, mong đợi {clsid}.");
                return 1;
            }

            Console.WriteLine($"COM smoke test PASS: title=\"{title}\", icon=\"{icon}\"");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("COM smoke test FAIL: " + ex);
            return 1;
        }
    }

    private static void RequireHr(string method, int hr)
    {
        if (hr < 0)
        {
            throw new InvalidOperationException($"{method} thất bại: 0x{hr:X8}");
        }
    }
}

[ComImport]
[Guid("a08ce4d0-fa25-44ab-b57c-c7b1c323e0b9")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IExplorerCommand
{
    [PreserveSig] int GetTitle(IntPtr psiItemArray, out IntPtr ppszName);
    [PreserveSig] int GetIcon(IntPtr psiItemArray, out IntPtr ppszIcon);
    [PreserveSig] int GetToolTip(IntPtr psiItemArray, out IntPtr ppszInfotip);
    [PreserveSig] int GetCanonicalName(out Guid pguidCommandName);
    [PreserveSig] int GetState(IntPtr psiItemArray, int fOkToBeSlow, out uint pCmdState);
    [PreserveSig] int Invoke(IntPtr psiItemArray, IntPtr pbc);
    [PreserveSig] int GetFlags(out uint pFlags);
    [PreserveSig] int EnumSubCommands(out IntPtr ppEnum);
}
