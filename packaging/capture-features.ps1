param(
    [string]$OutDir = "docs\ui-features",
    [string]$Theme = "Light"
)

# Chụp ảnh các tính năng mới (A1 Trình thử Regex · A2 kéo-thả quy tắc ·
# A3 cửa sổ phụ · A4 Command Palette) để đưa vào báo cáo.

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class CapFeature {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$outPath = Join-Path $repo $OutDir
New-Item -ItemType Directory -Force $outPath | Out-Null

$appData = Join-Path $env:LOCALAPPDATA 'BatchFileRenamer'
$exe = Join-Path $repo 'src\BFR.App\bin\Release\net8.0-windows10.0.19041.0\BatchFileRenamer.exe'

function Write-Settings([string]$theme) {
    New-Item -ItemType Directory -Force $appData | Out-Null
    $json = @{ Language = 'vi'; Theme = $theme; CheckForUpdates = $false } | ConvertTo-Json -Compress
    [System.IO.File]::WriteAllText((Join-Path $appData 'settings.json'), $json, (New-Object System.Text.UTF8Encoding($false)))
}

function Save-Window([IntPtr]$handle, [string]$file) {
    $rect = New-Object CapFeature+RECT
    [CapFeature]::GetWindowRect($handle, [ref]$rect) | Out-Null
    [int]$w = $rect.Right - $rect.Left
    [int]$h = $rect.Bottom - $rect.Top
    if ($w -le 0 -or $h -le 0) { throw "Không lấy được kích thước cửa sổ." }

    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [CapFeature]::PrintWindow($handle, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc)
    $g.Dispose()
    $bmp.Save($file, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  -> $(Split-Path $file -Leaf) (${w}x${h})"
}

function Find-ByName($root, [string]$name, $controlType) {
    $cond = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, $name)),
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $controlType)))
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
}

function Find-Dialog($root, [string]$name) {
    $cond = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, $name)),
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Window)))
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
}

function Start-App {
    Get-Process BatchFileRenamer -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -like '*BatchFileRenamer*' } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Milliseconds 1200

    $proc = Start-Process -FilePath $exe -PassThru
    Start-Sleep -Seconds 7
    return $proc
}

function Get-AppWindow([int]$processId) {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $processId)
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
}

Write-Settings $Theme

# Dữ liệu mẫu để Rule Builder hiện ra (không chụp empty state).
$sampleDir = Join-Path $env:TEMP 'bfr-capture-samples'
if (Test-Path $sampleDir) { Remove-Item $sampleDir -Recurse -Force }
New-Item -ItemType Directory -Force $sampleDir | Out-Null
$sampleNames = @('Anh 01.jpg', 'Anh 02.jpg', 'Bao cao thang 5.pdf', 'Hop dong 2024.docx', 'anh 10.jpg')
foreach ($n in $sampleNames) { [System.IO.File]::WriteAllText((Join-Path $sampleDir $n), 'demo') }
$listFile = Join-Path $sampleDir 'list.txt'
$sampleNames | ForEach-Object { Join-Path $sampleDir $_ } | Set-Content -LiteralPath $listFile -Encoding UTF8

Get-Process BatchFileRenamer -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -like '*BatchFileRenamer*' } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Start-Sleep -Milliseconds 1200

$proc = Start-Process -FilePath $exe -PassThru -ArgumentList ('--file-list "' + $listFile + '" --preset "Đánh số theo thứ tự"')
Start-Sleep -Seconds 8

try {
    $win = Get-AppWindow $proc.Id
    if (-not $win) { throw "Không mở được cửa sổ app." }

    # ---------- 1) Rule Builder (kéo-thả + nút lên/xuống + nút Regex) ----------
    Write-Host "Rule Builder" -ForegroundColor Cyan
    $addRule = Find-ByName $win 'Add rule button' ([System.Windows.Automation.ControlType]::Button)
    if ($addRule) {
        $addRule.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
        Start-Sleep -Milliseconds 900

        $menuCond = New-Object System.Windows.Automation.AndCondition(
            (New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                [System.Windows.Automation.ControlType]::MenuItem)),
            (New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)))
        $items = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
            [System.Windows.Automation.TreeScope]::Descendants, $menuCond)
        foreach ($item in $items) {
            if ($item.Current.Name -notmatch '^[-–—]{2,}$') {
                $item.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
                break
            }
        }
        Start-Sleep -Milliseconds 1200
    }

    # Mở rộng thẻ quy tắc để thấy form tham số (có nhãn đã địa phương hóa).
    $expand = Find-ByName $win 'Mở tuỳ chọn của quy tắc' ([System.Windows.Automation.ControlType]::Button)
    if (-not $expand) { $expand = Find-ByName $win 'Open rule options' ([System.Windows.Automation.ControlType]::Button) }
    if ($expand) {
        $expand.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
        Start-Sleep -Milliseconds 1200
    }

    Save-Window ([IntPtr]$win.Current.NativeWindowHandle) (Join-Path $outPath 'feature-1-rule-builder.png')

    # ---------- 2) Trình thử Regex ----------
    Write-Host "Trình thử Regex" -ForegroundColor Cyan
    $regexBtn = Find-ByName $win 'Open regex tester' ([System.Windows.Automation.ControlType]::Button)
    if ($regexBtn) {
        $regexBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
        Start-Sleep -Seconds 3

        $regex = Find-Dialog $win 'Regex tester dialog'
        if ($regex) {
            $pattern = Find-ByName $regex 'Regex pattern' ([System.Windows.Automation.ControlType]::Edit)
            $replacement = Find-ByName $regex 'Regex replacement' ([System.Windows.Automation.ControlType]::Edit)
            $input = Find-ByName $regex 'Regex test string' ([System.Windows.Automation.ControlType]::Edit)

            $pattern.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('^(?<date>\d{8})_(?<name>.+)$')
            Start-Sleep -Milliseconds 500
            $replacement.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('${name}_${date}')
            Start-Sleep -Milliseconds 500
            $input.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('20240501_bao-cao-thang.txt')
            Start-Sleep -Seconds 1

            Save-Window ([IntPtr]$regex.Current.NativeWindowHandle) (Join-Path $outPath 'feature-2-regex-tester.png')

            # Đóng hộp thoại
            $close = Find-ByName $regex 'Close regex tester' ([System.Windows.Automation.ControlType]::Button)
            if ($close) { $close.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
            Start-Sleep -Milliseconds 900
        }
    }

    # ---------- 3) Command Palette ----------
    Write-Host "Command Palette" -ForegroundColor Cyan
    Add-Type -AssemblyName Microsoft.VisualBasic
    Add-Type -AssemblyName System.Windows.Forms
    $palette = $null
    for ($attempt = 0; $attempt -lt 5 -and $null -eq $palette; $attempt++) {
        [Microsoft.VisualBasic.Interaction]::AppActivate($proc.Id) | Out-Null
        [CapFeature]::SetForegroundWindow([IntPtr]$win.Current.NativeWindowHandle) | Out-Null
        Start-Sleep -Milliseconds 800
        [System.Windows.Forms.SendKeys]::SendWait('^k')
        Start-Sleep -Seconds 2
        $palette = Find-Dialog $win 'Command palette dialog'
    }
    if ($palette) {
        $search = Find-ByName $palette 'Command palette search' ([System.Windows.Automation.ControlType]::Edit)
        if ($search) {
            $search.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('th')
            Start-Sleep -Seconds 1
        }

        Save-Window ([IntPtr]$palette.Current.NativeWindowHandle) (Join-Path $outPath 'feature-3-command-palette.png')

        [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
        Start-Sleep -Milliseconds 800
    }

    # ---------- 4) Cửa sổ cài đặt ----------
    Write-Host "Cửa sổ cài đặt" -ForegroundColor Cyan
    $settingsBtn = Find-ByName $win 'Open settings' ([System.Windows.Automation.ControlType]::Button)
    if ($settingsBtn) {
        $settingsBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
        Start-Sleep -Seconds 2

        $settings = Find-Dialog $win 'Settings window'
        if ($settings) {
            Save-Window ([IntPtr]$settings.Current.NativeWindowHandle) (Join-Path $outPath 'feature-4-settings.png')

            $close = Find-ByName $settings 'Close settings' ([System.Windows.Automation.ControlType]::Button)
            if ($close) { $close.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
            Start-Sleep -Milliseconds 900
        }
    }
}
finally {
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
}

Write-Host "Đã lưu ảnh tại: $outPath" -ForegroundColor Green
