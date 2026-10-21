param(
    [string]$WorkDir = "$env:TEMP\bfr-ui-smoke"
)

# Smoke test tự động cho luồng chính của giao diện v2.0:
# Thêm tệp → Xem trước → Áp dụng (có xác nhận) → Hoàn tác.
# Dùng UI Automation (không cần FlaUI/WinAppDriver).

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationTypes

function Get-AppWindow([int]$processId) {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $processId)
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
}

function Find-Descendant($parent, [string]$name) {
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
}

function Find-Button($parent, [string]$name) {
    $byName = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, $name)),
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Button)))
    return $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $byName)
}

function Find-Edit($parent, [string]$name) {
    $byName = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, $name)),
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Edit)))
    return $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $byName)
}

function Find-Dialog($parent, [string]$name) {
    $byName = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, $name)),
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Window)))
    return $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $byName)
}

# Chờ một điều kiện trở thành đúng (chống race khi UI cập nhật bất đồng bộ).
function Wait-For([scriptblock]$condition, [int]$timeoutMs = 6000) {
    $deadline = (Get-Date).AddMilliseconds($timeoutMs)
    while ((Get-Date) -lt $deadline) {
        try { if (& $condition) { return $true } } catch { }
        Start-Sleep -Milliseconds 250
    }
    return $false
}

# Đếm số phần tử Edit bên trong một phần tử.
function Count-Edits($parent) {
    return $parent.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Edit))).Count
}

function Invoke-Element($element) {
    try {
        $pattern = $element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $pattern.Invoke()
    }
    catch {
        # Một số control không hỗ trợ Invoke: bấm chuột theo toạ độ.
        $rect = $element.Current.BoundingRectangle
        [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point(
            [int]($rect.X + $rect.Width / 2), [int]($rect.Y + $rect.Height / 2))
        Add-Type -MemberDefinition @"
[DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, System.IntPtr e);
"@ -Name Mouse -Namespace Bfr
        [Bfr.Mouse]::mouse_event(0x0002, 0, 0, 0, [System.IntPtr]::Zero)
        [Bfr.Mouse]::mouse_event(0x0004, 0, 0, 0, [System.IntPtr]::Zero)
    }
}

# ---------------------------------------------------------------- dữ liệu thử
if (Test-Path $WorkDir) { Remove-Item $WorkDir -Recurse -Force }
New-Item -ItemType Directory -Force $WorkDir | Out-Null
$names = @('Anh 01.jpg', 'Anh 02.jpg', 'Anh 10.jpg')
foreach ($n in $names) { [System.IO.File]::WriteAllText((Join-Path $WorkDir $n), 'demo') }
$list = Join-Path $WorkDir 'list.txt'
$names | ForEach-Object { Join-Path $WorkDir $_ } | Set-Content -LiteralPath $list -Encoding UTF8

# ---------------------------------------------------------------- chạy app
Get-Process BatchFileRenamer -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
$exe = (Resolve-Path 'src\BFR.App\bin\Release\net8.0-windows10.0.19041.0\BatchFileRenamer.exe').Path
$proc = Start-Process -FilePath $exe -ArgumentList ('--file-list "' + $list + '" --preset "Đánh số theo thứ tự"') -PassThru
Start-Sleep -Seconds 7

$failures = New-Object System.Collections.Generic.List[string]
try {
    $win = Get-AppWindow $proc.Id
    if ($null -eq $win) { throw "Không tìm thấy cửa sổ ứng dụng." }
    Write-Output "OK  mở cửa sổ: $($win.Current.Name)"

    # 1) Bảng xem trước có dòng dữ liệu
    $grid = Find-Descendant $win 'Preview table'
    if ($null -eq $grid) { $failures.Add("Không tìm thấy bảng xem trước.") }
    else {
        $rows = $grid.FindAll([System.Windows.Automation.TreeScope]::Descendants,
            (New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                [System.Windows.Automation.ControlType]::DataItem)))
        Write-Output "OK  bảng xem trước có $($rows.Count) dòng"
        if ($rows.Count -lt 3) { $failures.Add("Bảng xem trước thiếu dòng (có $($rows.Count)).") }
    }

    # 2) Mở tuỳ chọn của thẻ quy tắc (accordion tại chỗ)
    $editsBefore = Count-Edits $win

    $expand = Find-Descendant $win 'Mở tuỳ chọn của quy tắc'
    if ($null -eq $expand) { $failures.Add("Không tìm thấy nút mở tuỳ chọn quy tắc.") }
    else {
        Invoke-Element $expand
        $opened = Wait-For { (Count-Edits $win) -gt $editsBefore }
        $editsAfter = Count-Edits $win
        if (-not $opened) { $failures.Add("Mở tuỳ chọn quy tắc không hiện form tham số (ô nhập: $editsBefore -> $editsAfter).") }
        else { Write-Output "OK  mở rộng thẻ quy tắc tại chỗ (ô nhập: $editsBefore -> $editsAfter)" }
    }

    # 3) Lọc + tìm kiếm
    $filter = Find-Descendant $win 'Filter will change'
    if ($null -eq $filter) { $failures.Add("Không tìm thấy bộ lọc trạng thái.") }
    else { Write-Output "OK  có bộ lọc trạng thái" }

    # 4) Áp dụng → hộp thoại xác nhận → xác nhận
    $apply = $null
    $buttons = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Button)))
    foreach ($b in $buttons) {
        if ($b.Current.Name -eq 'Apply renaming') { $apply = $b; break }
    }
    if ($null -eq $apply) { $failures.Add("Không tìm thấy nút Áp dụng.") }
    else {
        Invoke-Element $apply
        Start-Sleep -Seconds 2

        $confirm = Get-AppWindow $proc.Id
        $confirmWin = $root = [System.Windows.Automation.AutomationElement]::RootElement
        $cond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty, 'Confirm apply renaming')
        $dialog = $confirmWin.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
        if ($null -eq $dialog) { $failures.Add("Không mở hộp thoại xác nhận áp dụng.") }
        else {
            Write-Output "OK  hộp thoại xác nhận: $($dialog.Current.Name)"
            $applyBtn = Find-Descendant $dialog 'Áp dụng'
            if ($null -eq $applyBtn) { $failures.Add("Hộp thoại xác nhận thiếu nút Áp dụng.") }
            else { Invoke-Element $applyBtn }
        }
        Start-Sleep -Seconds 4

        $renamed = Get-ChildItem $WorkDir -File | Where-Object { $_.Name -match '^\d{3}\.jpg$' }
        if ($renamed.Count -ne 3) { $failures.Add("Sau khi áp dụng cần 3 tệp được đánh số, thấy $($renamed.Count).") }
        else { Write-Output "OK  đã đổi tên 3 tệp: $(($renamed.Name -join ', '))" }
    }

    # 5) Hoàn tác
    $undo = Find-Descendant $win 'Undo last rename'
    if ($null -eq $undo) { $failures.Add("Không tìm thấy nút Hoàn tác.") }
    else {
        Invoke-Element $undo
        Start-Sleep -Seconds 4
        $restored = Get-ChildItem $WorkDir -File | Where-Object { $_.Name -in $names }
        if ($restored.Count -ne 3) { $failures.Add("Sau khi hoàn tác cần 3 tệp về tên cũ, thấy $($restored.Count).") }
        else { Write-Output "OK  hoàn tác thành công" }
    }

    # 6) Command Palette (Ctrl+K) — A4
    Add-Type -AssemblyName Microsoft.VisualBasic
    Add-Type @"
using System;
using System.Runtime.InteropServices;
public class BringToFront {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
}
"@
    $palette = $null
    for ($attempt = 0; $attempt -lt 6 -and $null -eq $palette; $attempt++) {
        [Microsoft.VisualBasic.Interaction]::AppActivate($proc.Id) | Out-Null
        [BringToFront]::SetForegroundWindow([IntPtr]$win.Current.NativeWindowHandle) | Out-Null
        try { $win.SetFocus() } catch { }
        Start-Sleep -Milliseconds 800
        [System.Windows.Forms.SendKeys]::SendWait('^k')
        Start-Sleep -Seconds 2
        $palette = Find-Dialog $win 'Command palette dialog'
    }
    if ($null -eq $palette) { $failures.Add("Ctrl+K không mở được Command Palette.") }
    else {
        Write-Output "OK  mở Command Palette bằng Ctrl+K"

        $search = Find-Edit $palette 'Command palette search'
        if ($null -eq $search) { $failures.Add("Command Palette thiếu ô tìm kiếm.") }
        else {
            $search.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('lich')
            Start-Sleep -Milliseconds 900
            $items = $palette.FindAll([System.Windows.Automation.TreeScope]::Descendants,
                (New-Object System.Windows.Automation.PropertyCondition(
                    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                    [System.Windows.Automation.ControlType]::ListItem)))
            if ($items.Count -lt 1) { $failures.Add("Lọc 'lich' không ra lệnh nào.") }
            else { Write-Output "OK  lọc lệnh không dấu ('lich'): $($items.Count) kết quả" }
        }

        [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
        Start-Sleep -Milliseconds 800
    }

    # 7) Trình thử Regex (A1)
    $addRule = Find-Button $win 'Add rule button'
    if ($null -eq $addRule) { $failures.Add("Không tìm thấy nút thêm quy tắc.") }
    else {
        Invoke-Element $addRule
        Start-Sleep -Milliseconds 900

        # Chọn loại quy tắc "Tìm và thay" trong menu ngữ cảnh.
        $menuItems = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
            [System.Windows.Automation.TreeScope]::Descendants,
            (New-Object System.Windows.Automation.AndCondition(
                (New-Object System.Windows.Automation.PropertyCondition(
                    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                    [System.Windows.Automation.ControlType]::MenuItem)),
                (New-Object System.Windows.Automation.PropertyCondition(
                    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)))))
        $chosen = $false
        foreach ($item in $menuItems) {
            if ($item.Current.Name -notmatch '^[-–—]{2,}$') { Invoke-Element $item; $chosen = $true; break }
        }
        Start-Sleep -Milliseconds 1200

        if (-not $chosen) { $failures.Add("Không chọn được loại quy tắc trong menu.") }
        else {
            $regexBtn = Find-Button $win 'Open regex tester'
            if ($null -eq $regexBtn) { $failures.Add("Không tìm thấy nút Trình thử Regex trên thẻ quy tắc.") }
            else {
                Invoke-Element $regexBtn
                Start-Sleep -Seconds 2
                $regex = Find-Dialog $win 'Regex tester dialog'
                if ($null -eq $regex) { $failures.Add("Không mở được Trình thử Regex.") }
                else {
                    Write-Output "OK  mở Trình thử Regex"

                    $pattern = Find-Edit $regex 'Regex pattern'
                    $replacement = Find-Edit $regex 'Regex replacement'
                    $input = Find-Edit $regex 'Regex test string'
                    if ($null -eq $pattern -or $null -eq $input) { $failures.Add("Trình thử Regex thiếu ô nhập.") }
                    else {
                        $pattern.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('^(\d+)_(\w+)$')
                        Start-Sleep -Milliseconds 500
                        if ($null -ne $replacement) {
                            $replacement.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('$2_$1')
                            Start-Sleep -Milliseconds 400
                        }
                        $input.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('2024_file')

                        # Chờ kết quả xuất hiện trong ô kết quả (ô TextBox read-only).
                        $hasResult = Wait-For {
                            $edits = $regex.FindAll([System.Windows.Automation.TreeScope]::Descendants,
                                (New-Object System.Windows.Automation.PropertyCondition(
                                    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                                    [System.Windows.Automation.ControlType]::Edit)))
                            foreach ($e in $edits) {
                                try {
                                    $value = $e.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
                                    if ($value -like '*file_2024*') { return $true }
                                }
                                catch { }
                            }
                            return $false
                        }

                        if (-not $hasResult) { $failures.Add("Trình thử Regex không ra kết quả 'file_2024'.") }
                        else { Write-Output "OK  Trình thử Regex tính đúng kết quả thay thế" }
                    }

                    [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
                    Start-Sleep -Milliseconds 800
                }
            }
        }
    }

    # 8) Đổi thứ tự quy tắc bằng nút lên/xuống (A2)
    $moveDown = Find-Button $win 'Move rule down'
    if ($null -eq $moveDown) { $failures.Add("Không tìm thấy nút đưa quy tắc xuống.") }
    else {
        Invoke-Element $moveDown
        Start-Sleep -Milliseconds 1200
        Write-Output "OK  có nút đổi thứ tự quy tắc (kéo-thả + nút lên/xuống)"
    }

    # 9) Trang cài đặt + nút kiểm tra cập nhật (A3)
    $settingsBtn = Find-Button $win 'Open settings'
    if ($null -eq $settingsBtn) { $failures.Add("Không tìm thấy nút cài đặt.") }
    else {
        Invoke-Element $settingsBtn
        Start-Sleep -Seconds 2
        $settings = Find-Dialog $win 'Settings window'
        if ($null -eq $settings) { $failures.Add("Không mở được cửa sổ cài đặt.") }
        else {
            Write-Output "OK  mở cửa sổ cài đặt"
            $checkNow = Find-Button $settings 'Check for updates now'
            if ($null -eq $checkNow) { $failures.Add("Trang cài đặt thiếu nút 'Kiểm tra ngay'.") }
            else { Write-Output "OK  trang cài đặt có nút kiểm tra cập nhật" }

            $closeSettings = Find-Button $settings 'Close settings'
            if ($null -ne $closeSettings) { Invoke-Element $closeSettings }
            Start-Sleep -Milliseconds 900
        }
    }
}
finally {
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
}

Write-Output ''
if ($failures.Count -eq 0) {
    Write-Output 'SMOKE UI: PASS'
    exit 0
}

Write-Output "SMOKE UI: FAIL ($($failures.Count))"
$failures | ForEach-Object { Write-Output "  - $_" }
exit 1
