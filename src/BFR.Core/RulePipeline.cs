namespace BFR.Core;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Áp chuỗi IRule lên từng FileItem, sinh PreviewItem kèm vết trung gian (Rule Trace).
/// </summary>
public sealed class RulePipeline
{
    private readonly List<IRule> _rules = new();
    public IReadOnlyList<IRule> Rules => _rules.AsReadOnly();

    public void Add(IRule rule) => _rules.Add(rule);
    public bool Remove(IRule rule) => _rules.Remove(rule);
    public void Clear() => _rules.Clear();

    public PreviewResult Run(IReadOnlyList<FileItem> files, RuleContext ctx, System.Threading.CancellationToken ct = default)
    {
        var items = new List<PreviewItem>(files.Count);
        foreach (var f in files)
        {
            ct.ThrowIfCancellationRequested();
            var state = new RenameState(f.Name, f.Extension, f.LoadOrder, f.LoadOrder, new List<string>());
            foreach (var rule in _rules.Where(r => r.Enabled))
            {
                state = rule.Apply(f, state, ctx);
            }
            var finalName = $"{state.Name}{(string.IsNullOrEmpty(state.Extension) ? "" : "." + state.Extension)}";
            items.Add(new PreviewItem(f, $"{f.Name}{(string.IsNullOrEmpty(f.Extension) ? "" : "." + f.Extension)}", finalName, PreviewStatus.Ok, state.Trace));
        }
        return new PreviewResult(items, HasBlockingError: false);
    }
}
