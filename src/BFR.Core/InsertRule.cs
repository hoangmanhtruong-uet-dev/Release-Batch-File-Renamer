namespace BFR.Core;

/// <summary>
/// Quy tắc Chèn (FR-B6): chèn văn bản/token ở đầu, cuối hoặc vị trí N.
/// </summary>
public sealed class InsertRule : IRule
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public bool Enabled { get; set; } = true;

    public string Text { get; set; } = string.Empty;
    public InsertPosition Position { get; set; } = InsertPosition.Start;
    public int Index { get; set; } = 0;               // dùng khi Position = AtIndex
    public bool CountFromEnd { get; set; } = false;   // đếm vị trí từ cuối

    public RenameState Apply(FileItem file, RenameState state, RuleContext ctx)
    {
        var trace = new List<string>(state.Trace) { $"{Id}: Insert" };
        var text = new TokenEngine().Expand(Text, file, state, ctx);

        var result = Position switch
        {
            InsertPosition.Start => text + state.Name,
            InsertPosition.End => state.Name + text,
            InsertPosition.AtIndex => InsertAt(state.Name, text, Index, CountFromEnd),
            _ => state.Name
        };

        return state with { Name = result, Trace = trace };
    }

    private static string InsertAt(string name, string text, int index, bool fromEnd)
    {
        var pos = fromEnd ? Math.Max(0, name.Length - index) : Math.Clamp(index, 0, name.Length);
        return name.Insert(pos, text);
    }

    public enum InsertPosition { Start, End, AtIndex }
}
