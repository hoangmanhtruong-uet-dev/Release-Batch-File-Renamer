namespace BFR.Presentation.Tests;

using BFR.Core;
using BFR.Core.Licensing;
using BFR.Presentation;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

/// <summary>Va chạm tên với tệp NGOÀI lô phải được phát hiện trước khi đổi tên (lỗi có sẵn đã sửa).</summary>
public class OutsideCollisionTests : IDisposable
{
    private readonly string _temp = Path.Combine(Path.GetTempPath(), "bfr-outside-" + Guid.NewGuid().ToString("N"));

    public OutsideCollisionTests() => Directory.CreateDirectory(_temp);

    public void Dispose()
    {
        try { if (Directory.Exists(_temp)) Directory.Delete(_temp, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public async Task RenamingOntoAnExistingFileOutsideTheBatch_IsAConflict_AndBlocksApply()
    {
        var files = Path.Combine(_temp, "files");
        Directory.CreateDirectory(files);
        File.WriteAllText(Path.Combine(files, "a.txt"), "x");
        File.WriteAllText(Path.Combine(files, "001.txt"), "y");   // đã có sẵn, không thuộc lô

        var fs = new RealFileSystem();
        var license = new LicenseService(new LicenseStore(fs, Path.Combine(_temp, "license.json")), trialEnabled: false);
        var vm = new MainViewModel(new LicenseCoordinator(license), null, _temp);

        vm.AddPaths(new[] { Path.Combine(files, "a.txt") });
        vm.AddRule("FindReplace");
        var card = vm.Rules.Single();
        card.Parameters.Single(p => p.Key == "pattern").TextValue = "a";
        card.Parameters.Single(p => p.Key == "replacement").TextValue = "001";
        await vm.RebuildAsync();

        var row = Assert.Single(vm.Rows);
        Assert.Equal("001.txt", row.NewName);
        Assert.Equal(PreviewStatus.Conflict, row.Status);
        Assert.False(vm.CanApply);
    }

    [Fact]
    public async Task EditingAParameterOfAFreshlyAddedRule_RefreshesThePreview()
    {
        var files = Path.Combine(_temp, "f2");
        Directory.CreateDirectory(files);
        File.WriteAllText(Path.Combine(files, "a.txt"), "x");

        var fs = new RealFileSystem();
        var license = new LicenseService(new LicenseStore(fs, Path.Combine(_temp, "license2.json")), trialEnabled: false);
        var vm = new MainViewModel(new LicenseCoordinator(license), null, _temp);
        vm.AddPaths(new[] { Path.Combine(files, "a.txt") });
        vm.AddRule("FindReplace");
        await vm.RebuildAsync();

        var card = vm.Rules.Single();
        card.Parameters.Single(p => p.Key == "pattern").TextValue = "a";
        card.Parameters.Single(p => p.Key == "replacement").TextValue = "Z";

        for (var i = 0; i < 40 && vm.Rows.Single().NewName != "Z.txt"; i++) await Task.Delay(50);
        Assert.Equal("Z.txt", vm.Rows.Single().NewName);
    }
}
