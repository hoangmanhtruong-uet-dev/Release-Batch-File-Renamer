namespace BFR.Shell;

using System.Runtime.InteropServices;

/// <summary>
/// Lớp COM tối thiểu để Explorer truyền toàn bộ lựa chọn (IShellItemArray)
/// cho handler trong một lần gọi. Chỉ khai báo các thành viên cần dùng nhưng
/// phải giữ đúng thứ tự vtable của Windows Shell.
/// </summary>
[ComImport]
[Guid("a08ce4d0-fa25-44ab-b57c-c7b1c323e0b9")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComVisible(true)]
public interface IExplorerCommand
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

[ComImport]
[Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComVisible(true)]
public interface IShellItemArray
{
    [PreserveSig]
    int BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppvOut);

    [PreserveSig]
    int GetPropertyStore(int flags, ref Guid riid, out IntPtr ppv);

    [PreserveSig]
    int GetPropertyDescriptionList(IntPtr keyType, ref Guid riid, out IntPtr ppv);

    [PreserveSig]
    int GetAttributes(uint dwAttribFlags, uint sfgaoMask, out uint psfgaoAttribs);

    [PreserveSig]
    int GetCount(out uint pdwNumItems);

    [PreserveSig]
    int GetItemAt(uint dwIndex, out IShellItem? ppsi);

    [PreserveSig]
    int EnumItems(out IntPtr ppenumShellItems);
}

[ComImport]
[Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComVisible(true)]
public interface IShellItem
{
    [PreserveSig]
    int BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);

    [PreserveSig]
    int GetParent(out IShellItem? ppsi);

    [PreserveSig]
    int GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);

    [PreserveSig]
    int GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

    [PreserveSig]
    int Compare(IShellItem? psi, uint hint, out int piOrder);
}

public enum SIGDN : uint
{
    NormalDisplay = 0x00000000,
    ParentRelativeParsing = 0x80018001,
    DesktopAbsoluteParsing = 0x80028000,
    ParentRelativeEditing = 0x80031001,
    DesktopAbsoluteEditing = 0x8004C000,
    FileSysPath = 0x80058000,
    Url = 0x80068000,
    ParentRelativeForAddressBar = 0x8007C001,
    ParentRelative = 0x80080001
}

internal static class HResult
{
    public const int S_OK = 0;
    public const int S_FALSE = 1;
    public const int E_NOTIMPL = unchecked((int)0x80004001);
    public const int E_FAIL = unchecked((int)0x80004005);
    public const int E_INVALIDARG = unchecked((int)0x80070057);
    public const int E_ACCESSDENIED = unchecked((int)0x80070005);
    public const int E_FILE_NOT_FOUND = unchecked((int)0x80070002);
}
