namespace BFR.Core.WatchFolder;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

/// <summary>
/// Kết quả xử lý một lô tệp trong Watch Folder.
/// </summary>
public sealed class WatchFolderResult
{
    /// <summary>Số tệp được xét.</summary>
    public int Candidates { get; set; }

    /// <summary>Số tệp đã đổi tên thành công.</summary>
    public int Renamed { get; set; }

    /// <summary>Số tệp không thay đổi tên (preset không tác dụng).</summary>
    public int Unchanged { get; set; }

    /// <summary>Số tệp bị bỏ qua (lỗi/xung đột/không khớp bộ lọc).</summary>
    public int Skipped { get; set; }

    /// <summary>Số bước thất bại khi thực thi.</summary>
    public int Failed { get; set; }

    /// <summary>Chi tiết lỗi theo tệp.</summary>
    public List<(string Path, string Error)> Errors { get; } = new();

    /// <summary>Các đường dẫn MỚI sau khi đổi tên (dùng để chống xử lý lặp).</summary>
    public List<string> RenamedPaths { get; } = new();

    /// <summary>Mã lô (journal) để có thể hoàn tác.</summary>
    public string? JournalId { get; set; }

    /// <summary>Đường dẫn tệp journal.</summary>
    public string? JournalPath { get; set; }

    /// <summary>True nếu không có lỗi chặn.</summary>
    public bool Succeeded => Failed == 0;
}

/// <summary>
/// Áp dụng preset cho một lô tệp mới trong Watch Folder.
/// Dùng chung pipeline với giao diện/CLI nên hành vi giống hệt (FR-D4).
/// </summary>
public sealed class WatchFolderProcessor
{
    private readonly IFileSystem _fs;
    private readonly string _journalDir;
    private readonly Func<DateTime> _clock;

    public WatchFolderProcessor(IFileSystem fs, string? journalDir = null, Func<DateTime>? clock = null)
    {
        _fs = fs;
        _clock = clock ?? (() => DateTime.UtcNow);
        _journalDir = journalDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BatchFileRenamer", "journal");
    }

    public WatchFolderResult Process(
        IReadOnlyList<string> paths,
        Preset preset,
        WatchFolderOptions options,
        CancellationToken ct = default,
        IMetadataProvider? metadataProvider = null)
    {
        var result = new WatchFolderResult();
        if (paths.Count == 0) return result;

        var files = new List<FileItem>();
        int order = 0;
        foreach (var path in paths)
        {
            ct.ThrowIfCancellationRequested();

            if (!FileScanner.MatchesGlobs(path, options.IncludeGlob, options.ExcludeGlob)) continue;
            if (!_fs.FileExists(path)) continue;

            try
            {
                files.Add(FileScanner.FromFile(_fs, path, order++));
            }
            catch
            {
                // Tệp không truy cập được: bỏ qua.
            }
        }

        result.Candidates = files.Count;
        if (files.Count == 0) return result;

        var pipeline = new PresetCompiler().Compile(preset);
        var ctx = new RuleContext(_clock(), Guid.NewGuid().ToString("N"));
        var outside = OutsideBatchIndex.Build(files, _fs);
        var preview = new PreviewEngine(pipeline, metadataProvider: metadataProvider)
            .Run(files, ctx, null, ct, outside);

        var pairs = new List<(string From, string To)>();
        foreach (var item in preview.Items)
        {
            ct.ThrowIfCancellationRequested();

            if (item.Status == PreviewStatus.Ok && !string.Equals(item.NewName, item.OldName, StringComparison.Ordinal))
            {
                pairs.Add((item.File.FullPath, Path.Combine(item.File.Directory, item.NewName)));
            }
            else if (item.Status == PreviewStatus.Unchanged)
            {
                result.Unchanged++;
            }
            else
            {
                result.Skipped++;
                result.Errors.Add((item.File.FullPath, item.Detail ?? item.Status.ToString()));
            }
        }

        if (pairs.Count == 0 || options.DryRun) return result;

        var steps = new RenamePlanner().Plan(pairs);
        var execution = new RenameExecutor(_fs, _journalDir).Execute(steps, "1.2.0", ct);

        result.JournalId = execution.JournalId;
        result.JournalPath = execution.JournalPath;
        result.Renamed = execution.Succeeded;
        result.Failed = execution.Failed;
        result.RenamedPaths.AddRange(pairs.Select(p => p.To));
        foreach (var (step, error) in execution.Errors)
        {
            result.Errors.Add((step, error));
        }

        return result;
    }
}
