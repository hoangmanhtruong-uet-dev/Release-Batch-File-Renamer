namespace BFR.Core.Tests;

using BFR.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

/// <summary>
/// QA-15: CLI phải cho kết quả khớp với giao diện (cùng dùng BFR.Core),
/// tôn trọng --dry-run, --format json và các mã thoát theo đặc tả.
/// </summary>
public class Phase3CliTests
{
    [Fact]
    public void Cli_FreeTier_BlocksRename_AndExplainsProRequired()
    {
        var cliDll = FindCliDll();
        if (cliDll is null) return; // CLI chưa được build trong môi trường này

        var dir = CreateSampleDirectory();
        try
        {
            var (exitCode, stdout, stderr) = RunCli(cliDll,
                new[] { "rename", dir, "--find", "IMG", "--replace", "PIC", "--dry-run", "--format", "json" },
                devTier: null);

            // Mã thoát 1 = lỗi tham số/trừng quyền; CLI là tính năng Pro.
            Assert.Equal(1, exitCode);
            Assert.Contains("Pro", stderr);
            Assert.Empty(stdout);
        }
        finally
        {
            DeleteDirectory(dir);
        }
    }

    [Fact]
    public void Cli_DryRunJson_MatchesCorePreview()
    {
        var cliDll = FindCliDll();
        if (cliDll is null) return;

        var dir = CreateSampleDirectory();
        try
        {
            var (exitCode, stdout, stderr) = RunCli(cliDll,
                new[] { "rename", dir, "--find", "IMG", "--replace", "PIC", "--dry-run", "--format", "json" },
                devTier: "pro");

            Assert.Equal(0, exitCode);

            var cliItems = ParseCliJson(stdout);

            // Tính lại bằng BFR.Core (giống hệt đường dẫn mà giao diện dùng).
            var files = new FileScanner().Scan(new[] { dir }, new ScanOptions());
            var pipeline = new RulePipeline();
            pipeline.Add(new FindReplaceRule { Pattern = "IMG", Replacement = "PIC" });
            var preview = new PreviewEngine(pipeline).Run(files, new RuleContext(DateTime.Now, "seed"));
            var coreItems = preview.Items.Select(i => (Old: i.OldName, New: i.NewName)).ToList();

            Assert.Equal(coreItems.Count, cliItems.Count);
            for (int i = 0; i < coreItems.Count; i++)
            {
                Assert.Equal(coreItems[i].Old, cliItems[i].Old);
                Assert.Equal(coreItems[i].New, cliItems[i].New);
            }
        }
        finally
        {
            DeleteDirectory(dir);
        }
    }

    [Fact]
    public void Cli_LicenseStatus_ReportsTierAndLimits()
    {
        var cliDll = FindCliDll();
        if (cliDll is null) return;

        var (exitCode, stdout, _) = RunCli(cliDll, new[] { "license", "status" }, devTier: null);

        Assert.Equal(0, exitCode);
        Assert.Contains("Cấp giấy phép", stdout);
        Assert.Contains("Số quy tắc tối đa: 3", stdout);
        Assert.Contains("Độ sâu quét tối đa: 2", stdout);
    }

    [Fact]
    public void Cli_ContextMenu_WorksInFreeTier()
    {
        // Tích hợp Explorer là Free: lệnh context-menu phải chạy được ở mọi cấp.
        var cliDll = FindCliDll();
        if (cliDll is null) return;

        var (exitCode, stdout, _) = RunCli(cliDll, new[] { "context-menu", "status" }, devTier: null);

        Assert.Equal(0, exitCode);
        Assert.Contains("menu ngữ cảnh Explorer", stdout);
    }

    // ---------------------------------------------------------------------

    private static List<(string Old, string New)> ParseCliJson(string stdout)
    {
        using var doc = JsonDocument.Parse(stdout);
        return doc.RootElement.EnumerateArray()
            .Select(e => (Old: e.GetProperty("old").GetString() ?? string.Empty,
                          New: e.GetProperty("new").GetString() ?? string.Empty))
            .ToList();
    }

    private static (int ExitCode, string StdOut, string StdErr) RunCli(string cliDll, string[] args, string? devTier)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(cliDll) ?? Environment.CurrentDirectory,
            // CLI ghi tiếng Việt ra UTF-8: đọc kết quả bằng đúng encoding.
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        psi.ArgumentList.Add(cliDll);
        foreach (var a in args) psi.ArgumentList.Add(a);

        if (devTier is not null) psi.Environment["BFR_DEV_TIER"] = devTier;

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(60_000);

        return (process.ExitCode, stdout, stderr);
    }

    private static string? FindCliDll()
    {
        var testDir = AppContext.BaseDirectory;
        var repoRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", ".."));
        var cliBin = Path.Combine(repoRoot, "src", "BFR.Cli", "bin", "Release");

        if (!Directory.Exists(cliBin)) return null;

        return Directory.EnumerateFiles(cliBin, "bfr.dll", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static string CreateSampleDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "bfr-cli-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        File.WriteAllText(Path.Combine(dir, "IMG_001.jpg"), "a");
        File.WriteAllText(Path.Combine(dir, "IMG_002.jpg"), "b");
        File.WriteAllText(Path.Combine(dir, "note.txt"), "c");

        return dir;
    }

    private static void DeleteDirectory(string dir)
    {
        try
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
        catch (IOException)
        {
            // Bỏ qua: thư mục tạm.
        }
    }
}
