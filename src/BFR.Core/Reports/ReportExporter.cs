namespace BFR.Core.Reports;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

public enum ReportFormat
{
    Csv,
    Json
}

/// <summary>
/// Một dòng báo cáo thay đổi tên (FR-C7).
/// </summary>
public sealed record RenameReportEntry
{
    /// <summary>Đường dẫn cũ.</summary>
    public string OldPath { get; init; } = string.Empty;

    /// <summary>Đường dẫn mới (null nếu bước thất bại).</summary>
    public string? NewPath { get; init; }

    /// <summary>Trạng thái: ok, failed, skipped, undone.</summary>
    public string Status { get; init; } = "ok";

    /// <summary>Thông điệp lỗi (nếu có).</summary>
    public string? Error { get; init; }

    /// <summary>Thời điểm thực hiện (UTC).</summary>
    public DateTime TimestampUtc { get; init; }

    /// <summary>True nếu bước này thực sự đổi tên thành công.</summary>
    public bool Succeeded => string.Equals(Status, "ok", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Xuất báo cáo thay đổi tên ra CSV/JSON để lưu hoặc kiểm toán (FR-C7, Pro).
/// </summary>
public static class ReportExporter
{
    public static string ToCsv(IEnumerable<RenameReportEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("old_path,new_path,status,error,timestamp_utc");

        foreach (var e in entries ?? Enumerable.Empty<RenameReportEntry>())
        {
            sb.AppendLine(string.Join(',',
                Csv(e.OldPath),
                Csv(e.NewPath ?? string.Empty),
                Csv(e.Status),
                Csv(e.Error ?? string.Empty),
                Csv(e.TimestampUtc.ToString("O", CultureInfo.InvariantCulture))));
        }

        return sb.ToString();
    }

    public static string ToJson(IEnumerable<RenameReportEntry> entries)
    {
        var list = (entries ?? Enumerable.Empty<RenameReportEntry>()).ToList();
        return JsonSerializer.Serialize(list, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    /// <summary>Ghi báo cáo ra tệp (UTF-8, không BOM).</summary>
    public static void Export(IEnumerable<RenameReportEntry> entries, string path, ReportFormat format)
    {
        var content = format == ReportFormat.Json ? ToJson(entries) : ToCsv(entries);
        var dir = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
        {
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
        return text;
    }
}
