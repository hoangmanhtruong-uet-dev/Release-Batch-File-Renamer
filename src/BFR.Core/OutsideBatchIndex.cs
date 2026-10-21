namespace BFR.Core;

using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Dựng chỉ mục tên tệp/thư mục đang tồn tại ngoài lô, theo từng thư mục,
/// để <see cref="ConflictDetector"/> phát hiện va chạm (QA-07).
/// </summary>
public static class OutsideBatchIndex
{
    public static IReadOnlyDictionary<string, IReadOnlySet<string>> Build(
        IReadOnlyList<FileItem> batch,
        IFileSystem fs)
    {
        var result = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
        var batchPaths = new HashSet<string>(batch.Select(f => NormalizePath(f.FullPath)), StringComparer.Ordinal);

        foreach (var directory in batch.Select(f => f.Directory).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var path in fs.GetFiles(directory, "*", false))
                {
                    if (!batchPaths.Contains(NormalizePath(path))) names.Add(Path.GetFileName(path));
                }
                foreach (var path in fs.GetDirectories(directory, false))
                {
                    if (!batchPaths.Contains(NormalizePath(path))) names.Add(Path.GetFileName(path));
                }
            }
            catch
            {
                // Thư mục không truy cập được: bỏ qua, không chặn preview.
            }

            result[ConflictDetector.NormalizeDirectory(directory)] = names;
        }

        return result;
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/').TrimEnd('/').ToLowerInvariant();
}
