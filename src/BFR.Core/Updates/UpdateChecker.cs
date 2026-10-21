namespace BFR.Core.Updates;

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Kết quả kiểm tra phiên bản mới (tự động cập nhật bản portable — v1.2).
/// </summary>
public sealed record UpdateCheckResult(
    bool UpdateAvailable,
    string CurrentVersion,
    string LatestVersion,
    string? ReleaseUrl = null,
    string? Notes = null,
    string? Error = null)
{
    public static UpdateCheckResult Failed(string currentVersion, string error) =>
        new(false, currentVersion, currentVersion, null, null, error);
}

/// <summary>Kết quả tải thông tin phiên bản mới từ nguồn cập nhật.</summary>
public sealed record UpdateFeedResult(
    bool Success,
    string? LatestVersion = null,
    string? ReleaseUrl = null,
    string? Notes = null,
    string? Error = null);

/// <summary>Nguồn cung cấp thông tin phiên bản mới (mặc định: GitHub Releases).</summary>
public interface IUpdateFeed
{
    Task<UpdateFeedResult> FetchAsync(CancellationToken ct = default);
}

/// <summary>
/// Kiểm tra bản cập nhật. Hoàn toàn opt-in: chỉ chạy khi người dùng bật trong Cài đặt.
/// </summary>
public sealed class UpdateChecker
{
    private readonly string _currentVersion;
    private readonly IUpdateFeed _feed;

    public UpdateChecker(string currentVersion, IUpdateFeed feed)
    {
        _currentVersion = currentVersion;
        _feed = feed;
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default)
    {
        UpdateFeedResult feed;
        try
        {
            feed = await _feed.FetchAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return UpdateCheckResult.Failed(_currentVersion, ex.Message);
        }

        if (!feed.Success || string.IsNullOrWhiteSpace(feed.LatestVersion))
        {
            return UpdateCheckResult.Failed(_currentVersion, feed.Error ?? "Không đọc được thông tin phiên bản.");
        }

        var available = VersionComparer.IsNewer(feed.LatestVersion!, _currentVersion);
        return new UpdateCheckResult(
            available,
            _currentVersion,
            feed.LatestVersion!,
            feed.ReleaseUrl,
            feed.Notes);
    }
}

/// <summary>So sánh phiên bản dạng "1.2.3" hoặc "v1.2.3-beta".</summary>
public static class VersionComparer
{
    public static bool IsNewer(string candidate, string current)
    {
        if (!TryParse(candidate, out var c) || !TryParse(current, out var cur)) return false;
        return Compare(c, cur) > 0;
    }

    public static bool TryParse(string? version, out VersionTriplet triplet)
    {
        triplet = default;
        if (string.IsNullOrWhiteSpace(version)) return false;

        var text = version.Trim();
        if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase)) text = text[1..];

        // Bỏ phần hậu tố pre-release (-beta.1, +build)
        var cut = text.IndexOfAny(new[] { '-', '+' });
        if (cut > 0) text = text[..cut];

        var parts = text.Split('.');
        if (parts.Length == 0) return false;

        int Parse(string s, int fallback) =>
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;

        var major = Parse(parts[0], 0);
        var minor = parts.Length > 1 ? Parse(parts[1], 0) : 0;
        var patch = parts.Length > 2 ? Parse(parts[2], 0) : 0;

        triplet = new VersionTriplet(major, minor, patch);
        return true;
    }

    public static int Compare(VersionTriplet a, VersionTriplet b)
    {
        if (a.Major != b.Major) return a.Major.CompareTo(b.Major);
        if (a.Minor != b.Minor) return a.Minor.CompareTo(b.Minor);
        return a.Patch.CompareTo(b.Patch);
    }

    public readonly record struct VersionTriplet(int Major, int Minor, int Patch);
}
