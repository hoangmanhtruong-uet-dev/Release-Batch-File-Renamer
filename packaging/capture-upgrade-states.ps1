param(
    [string]$OutDir = "docs\ui-upgrade",
    [string]$Theme = "Light"
)

# Chụp ảnh từng trạng thái của hộp thoại nâng cấp Pro.
# Mỗi trạng thái được tạo bằng cách ghi license.json rồi mở app và bấm nút nâng cấp.

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class CapUpgrade {
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
$licenseFile = Join-Path $appData 'license.json'
$settingsFile = Join-Path $appData 'settings.json'
$exe = Join-Path $repo 'src\BFR.App\bin\Release\net8.0-windows10.0.19041.0\BatchFileRenamer.exe'

function Write-License([hashtable]$state) {
    New-Item -ItemType Directory -Force $appData | Out-Null
    $json = $state | ConvertTo-Json -Compress
    [System.IO.File]::WriteAllText($licenseFile, $json, (New-Object System.Text.UTF8Encoding($false)))
}

function Write-Settings([string]$theme) {
    New-Item -ItemType Directory -Force $appData | Out-Null
    $json = @{ Language = 'vi'; Theme = $theme; CheckForUpdates = $false } | ConvertTo-Json -Compress
    [System.IO.File]::WriteAllText($settingsFile, $json, (New-Object System.Text.UTF8Encoding($false)))
}

function Save-Window([IntPtr]$handle, [string]$file) {
    $rect = New-Object CapUpgrade+RECT
    [CapUpgrade]::GetWindowRect($handle, [ref]$rect) | Out-Null
    [int]$w = $rect.Right - $rect.Left
    [int]$h = $rect.Bottom - $rect.Top

    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [CapUpgrade]::PrintWindow($handle, $hdc, 2) | Out-Null
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

function Start-App {
    Get-Process BatchFileRenamer -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 800
    $proc = Start-Process -FilePath $exe -PassThru
    Start-Sleep -Seconds 6
    return $proc
}

function Get-AppWindow([int]$processId) {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $processId)
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
}

function Capture-State {
    param([string]$name, [hashtable]$license, [switch]$Context, [switch]$InfoScreen)

    Write-Host "Trạng thái: $name" -ForegroundColor Cyan
    Write-License $license
    Write-Settings $Theme

    $proc = Start-App
    try {
        $win = Get-AppWindow $proc.Id
        if (-not $win) { throw "Không mở được cửa sổ app." }

        # Chờ cửa sổ sẵn sàng (nút badge/upgrade xuất hiện và bật).
        $badge = $null
        for ($i = 0; $i -lt 30; $i++) {
            $badge = Find-ByName $win 'License tier' ([System.Windows.Automation.ControlType]::Button)
            if (-not $badge) { $badge = Find-ByName $win 'Upgrade to Pro' ([System.Windows.Automation.ControlType]::Button) }
            if ($badge -and $badge.Current.IsEnabled) { break }
            Start-Sleep -Milliseconds 500
        }

        if ($Context) {
            # Chạm vào tính năng Pro: thêm quy tắc thứ 4 (Free chỉ cho 3) để mở hộp thoại nâng cấp.
            for ($i = 0; $i -lt 4; $i++) {
                $addRule = Find-ByName $win 'Add rule button' ([System.Windows.Automation.ControlType]::Button)
                if (-not $addRule) { break }

                $addRule.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
                Start-Sleep -Milliseconds 700

                # Chọn loại quy tắc đầu tiên trong menu ngữ cảnh (menu là popup riêng).
                $menuTypeCond = New-Object System.Windows.Automation.PropertyCondition(
                    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                    [System.Windows.Automation.ControlType]::MenuItem)
                $menuPidCond = New-Object System.Windows.Automation.PropertyCondition(
                    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
                $menuCond = New-Object System.Windows.Automation.AndCondition($menuTypeCond, $menuPidCond)

                $menuItem = $null
                $desktop = [System.Windows.Automation.AutomationElement]::RootElement
                foreach ($candidate in $desktop.FindAll([System.Windows.Automation.TreeScope]::Descendants, $menuCond)) {
                    if ($candidate.Current.IsEnabled -and $candidate.Current.Name -notmatch '^-{3,}$') {
                        $menuItem = $candidate
                        break
                    }
                }
                if (-not $menuItem) { break }
                $menuItem.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
                Start-Sleep -Milliseconds 700
            }
        }
        else {
            if (-not $badge) { throw "Không tìm thấy nút nâng cấp/badge." }
            $badge.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
        }

        Start-Sleep -Seconds 2

        # Hộp thoại là cửa sổ con của cửa sổ chính (Owner). Chờ tới khi xuất hiện.
        $dialog = $null
        for ($attempt = 0; $attempt -lt 20 -and -not $dialog; $attempt++) {
            foreach ($candidate in $win.FindAll([System.Windows.Automation.TreeScope]::Descendants,
                (New-Object System.Windows.Automation.PropertyCondition(
                    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                    [System.Windows.Automation.ControlType]::Window)))) {
                $n = $candidate.Current.Name
                if ($n -eq 'Upgrade to Pro dialog' -or $n -eq 'License information') { $dialog = $candidate; break }
            }

            if (-not $dialog) {
                Start-Sleep -Milliseconds 500
                if ($attempt -eq 4 -and -not $Context) {
                    # Thử bấm lại badge (lần đầu có thể chưa sẵn sàng).
                    $again = Find-ByName $win 'License tier' ([System.Windows.Automation.ControlType]::Button)
                    if (-not $again) { $again = Find-ByName $win 'Upgrade to Pro' ([System.Windows.Automation.ControlType]::Button) }
                    if ($again) { $again.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
                }
            }
        }

        if (-not $dialog) { throw "Không mở được hộp thoại nâng cấp." }

        # Đợi hộp thoại vẽ xong (handle hợp lệ) rồi mới chụp.
        for ($i = 0; $i -lt 20; $i++) {
            $h = [IntPtr]$dialog.Current.NativeWindowHandle
            if ($h -ne [IntPtr]::Zero) {
                $r = New-Object CapUpgrade+RECT
                [CapUpgrade]::GetWindowRect($h, [ref]$r) | Out-Null
                if (($r.Right - $r.Left) -gt 0) { break }
            }
            Start-Sleep -Milliseconds 250
        }

        Save-Window ([IntPtr]$dialog.Current.NativeWindowHandle) (Join-Path $outPath "$name.png")
    }
    finally {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 500
    }
}

$now = (Get-Date).ToUniversalTime()

# 1) Chưa dùng thử: có cả "Mua Pro" và "Dùng thử Pro 14 ngày"
Capture-State -Name 'upgrade-1-not-trialed' -license @{}

# 2) Đang dùng thử: ẩn nút dùng thử, hiện số ngày còn lại
Capture-State -Name 'upgrade-2-trialing' -license @{
    TrialStartedUtc = $now.AddDays(-1).ToString('o')
    TrialProductId   = 'BFR_PRO_LIFETIME'
}

# 3) Hết hạn dùng thử: chỉ còn "Mua Pro" + nhập mã kích hoạt
Capture-State -Name 'upgrade-3-trial-expired' -license @{
    TrialStartedUtc = $now.AddDays(-20).ToString('o')
    TrialProductId   = 'BFR_PRO_LIFETIME'
}

# 4) Đã là Pro: badge "Pro" + màn hình thông tin license
Capture-State -Name 'upgrade-4-license-info' -license @{
    PurchasedProductId = 'BFR_PRO_LIFETIME'
    PurchasedUtc      = $now.AddDays(-30).ToString('o')
    ActivationSource  = 'store'
} -InfoScreen

# 5) Ngữ cảnh: chạm tính năng Pro khi đang ở cấp Free
Capture-State -Name 'upgrade-5-context' -license @{} -Context

Write-Host "Đã lưu ảnh tại: $outPath" -ForegroundColor Green
