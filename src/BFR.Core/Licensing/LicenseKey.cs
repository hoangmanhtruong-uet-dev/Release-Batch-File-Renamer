namespace BFR.Core.Licensing;

using System;
using System.Linq;
using System.Text;

/// <summary>
/// Mã kích hoạt Pro dạng `BFR-XXXXX-XXXXX-XXXXX-XXXXX-XXXXX`.
///
/// Bản cài từ Microsoft Store dùng Store IAP (mua đứt), còn bản portable có thể dùng
/// mã kích hoạt do nhà phát triển cấp. Mã gồm 20 ký tự payload + 5 ký tự kiểm tra
/// (checksum FNV-1a mã hoá base32) nên sai 1 ký tự sẽ bị phát hiện ngay.
/// </summary>
public static class LicenseKey
{
    /// <summary>Bảng chữ cái dùng cho mã (loại bỏ I/O/0/1 để tránh nhầm lẫn).</summary>
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Số nhóm payload (mỗi nhóm 5 ký tự).</summary>
    public const int PayloadGroups = 4;

    public const int GroupLength = 5;
    public const string Prefix = "BFR";

    /// <summary>Lý do mã bị từ chối (dùng làm khoá chuỗi thông báo trong UI).</summary>
    public enum KeyError
    {
        None = 0,
        Empty,
        BadFormat,
        BadChecksum
    }

    /// <summary>Chuẩn hoá: bỏ khoảng trắng, gạch ngang, chữ hoa/thường.</summary>
    public static string Normalize(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        var raw = key.Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .Trim()
            .ToUpperInvariant();

        if (raw.StartsWith(Prefix, StringComparison.Ordinal))
        {
            raw = raw[Prefix.Length..];
        }

        var builder = new StringBuilder(Prefix.Length + (PayloadGroups + 1) * (GroupLength + 1));
        builder.Append(Prefix);

        for (int i = 0; i < raw.Length; i++)
        {
            if (i % GroupLength == 0) builder.Append('-');
            builder.Append(raw[i]);
        }

        return builder.ToString();
    }

    /// <summary>Kiểm tra định dạng + checksum của mã.</summary>
    public static KeyError Validate(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return KeyError.Empty;

        var groups = Normalize(key).Split('-', StringSplitOptions.RemoveEmptyEntries);

        // BFR + 4 nhóm payload + 1 nhóm checksum.
        if (groups.Length != PayloadGroups + 2) return KeyError.BadFormat;
        if (!string.Equals(groups[0], Prefix, StringComparison.Ordinal)) return KeyError.BadFormat;

        foreach (var group in groups.Skip(1))
        {
            if (group.Length != GroupLength) return KeyError.BadFormat;
            if (group.Any(c => Alphabet.IndexOf(c) < 0)) return KeyError.BadFormat;
        }

        var payload = string.Concat(groups.Skip(1).Take(PayloadGroups));
        var checksum = groups[^1];
        return string.Equals(Checksum(payload), checksum, StringComparison.Ordinal)
            ? KeyError.None
            : KeyError.BadChecksum;
    }

    public static bool IsValid(string? key) => Validate(key) == KeyError.None;

    /// <summary>
    /// Sinh mã hợp lệ từ một chuỗi seed (dùng cho bản dev / test / cấp mã thật).
    /// </summary>
    public static string Generate(string seed)
    {
        var payload = new StringBuilder();
        var bytes = Encoding.UTF8.GetBytes(seed ?? string.Empty);

        for (int i = 0; i < PayloadGroups * GroupLength; i++)
        {
            var index = (bytes.Length == 0 ? 0 : bytes[i % bytes.Length]) + i * 7;
            payload.Append(Alphabet[Math.Abs(index) % Alphabet.Length]);
        }

        var text = payload.ToString();
        return $"{Prefix}-{Chunk(text, 0)}-{Chunk(text, 1)}-{Chunk(text, 2)}-{Chunk(text, 3)}-{Checksum(text)}";
    }

    private static string Chunk(string value, int index) =>
        value.Substring(index * GroupLength, GroupLength);

    private static string Checksum(string payload)
    {
        uint hash = 2166136261u;
        foreach (var c in payload)
        {
            hash = (hash ^ (uint)Alphabet.IndexOf(c)) * 16777619u;
        }

        var builder = new StringBuilder(GroupLength);
        for (int i = 0; i < GroupLength; i++)
        {
            builder.Insert(0, Alphabet[(int)(hash % Alphabet.Length)]);
            hash /= (uint)Alphabet.Length;
        }

        return builder.ToString();
    }

    /// <summary>Định dạng hiển thị cho người dùng (không phụ thuộc định dạng nhập).</summary>
    public static string ForDisplay(string? key) => Normalize(key);
}
