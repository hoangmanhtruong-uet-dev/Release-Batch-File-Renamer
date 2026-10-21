namespace BFR.Core;

/// <summary>
/// Giao diện trừu tượng hóa hệ thống tệp để kiểm thử (có bản thật và bản giả).
/// </summary>
public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    void EnsureDirectory(string path);
    void Move(string from, string to, bool overwrite);
    string[] GetFiles(string directory, string pattern, bool recursive);
    string[] GetDirectories(string directory, bool recursive);
    string ReadAllText(string path);
    void WriteAllText(string path, string content);
    void AppendAllText(string path, string content);
    void Delete(string path, bool recursive);
    long GetLength(string path);
    DateTime GetCreationTimeUtc(string path);
    DateTime GetLastWriteTimeUtc(string path);
}
