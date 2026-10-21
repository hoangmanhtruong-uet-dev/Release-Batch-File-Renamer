namespace BFR.Shell;

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Định nghĩa COM-visible của <c>IExplorerCommand</c> dùng để tạo CCW cho
/// tiến trình COM server out-of-process. Không dùng <see cref="ComImportAttribute"/>
/// vì interface này do phía .NET triển khai và được marshaller đóng gói.
/// </summary>
[ComVisible(true)]
[Guid("a08ce4d0-fa25-44ab-b57c-c7b1c323e0b9")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IExplorerCommandServer
{
    [PreserveSig]
    int GetTitle(IShellItemArray? psiItemArray, out IntPtr ppszName);

    [PreserveSig]
    int GetIcon(IShellItemArray? psiItemArray, out IntPtr ppszIcon);

    [PreserveSig]
    int GetToolTip(IShellItemArray? psiItemArray, out IntPtr ppszInfotip);

    [PreserveSig]
    int GetCanonicalName(out Guid pguidCommandName);

    [PreserveSig]
    int GetState(IShellItemArray? psiItemArray, int fOkToBeSlow, out uint pCmdState);

    [PreserveSig]
    int Invoke(IShellItemArray? psiItemArray, IntPtr pbc);

    [PreserveSig]
    int GetFlags(out uint pFlags);

    [PreserveSig]
    int EnumSubCommands(out IntPtr ppEnum);
}

/// <summary>
/// Adapter chuyển lời gọi COM sang <see cref="ExplorerCommand"/>. Tách riêng
/// khỏi lớp <see cref="ExplorerCommand"/> để có thể tạo CCW từ một interface
/// managed thuần, tránh lỗi khi expose interface <c>ComImport</c>.
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class ExplorerCommandServer : IExplorerCommandServer
{
    private readonly ExplorerCommand _inner = new();

    public int GetTitle(IShellItemArray? psiItemArray, out IntPtr ppszName)
    {
        ComServerLifetime.Touch();
        return _inner.GetTitle(psiItemArray, out ppszName);
    }

    public int GetIcon(IShellItemArray? psiItemArray, out IntPtr ppszIcon)
    {
        ComServerLifetime.Touch();
        return _inner.GetIcon(psiItemArray, out ppszIcon);
    }

    public int GetToolTip(IShellItemArray? psiItemArray, out IntPtr ppszInfotip)
    {
        ComServerLifetime.Touch();
        return _inner.GetToolTip(psiItemArray, out ppszInfotip);
    }

    public int GetCanonicalName(out Guid pguidCommandName)
    {
        ComServerLifetime.Touch();
        return _inner.GetCanonicalName(out pguidCommandName);
    }

    public int GetState(IShellItemArray? psiItemArray, int fOkToBeSlow, out uint pCmdState)
    {
        ComServerLifetime.Touch();
        return _inner.GetState(psiItemArray, fOkToBeSlow, out pCmdState);
    }

    public int Invoke(IShellItemArray? psiItemArray, IntPtr pbc)
    {
        ComServerLifetime.Touch();
        return _inner.Invoke(psiItemArray, pbc);
    }

    public int GetFlags(out uint pFlags)
    {
        ComServerLifetime.Touch();
        return _inner.GetFlags(out pFlags);
    }

    public int EnumSubCommands(out IntPtr ppEnum)
    {
        ComServerLifetime.Touch();
        return _inner.EnumSubCommands(out ppEnum);
    }
}
