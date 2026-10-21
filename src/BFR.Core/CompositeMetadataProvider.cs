namespace BFR.Core;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gộp nhiều nguồn metadata (EXIF + media) thành một nhà cung cấp duy nhất.
/// </summary>
public sealed class CompositeMetadataProvider : IMetadataProvider
{
    private readonly IReadOnlyList<IMetadataProvider> _providers;

    public CompositeMetadataProvider(params IMetadataProvider[] providers)
    {
        _providers = providers ?? Array.Empty<IMetadataProvider>();
    }

    public IReadOnlyDictionary<string, string> Read(FileItem file)
    {
        if (_providers.Count == 0) return new Dictionary<string, string>();

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in _providers)
        {
            var map = provider.Read(file);
            foreach (var pair in map)
            {
                if (!result.ContainsKey(pair.Key)) result[pair.Key] = pair.Value;
            }
        }

        return result;
    }
}
