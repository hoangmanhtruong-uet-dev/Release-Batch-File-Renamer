namespace BFR.Core;

/// <summary>
/// Quy tắc Mẫu tên (FR-B10): dựng tên mới từ mẫu bằng token, thay thế hoàn toàn tên cũ.
/// </summary>
public sealed class TemplateRule : IRule
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public bool Enabled { get; set; } = true;

    public string Template { get; set; } = "{name}";

    public RenameState Apply(FileItem file, RenameState state, RuleContext ctx)
    {
        var trace = new List<string>(state.Trace) { $"{Id}: Template" };
        var errors = new List<string>();
        var result = new TokenEngine().Expand(Template, file, state, ctx, errors);
        foreach (var e in errors) trace.Add($"{Id}: {e}");
        return state with { Name = result, Trace = trace };
    }
}
