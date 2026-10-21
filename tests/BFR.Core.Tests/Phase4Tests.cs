namespace BFR.Core.Tests;

using BFR.Core;
using BFR.Core.Updates;
using BFR.Core.WatchFolder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

/// <summary>
/// Test Giai đoạn 4 (v1.2): Watch Folder, preset chia sẻ, kiểm tra cập nhật,
/// tối ưu hiệu năng metadata và tính nhất quán đa ngôn ngữ.
/// </summary>
public class Phase4Tests
{
    // ---------------------------------------------------------------------
    // Watch Folder — logic thời gian (độc lập hệ thống tệp)
    // ---------------------------------------------------------------------

    [Fact]
    public void WatchPlanner_FileIsReadyOnlyAfterSettleDelay()
    {
        var planner = new WatchFolderPlanner();
        var t0 = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);

        planner.Notify(@"C:\in\a.txt", t0);

        Assert.Empty(planner.TakeReady(t0.AddMilliseconds(200), TimeSpan.FromMilliseconds(500)));
        Assert.Equal(1, planner.PendingCount);

        var ready = planner.TakeReady(t0.AddMilliseconds(600), TimeSpan.FromMilliseconds(500));
        Assert.Single(ready);
        Assert.Equal(@"C:\in\a.txt", ready[0]);
        Assert.Equal(0, planner.PendingCount);
    }

    [Fact]
    public void WatchPlanner_MoreEventsRestartTheSettleWindow()
    {
        var planner = new WatchFolderPlanner();
        var t0 = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);

        planner.Notify(@"C:\in\b.txt", t0);
        planner.Notify(@"C:\in\b.txt", t0.AddMilliseconds(400));

        Assert.Empty(planner.TakeReady(t0.AddMilliseconds(500), TimeSpan.FromMilliseconds(500)));

        var ready = planner.TakeReady(t0.AddMilliseconds(1000), TimeSpan.FromMilliseconds(500));
        Assert.Single(ready);
    }

    // ---------------------------------------------------------------------
    // Watch Folder — xử lý lô (dùng IFileSystem giả)
    // ---------------------------------------------------------------------

    [Fact]
    public void WatchProcessor_AppliesPresetToNewFiles()
    {
        var fs = new FakeFileSystem();
        fs.WriteAllText(@"C:\watch\IMG_1.txt", "a");
        fs.WriteAllText(@"C:\watch\IMG_2.txt", "b");

        var preset = new Preset
        {
            Name = "Đánh số",
            Rules = new List<RuleDefinition>
            {
                new()
                {
                    Type = "Numbering",
                    Params = new()
                    {
                        ["start"] = System.Text.Json.JsonSerializer.SerializeToElement(1),
                        ["step"] = System.Text.Json.JsonSerializer.SerializeToElement(1),
                        ["width"] = System.Text.Json.JsonSerializer.SerializeToElement(3),
                        ["position"] = System.Text.Json.JsonSerializer.SerializeToElement("ReplaceAll")
                    }
                }
            }
        };

        var processor = new WatchFolderProcessor(fs, "journal");
        var options = new WatchFolderOptions { Folder = @"C:\watch" };

        var result = processor.Process(new[] { @"C:\watch\IMG_1.txt", @"C:\watch\IMG_2.txt" }, preset, options);

        Assert.Equal(2, result.Candidates);
        Assert.Equal(2, result.Renamed);
        Assert.Equal(0, result.Failed);
        Assert.True(fs.FileExists(@"C:\watch\001.txt"));
        Assert.True(fs.FileExists(@"C:\watch\002.txt"));
        Assert.False(fs.FileExists(@"C:\watch\IMG_1.txt"));
        Assert.NotNull(result.JournalPath);
    }

    [Fact]
    public void WatchProcessor_DryRun_DoesNotTouchFilesystem()
    {
        var fs = new FakeFileSystem();
        fs.WriteAllText(@"C:\watch\IMG_1.txt", "a");

        var preset = new Preset
        {
            Name = "Đánh số",
            Rules = new List<RuleDefinition>
            {
                new()
                {
                    Type = "Numbering",
                    Params = new()
                    {
                        ["start"] = System.Text.Json.JsonSerializer.SerializeToElement(1),
                        ["width"] = System.Text.Json.JsonSerializer.SerializeToElement(3),
                        ["position"] = System.Text.Json.JsonSerializer.SerializeToElement("ReplaceAll")
                    }
                }
            }
        };

        var processor = new WatchFolderProcessor(fs, "journal");
        var options = new WatchFolderOptions { Folder = @"C:\watch", DryRun = true };

        var result = processor.Process(new[] { @"C:\watch\IMG_1.txt" }, preset, options);

        Assert.Equal(0, result.Renamed);
        Assert.True(fs.FileExists(@"C:\watch\IMG_1.txt"));
        Assert.Null(result.JournalPath);
    }

    [Fact]
    public void WatchProcessor_ConflictWithExistingFile_IsSkipped()
    {
        var fs = new FakeFileSystem();
        fs.WriteAllText(@"C:\watch\IMG_1.txt", "a");
        fs.WriteAllText(@"C:\watch\001.txt", "existing");

        var preset = new Preset
        {
            Name = "Đánh số",
            Rules = new List<RuleDefinition>
            {
                new()
                {
                    Type = "Numbering",
                    Params = new()
                    {
                        ["start"] = System.Text.Json.JsonSerializer.SerializeToElement(1),
                        ["width"] = System.Text.Json.JsonSerializer.SerializeToElement(3),
                        ["position"] = System.Text.Json.JsonSerializer.SerializeToElement("ReplaceAll")
                    }
                }
            }
        };

        var processor = new WatchFolderProcessor(fs, "journal");
        var options = new WatchFolderOptions { Folder = @"C:\watch" };

        var result = processor.Process(new[] { @"C:\watch\IMG_1.txt" }, preset, options);

        Assert.Equal(0, result.Renamed);
        Assert.True(result.Skipped >= 1);
        Assert.True(fs.FileExists(@"C:\watch\001.txt"));
        Assert.True(fs.FileExists(@"C:\watch\IMG_1.txt"));
    }

    [Fact]
    public void WatchProcessor_RespectsIncludeAndExcludeGlobs()
    {
        var fs = new FakeFileSystem();
        fs.WriteAllText(@"C:\watch\a.jpg", "1");
        fs.WriteAllText(@"C:\watch\b.txt", "2");
        fs.WriteAllText(@"C:\watch\c.jpg", "3");

        var preset = new Preset
        {
            Name = "Đánh số",
            Rules = new List<RuleDefinition>
            {
                new()
                {
                    Type = "Numbering",
                    Params = new()
                    {
                        ["start"] = System.Text.Json.JsonSerializer.SerializeToElement(1),
                        ["width"] = System.Text.Json.JsonSerializer.SerializeToElement(3),
                        ["position"] = System.Text.Json.JsonSerializer.SerializeToElement("ReplaceAll")
                    }
                }
            }
        };

        var processor = new WatchFolderProcessor(fs, "journal");
        var options = new WatchFolderOptions { Folder = @"C:\watch", IncludeGlob = "*.jpg", ExcludeGlob = "c.*" };

        var result = processor.Process(new[] { @"C:\watch\a.jpg", @"C:\watch\b.txt", @"C:\watch\c.jpg" }, preset, options);

        Assert.Equal(1, result.Candidates);
        Assert.True(fs.FileExists(@"C:\watch\001.jpg"));
        Assert.True(fs.FileExists(@"C:\watch\b.txt"));
    }

    [Fact]
    public async Task WatchService_RenamesNewFile_AndIgnoresExistingOnes()
    {
        var dir = Path.Combine(Path.GetTempPath(), "bfr-watch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        try
        {
            var existing = Path.Combine(dir, "OLD.txt");
            File.WriteAllText(existing, "old");

            var preset = new Preset
            {
                Name = "Đánh số",
                Rules = new List<RuleDefinition>
                {
                    new()
                    {
                        Type = "Numbering",
                        Params = new()
                        {
                            ["start"] = System.Text.Json.JsonSerializer.SerializeToElement(1),
                            ["width"] = System.Text.Json.JsonSerializer.SerializeToElement(3),
                            ["position"] = System.Text.Json.JsonSerializer.SerializeToElement("ReplaceAll")
                        }
                    }
                }
            };

            var options = new WatchFolderOptions
            {
                Folder = dir,
                SettleDelayMs = 300,
                DebounceMs = 200
            };

            var processor = new WatchFolderProcessor(new RealFileSystem(), Path.Combine(dir, "journal"));
            using var service = new WatchFolderService(options, preset, processor, new RealFileSystem());
            service.Start();

            // Tệp có sẵn trước khi bắt đầu không được xử lý.
            Assert.True(File.Exists(existing));

            var fresh = Path.Combine(dir, "NEW.txt");
            File.WriteAllText(fresh, "new");

            var renamed = Path.Combine(dir, "001.txt");
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < deadline && !File.Exists(renamed))
            {
                await Task.Delay(200);
            }

            Assert.True(File.Exists(renamed), "Tệp mới không được đổi tên.");
            Assert.True(File.Exists(existing), "Tệp có sẵn không được đổi tên.");
        }
        finally
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch (IOException) { }
        }
    }

    // ---------------------------------------------------------------------
    // Kiểm tra cập nhật (bản portable)
    // ---------------------------------------------------------------------

    [Fact]
    public async Task UpdateChecker_ReportsNewerVersion()
    {
        var checker = new UpdateChecker("1.2.0", new FakeFeed
        {
            Result = new UpdateFeedResult(true, "1.3.0", "https://example/releases/1.3.0", "notes")
        });

        var result = await checker.CheckAsync();

        Assert.True(result.UpdateAvailable);
        Assert.Equal("1.3.0", result.LatestVersion);
        Assert.Equal("https://example/releases/1.3.0", result.ReleaseUrl);
    }

    [Fact]
    public async Task UpdateChecker_NoUpdateWhenSameOrOlder()
    {
        var same = new UpdateChecker("1.2.0", new FakeFeed { Result = new UpdateFeedResult(true, "1.2.0") });
        Assert.False((await same.CheckAsync()).UpdateAvailable);

        var older = new UpdateChecker("1.2.0", new FakeFeed { Result = new UpdateFeedResult(true, "v1.1.9") });
        Assert.False((await older.CheckAsync()).UpdateAvailable);
    }

    [Fact]
    public async Task UpdateChecker_FeedError_IsReportedNotThrown()
    {
        var checker = new UpdateChecker("1.2.0", new FakeFeed { Result = new UpdateFeedResult(false, Error: "offline") });

        var result = await checker.CheckAsync();

        Assert.False(result.UpdateAvailable);
        Assert.Equal("offline", result.Error);
    }

    [Theory]
    [InlineData("1.2.1", "1.2.0", true)]
    [InlineData("v1.3.0", "1.2.0", true)]
    [InlineData("2.0.0", "1.99.99", true)]
    [InlineData("1.2.0", "1.2.0", false)]
    [InlineData("1.1.0", "1.2.0", false)]
    [InlineData("không-phải-số", "1.2.0", false)]
    public void VersionComparer_ComparesSemantically(string candidate, string current, bool expected)
    {
        Assert.Equal(expected, VersionComparer.IsNewer(candidate, current));
    }

    private sealed class FakeFeed : IUpdateFeed
    {
        public UpdateFeedResult Result { get; set; } = new(true, "1.0.0");
        public Task<UpdateFeedResult> FetchAsync(CancellationToken ct = default) => Task.FromResult(Result);
    }

    // ---------------------------------------------------------------------
    // Tối ưu hiệu năng (NFR-1)
    // ---------------------------------------------------------------------

    [Fact]
    public void MediaFileTypes_SkipsFilesWithoutMetadata()
    {
        Assert.True(MediaFileTypes.MayHaveExif("jpg"));
        Assert.True(MediaFileTypes.MayHaveExif(".JPG"));
        Assert.False(MediaFileTypes.MayHaveExif("txt"));
        Assert.False(MediaFileTypes.MayHaveExif("pdf"));

        Assert.True(MediaFileTypes.MayHaveMediaDate("mp4"));
        Assert.False(MediaFileTypes.MayHaveMediaDate("docx"));
        Assert.True(MediaFileTypes.MayHaveAnyMetadata("png"));
        Assert.False(MediaFileTypes.MayHaveAnyMetadata("zip"));
    }

    [Fact]
    public void MetadataReaders_SkipNonMediaFilesWithoutTouchingDisk()
    {
        var file = new FileItem(@"C:\data\notes.txt", @"C:\data", "notes", "txt", false, 100,
            DateTime.UtcNow, DateTime.UtcNow, 0);

        Assert.Empty(new ExifMetadataReader().Read(file));
        Assert.Empty(new MediaMetadataReader().Read(file));
    }

    [Fact]
    public void BoundedCache_ClearsWhenCapacityExceeded()
    {
        var cache = new BoundedCache<string, int>(capacity: 4);

        for (int i = 0; i < 10; i++) cache.Set("k" + i, i);

        Assert.True(cache.Count <= 4, $"Cache còn {cache.Count} phần tử vượt dung lượng.");
    }

    [Fact]
    public void Scanner_MatchesGlobs_ForIncludeAndExclude()
    {
        Assert.True(FileScanner.MatchesGlobs(@"C:\a\IMG_1.jpg", "*.jpg", null));
        Assert.False(FileScanner.MatchesGlobs(@"C:\a\IMG_1.jpg", "*.png", null));
        Assert.False(FileScanner.MatchesGlobs(@"C:\a\tmp.jpg", "*.jpg", "tmp.*"));
        Assert.True(FileScanner.MatchesGlobs(@"C:\a\any.bin", null, null));
    }

    // ---------------------------------------------------------------------
    // Chia sẻ preset
    // ---------------------------------------------------------------------

    [Fact]
    public void PresetShare_RoundTripsThroughJson()
    {
        var fs = new FakeFileSystem();
        var store = new PresetStore(fs, "presets");

        var preset = new Preset
        {
            Name = "Chia sẻ",
            Description = "mô tả",
            Rules = new List<RuleDefinition>
            {
                new() { Type = "FindReplace", Params = new() { ["pattern"] = System.Text.Json.JsonSerializer.SerializeToElement("a") } }
            }
        };

        var json = store.ToJson(preset);
        var restored = store.FromJson(json);

        Assert.Equal("Chia sẻ", restored.Name);
        Assert.Single(restored.Rules);
        Assert.Equal("FindReplace", restored.Rules[0].Type);
    }

    // ---------------------------------------------------------------------
    // Đa ngôn ngữ: các file ngôn ngữ phải có cùng bộ khoá
    // ---------------------------------------------------------------------

    [Fact]
    public void LanguageResources_AllLanguagesHaveSameKeys()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "BFR.App", "Resources");
        dir = Path.GetFullPath(dir);

        var files = new[] { "Strings.vi.xaml", "Strings.en.xaml", "Strings.fr.xaml", "Strings.zh-Hans.xaml" };
        var keySets = new Dictionary<string, List<string>>();

        foreach (var file in files)
        {
            var path = Path.Combine(dir, file);
            Assert.True(File.Exists(path), $"Thiếu file ngôn ngữ: {file}");

            var doc = XDocument.Load(path);
            var keys = doc.Root!
                .Elements()
                .Where(e => e.Name.LocalName == "String")
                .Select(e => (string?)e.Attribute("Key") ?? string.Empty)
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToList();

            keySets[file] = keys;
        }

        var reference = keySets[files[0]];
        foreach (var file in files.Skip(1))
        {
            var missing = reference.Except(keySets[file]).ToList();
            var extra = keySets[file].Except(reference).ToList();

            Assert.True(missing.Count == 0, $"{file} thiếu khoá: {string.Join(", ", missing)}");
            Assert.True(extra.Count == 0, $"{file} có khoá lạ: {string.Join(", ", extra)}");
        }
    }
}
