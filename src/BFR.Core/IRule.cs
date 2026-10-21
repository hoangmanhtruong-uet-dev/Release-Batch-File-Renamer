namespace BFR.Core;

/// <summary>
/// Mọi quy tắc đổi tên là hàm thuần (pure): cùng đầu vào, cùng đầu ra;
/// không đọc đồng hồ hay tệp trực tiếp.
/// </summary>
public interface IRule
{
    string Id { get; }
    bool Enabled { get; }
    RenameState Apply(FileItem file, RenameState state, RuleContext ctx);
}
