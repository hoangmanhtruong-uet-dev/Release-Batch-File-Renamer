<#
.SYNOPSIS
    Quản lý chứng chỉ ký dev (self-signed) cho Batch File Renamer.

.DESCRIPTION
    Máy dev có thể bật "Smart App Control / Code Integrity (WDAC)" chặn việc nạp các
    DLL mới build (chưa ký) vào test host — khiến `dotnet test` báo:
        "An Application Control policy has blocked this file (0x800711C7)".

    Script này tạo chứng chỉ tự ký, cài **public key** vào
    **LocalMachine\TrustedPeople** (KHÔNG dùng Trusted Root để không mở rộng
    niềm tin gốc của máy), rồi ký các DLL/EXE trong thư mục build.

    Sau khi test xong: chạy  .\packaging\dev-cert.ps1 -Remove

.EXAMPLE
    .\packaging\dev-cert.ps1 -Install
    .\packaging\dev-cert.ps1 -Sign -Path src\BFR.Core\bin\Release\net8.0
    .\packaging\dev-cert.ps1 -Remove
#>
[CmdletBinding(DefaultParameterSetName = 'Install')]
param(
    [Parameter(ParameterSetName = 'Install', Mandatory = $true)]
    [switch]$Install,

    [Parameter(ParameterSetName = 'Remove', Mandatory = $true)]
    [switch]$Remove,

    [Parameter(ParameterSetName = 'Sign', Mandatory = $true)]
    [switch]$Sign,

    # Thư mục chứa DLL/EXE cần ký (dùng với -Sign)
    [string]$Path = '.',

    # Tên chứng chỉ dev
    [string]$Subject = 'CN=BFR Dev Code Signing'
)

$ErrorActionPreference = 'Stop'

$certDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'build\certs'
$cerFile = Join-Path $certDir 'bfr-dev.cer'
$pfxFile = Join-Path $certDir 'bfr-dev.pfx'
$pfxPassword = 'bfr-dev'

function Get-DevCert {
    param([string]$Location = 'CurrentUser')
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store('My', $Location)
    $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadOnly)
    try {
        return $store.Certificates | Where-Object { $_.Subject -eq $Subject } | Select-Object -First 1
    } finally {
        $store.Close()
    }
}

function Install-TrustedPeople {
    param(
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate,
        [string]$Scope = 'LocalMachine'
    )

    $public = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($Certificate.RawData)

    foreach ($candidate in @($Scope, 'CurrentUser') | Select-Object -Unique) {
        try {
            $store = New-Object System.Security.Cryptography.X509Certificates.X509Store('TrustedPeople', $candidate)
            $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
            try {
                $store.Add($public)
            } finally {
                $store.Close()
            }

            Write-Host "Đã cài chứng chỉ vào $candidate\TrustedPeople." -ForegroundColor Green
            return $candidate
        } catch {
            Write-Host "Không cài được vào $candidate\TrustedPeople: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }

    throw "Không cài được chứng chỉ vào TrustedPeople (cần chạy PowerShell với quyền Administrator)."
}

function Remove-TrustedPeople {
    param([System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate)

    $public = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($Certificate.RawData)

    foreach ($candidate in @('LocalMachine', 'CurrentUser')) {
        try {
            $store = New-Object System.Security.Cryptography.X509Certificates.X509Store('TrustedPeople', $candidate)
            $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
            try {
                foreach ($c in @($store.Certificates | Where-Object { $_.Thumbprint -eq $public.Thumbprint })) {
                    $store.Remove($c)
                }
            } finally {
                $store.Close()
            }
            Write-Host "Đã gỡ khỏi $candidate\TrustedPeople." -ForegroundColor Green
        } catch {
            Write-Host "Bỏ qua $candidate\TrustedPeople: $($_.Exception.Message)" -ForegroundColor DarkGray
        }
    }
}

switch ($PSCmdlet.ParameterSetName) {
    'Install' {
        New-Item -ItemType Directory -Force $certDir | Out-Null

        $cert = Get-DevCert
        if (-not $cert) {
            $cert = New-SelfSignedCertificate `
                -Type CodeSigningCert `
                -Subject $Subject `
                -CertStoreLocation Cert:\CurrentUser\My `
                -NotAfter (Get-Date).AddYears(2)
            Write-Host "Đã tạo chứng chỉ dev: $($cert.Thumbprint)" -ForegroundColor Green
        } else {
            Write-Host "Dùng lại chứng chỉ dev: $($cert.Thumbprint)" -ForegroundColor DarkGray
        }

        # Xuất public (.cer) và private (.pfx) — cả hai đều được .gitignore bỏ qua.
        [System.IO.File]::WriteAllBytes($cerFile, $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))
        $exported = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $pfxPassword)
        [System.IO.File]::WriteAllBytes($pfxFile, $exported)
        Write-Host "Đã xuất: $cerFile" -ForegroundColor DarkGray
        Write-Host "Đã xuất: $pfxFile (mật khẩu: $pfxPassword)" -ForegroundColor DarkGray

        $scope = Install-TrustedPeople -Certificate $cert -Scope $StoreScope
        Write-Host "Nhớ chạy '.\packaging\dev-cert.ps1 -Remove' khi test xong." -ForegroundColor Yellow
        break
    }

    'Remove' {
        $cert = Get-DevCert
        if ($cert) {
            Remove-TrustedPeople -Certificate $cert

            $my = New-Object System.Security.Cryptography.X509Certificates.X509Store('My', 'CurrentUser')
            $my.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
            try {
                foreach ($c in @($my.Certificates | Where-Object { $_.Thumbprint -eq $cert.Thumbprint })) {
                    $my.Remove($c)
                }
            } finally {
                $my.Close()
            }
            Write-Host "Đã xoá chứng chỉ khỏi CurrentUser\My." -ForegroundColor Green
        } else {
            Write-Host "Không tìm thấy chứng chỉ dev trong CurrentUser\My." -ForegroundColor DarkGray
        }

        foreach ($file in @($cerFile, $pfxFile)) {
            if (Test-Path $file) {
                Remove-Item $file -Force
                Write-Host "Đã xoá $file" -ForegroundColor DarkGray
            }
        }
        break
    }

    'Sign' {
        $cert = Get-DevCert
        if (-not $cert) { throw "Chưa có chứng chỉ dev. Chạy trước: .\packaging\dev-cert.ps1 -Install" }

        $files = Get-ChildItem -Path $Path -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Extension -in '.dll', '.exe' }
        $signed = 0
        foreach ($file in $files) {
            $result = Set-AuthenticodeSignature -FilePath $file.FullName -Certificate $cert -HashAlgorithm SHA256 -ErrorAction SilentlyContinue
            if ($result.Status -eq 'Valid') { $signed++ }
        }
        Write-Host "Đã ký $signed/$($files.Count) tệp trong '$Path'." -ForegroundColor Green
        break
    }
}
