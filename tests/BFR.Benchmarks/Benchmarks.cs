namespace BFR.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BFR.Core;

[MemoryDiagnoser]
public class PipelineBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int FileCount;

    private List<FileItem> _files = null!;
    private RulePipeline _pipeline = null!;
    private RuleContext _ctx = null!;

    [GlobalSetup]
    public void Setup()
    {
        _files = new List<FileItem>(FileCount);
        for (int i = 0; i < FileCount; i++)
        {
            _files.Add(new FileItem($@"/root/file{i}.txt", "/root", $"file{i}", "txt", false, 100 + i, DateTime.UtcNow, DateTime.UtcNow, i));
        }
        _pipeline = new RulePipeline();
        _pipeline.Add(new FindReplaceRule { Pattern = " ", Replacement = "_", Scope = FindReplaceRule.ApplyScope.Name });
        _pipeline.Add(new NumberingRule { Start = 1, Step = 1, Width = 3, Position = NumberingRule.NumberPosition.Start });
        _ctx = new RuleContext(DateTime.UtcNow, "bench-seed");
    }

    [Benchmark]
    public PreviewResult RunPipeline() => _pipeline.Run(_files, _ctx);
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<PipelineBenchmarks>();
    }
}
