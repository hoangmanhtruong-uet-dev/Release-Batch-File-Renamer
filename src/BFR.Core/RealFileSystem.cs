namespace BFR.Core;

using System.IO;

/// <summary>
/// Triển khai IFileSystem thật trên Windows/.NET.
/// </summary>
public sealed class RealFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public void EnsureDirectory(string path) => Directory.CreateDirectory(path);
    public void Move(string from, string to, bool overwrite) => File.Move(from, to, overwrite);
    public string[] GetFiles(string directory, string pattern, bool recursive) =>
        Directory.Exists(directory)
            ? Directory.GetFiles(directory, pattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            : Array.Empty<string>();

    public string[] GetDirectories(string directory, bool recursive) =>
        Directory.Exists(directory)
            ? Directory.GetDirectories(directory, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            : Array.Empty<string>();
    public string ReadAllText(string path) => File.ReadAllText(path);
    public void WriteAllText(string path, string content) => File.WriteAllText(path, content);
    public void AppendAllText(string path, string content) => File.AppendAllText(path, content);
    public void Delete(string path, bool recursive)
    {
        if (Directory.Exists(path)) Directory.Delete(path, recursive);
        else if (File.Exists(path)) File.Delete(path);
        else throw new FileNotFoundException("Tệp không tồn tại", path);
    }
    public long GetLength(string path) => new FileInfo(path).Length;
    public DateTime GetCreationTimeUtc(string path) => File.GetCreationTimeUtc(path);
    public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);
}
