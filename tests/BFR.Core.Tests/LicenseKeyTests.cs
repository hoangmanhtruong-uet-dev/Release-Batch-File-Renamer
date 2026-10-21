namespace BFR.Core.Tests;

using BFR.Core.Licensing;
using System;
using System.IO;
using Xunit;

/// <summary>
/// Test mã kích hoạt Pro (bản portable) — dùng cho hộp thoại nâng cấp.
/// </summary>
public class LicenseKeyTests
{
    [Fact]
    public void Generate_ProducesValidKey()
    {
        var key = LicenseKey.Generate("seed-abc");

        Assert.StartsWith("BFR-", key);
        Assert.Equal(LicenseKey.KeyError.None, LicenseKey.Validate(key));
    }

    [Fact]
    public void Validate_AcceptsLowercaseAndMissingDashes()
    {
        var key = LicenseKey.Generate("seed-abc");
        var messy = key.Replace("-", string.Empty).ToLowerInvariant();

        Assert.Equal(LicenseKey.KeyError.None, LicenseKey.Validate(messy));
    }

    [Fact]
    public void Validate_RejectsEmpty()
    {
        Assert.Equal(LicenseKey.KeyError.Empty, LicenseKey.Validate(null));
        Assert.Equal(LicenseKey.KeyError.Empty, LicenseKey.Validate("   "));
    }

    [Fact]
    public void Validate_RejectsBadFormat()
    {
        Assert.Equal(LicenseKey.KeyError.BadFormat, LicenseKey.Validate("BFR-ABC"));
        Assert.Equal(LicenseKey.KeyError.BadFormat, LicenseKey.Validate("XXXX-AAAAA-BBBBB-CCCCC-DDDDD"));
        Assert.Equal(LicenseKey.KeyError.BadFormat, LicenseKey.Validate("BFR-AAAAA-BBBBB-CCCCC-DDDD1"));
    }

    [Fact]
    public void Validate_RejectsWrongChecksum()
    {
        var key = LicenseKey.Generate("seed-abc");
        var groups = key.Split('-');

        // Sửa 1 ký tự trong payload → checksum không còn khớp.
        var payload = groups[1].ToCharArray();
        payload[0] = payload[0] == 'A' ? 'B' : 'A';
        groups[1] = new string(payload);

        Assert.Equal(LicenseKey.KeyError.BadChecksum, LicenseKey.Validate(string.Join('-', groups)));
    }

    [Fact]
    public void TryActivateKey_ValidKey_UnlocksPro()
    {
        var fs = new FakeFileSystem();
        var service = new LicenseService(new LicenseStore(fs, "license.json"), trialEnabled: false);
        var key = LicenseKey.Generate("unlock-me");

        var error = service.TryActivateKey(key);

        Assert.Equal(LicenseKey.KeyError.None, error);
        Assert.Equal(LicenseTier.Pro, service.Status.Tier);
        Assert.True(service.ActivatedByKey);
    }

    [Fact]
    public void TryActivateKey_InvalidKey_KeepsFreeAndReportsError()
    {
        var fs = new FakeFileSystem();
        var service = new LicenseService(new LicenseStore(fs, "license.json"), trialEnabled: false);

        // Đúng định dạng nhưng sai checksum.
        var error = service.TryActivateKey("BFR-AAAAA-BBBBB-CCCCC-DDDDD-EEEEE");

        Assert.Equal(LicenseKey.KeyError.BadChecksum, error);
        Assert.Equal(LicenseTier.Free, service.Status.Tier);
    }

    [Fact]
    public void TryActivateKey_WrongFormat_ReportsBadFormat()
    {
        var fs = new FakeFileSystem();
        var service = new LicenseService(new LicenseStore(fs, "license.json"), trialEnabled: false);

        var error = service.TryActivateKey("BFR-XXXXX-XXXXX-XXXXX-XXXXX");

        Assert.Equal(LicenseKey.KeyError.BadFormat, error);
        Assert.Equal(LicenseTier.Free, service.Status.Tier);
    }
}
