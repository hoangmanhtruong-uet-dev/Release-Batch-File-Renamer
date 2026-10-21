namespace BFR.Core;

using System.Collections.Concurrent;
using System.Collections.Generic;

/// <summary>
/// Bộ nhớ đệm có giới hạn số phần tử.
///
/// Dùng cho cache metadata (EXIF/media): trên lô 100.000 tệp, cache không giới hạn
/// sẽ chiếm hàng trăm MB. Khi vượt dung lượng, cache được xoá (hồi quy thế hệ)
/// để giữ mức sử dụng bộ nhớ ổn định.
/// </summary>
public sealed class BoundedCache<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _items = new();
    private readonly int _capacity;

    public BoundedCache(int capacity = 8192)
    {
        _capacity = capacity <= 0 ? 1024 : capacity;
    }

    public int Count => _items.Count;

    public int Capacity => _capacity;

    public bool TryGet(TKey key, out TValue value) => _items.TryGetValue(key, out value!);

    public void Set(TKey key, TValue value)
    {
        _items[key] = value;
        if (_items.Count > _capacity) _items.Clear();
    }

    public void Clear() => _items.Clear();
}
