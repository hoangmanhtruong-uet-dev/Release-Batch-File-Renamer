param(
    [string]$Out = "$env:LOCALAPPDATA\Temp\opencode\ui",
    [string]$Theme = "Light",
    [string]$Preset = "Đánh số theo thứ tự"
)

# Chụp màn hình cửa sổ chính BFR ở theme sáng/tối để review UI.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class BfrCapture {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
  [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@

$settings = Join-Path $env:LOCALAPPDATA 'BatchFileRenamer\settings.json'
$dir = Split-Path $settings -Parent
New-Item -ItemType Directory -Force $dir | Out-Null
"{`"Language`":`"vi`",`"Theme`":`"$Theme`"}" | Set-Content -LiteralPath $settings -Encoding UTF8

$demo = Join-Path $env:USERPROFILE 'Desktop\BFR-Demo'
$list = Join-Path $demo 'danh-sach.txt'
Get-ChildItem $demo -Recurse -File | Select-Object -ExpandProperty FullName | Set-Content -LiteralPath $list -Encoding UTF8

Get-Process BatchFileRenamer -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
$exe = $null
$dev = Join-Path (Resolve-Path '.').Path 'src\BFR.App\bin\Release\net8.0-windows10.0.19041.0\BatchFileRenamer.exe'
if (Test-Path $dev) { $exe = $dev }
else { $exe = (Resolve-Path 'artifacts\msix-layout\BatchFileRenamer.exe').Path }

$proc = Start-Process -FilePath $exe -ArgumentList ('--file-list "' + $list + '" --preset "' + $Preset + '"') -PassThru
Start-Sleep -Seconds 8
$proc.Refresh()
$handle = $proc.MainWindowHandle
if ($handle -eq 0) { throw "Không lấy được cửa sổ." }

# Đưa cửa sổ về 1280x720 (ảnh chụp 1600x900 ở DPI 125%).
[BfrCapture]::MoveWindow($handle, 40, 40, 1280, 720, $true) | Out-Null
Start-Sleep -Seconds 2

$rect = New-Object BfrCapture+RECT
[BfrCapture]::GetWindowRect($handle, [ref]$rect) | Out-Null
[int]$w = $rect.Right - $rect.Left
[int]$h = $rect.Bottom - $rect.Top

$bmp = New-Object System.Drawing.Bitmap 1920, 1080
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[BfrCapture]::PrintWindow($handle, $hdc, 2) | Out-Null
$g.ReleaseHdc($hdc)
$g.Dispose()

$crop = New-Object System.Drawing.Bitmap 1600, 900
$g2 = [System.Drawing.Graphics]::FromImage($crop)
$g2.DrawImage($bmp, (New-Object System.Drawing.Rectangle 0, 0, 1600, 900), (New-Object System.Drawing.Rectangle 0, 0, 1600, 900), [System.Drawing.GraphicsUnit]::Pixel)
$g2.Dispose()
$bmp.Dispose()

New-Item -ItemType Directory -Force (Split-Path $Out -Parent) | Out-Null
$crop.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$crop.Dispose()

Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
Write-Output "Đã chụp: $Out (cửa sổ ${w}x${h})"
