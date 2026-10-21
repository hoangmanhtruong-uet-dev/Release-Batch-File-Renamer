namespace BFR.Core.Licensing;

using System;
using System.IO;
using System.Text.Json;

/// <summary>
/// Lưu/đọc trạng thái giấy phép cục bộ với ghi nguyên tử (tệp .bak).
/// Không chứa dữ liệu người dùng ngoài mã sản phẩm và mốc thời gian.
/// </summary>
public sealed class LicenseStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly IFileSystem _fs;
    private readonly string _path;

    public LicenseStore(IFileSystem fs, string? path = null)
    {
        _fs = fs;
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BatchFileRenamer", "license.json");
    }

    public string FilePath => _path;

    public LicenseRecord Load()
    {
        try
        {
            if (!_fs.FileExists(_path)) return new LicenseRecord();
            var json = _fs.ReadAllText(_path);
            return JsonSerializer.Deserialize<LicenseRecord>(json, JsonOptions) ?? new LicenseRecord();
        }
        catch (JsonException)
        {
            return new LicenseRecord();
        }
        catch (IOException)
        {
            return new LicenseRecord();
        }
    }

    public void Save(LicenseRecord record)
    {
        var dir = System.IO.Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) _fs.EnsureDirectory(dir);

        var temp = _path + ".tmp";
        var backup = _path + ".bak";
        _fs.WriteAllText(temp, JsonSerializer.Serialize(record, JsonOptions));
        if (_fs.FileExists(_path)) _fs.Move(_path, backup, overwrite: true);
        _fs.Move(temp, _path, overwrite: true);
    }
}
