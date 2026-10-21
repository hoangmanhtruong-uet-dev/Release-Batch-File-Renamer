namespace BFR.Presentation;

using System.Collections.ObjectModel;
using System.Globalization;

using BFR.Core;

/// <summary>Một mẫu Regex dựng sẵn trong "Thư viện mẫu".</summary>
public sealed class RegexSample
{
    public RegexSample(string name, string pattern, string replacement, string testInput)
    {
        Name = name;
        Pattern = pattern;
        Replacement = replacement;
        TestInput = testInput;
    }

    /// <summary>Tên mẫu (đã địa phương hóa).</summary>
    public string Name { get; }

    /// <summary>Mẫu biểu thức.</summary>
    public string Pattern { get; }

    /// <summary>Chuỗi thay thế.</summary>
    public string Replacement { get; }

    /// <summary>Chuỗi thử mặc định.</summary>
    public string TestInput { get; }
}

/// <summary>Một nhóm bắt trong kết quả Regex.</summary>
public sealed class RegexGroupViewModel
{
    public RegexGroupViewModel(int index, string value)
    {
        Index = index;
        Value = value;
    }

    /// <summary>Số thứ tự nhóm (0 = cả đoạn khớp).</summary>
    public int Index { get; }

    /// <summary>Giá trị của nhóm.</summary>
    public string Value { get; }
}

/// <summary>Một đoạn khớp hiển thị trong danh sách kết quả.</summary>
public sealed class RegexMatchViewModel
{
    public RegexMatchViewModel(string value, IReadOnlyList<string> groups)
    {
        Value = value;
        Groups = groups
            .Select((g, i) => new RegexGroupViewModel(i, g))
            .ToArray();
    }

    /// <summary>Đoạn khớp.</summary>
    public string Value { get; }

    /// <summary>Các nhóm bắt (nhóm 0 là cả đoạn khớp).</summary>
    public IReadOnlyList<RegexGroupViewModel> Groups { get; }
}

/// <summary>
/// ViewModel cho "Trình thử Regex" (spec 7.3).
/// Tách khỏi WPF để unit test được; logic khớp nằm ở <see cref="RegexProbe"/> (BFR.Core).
/// </summary>
public sealed class RegexTesterViewModel : ObservableObject
{
    private readonly IStringProvider _strings;
    private readonly IUiDispatcher _dispatcher;
    private readonly Action<string, string, bool, bool>? _applyToAll;

    private string _pattern = string.Empty;
    private string _replacement = string.Empty;
    private string _input = string.Empty;
    private bool _ignoreCase;
    private string _output = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private string? _selectedSampleName;
    private string _statusText = string.Empty;

    public RegexTesterViewModel(
        IStringProvider strings,
        IUiDispatcher dispatcher,
        Action<string, string, bool, bool>? applyToAll = null)
    {
        _strings = strings;
        _dispatcher = dispatcher;
        _applyToAll = applyToAll;
        Samples = new ObservableCollection<RegexSample>(BuildSamples(strings));

        // Mở sẵn mẫu đầu tiên để người dùng có ví dụ làm mốc (như bản mô tả trong spec).
        SelectedSampleName = Samples.FirstOrDefault()?.Name;
    }

    /// <summary>Thư viện mẫu Regex.</summary>
    public ObservableCollection<RegexSample> Samples { get; }

    public string Title => _strings.T("RegexTesterTitle");

    public string PatternLabel => _strings.T("RegexTesterPattern");

    public string ReplacementLabel => _strings.T("RegexTesterReplacement");

    public string InputLabel => _strings.T("RegexTesterInput");

    public string IgnoreCaseLabel => _strings.T("RegexTesterIgnoreCase");

    public string ResultLabel => _strings.T("RegexTesterResult");

    public string UseSelectedLabel => _strings.T("RegexTesterUseSelected");

    public string ApplyAllLabel => _strings.T("RegexTesterApplyAll");

    public string HintText => _strings.T("RegexTesterHint");

    /// <summary>Thông báo khi không áp dụng được (thiếu quy tắc Tìm và thay).</summary>
    public string ApplyFailedMessage => _strings.T("RegexTesterNeedFindReplace");

    /// <summary>Mẫu biểu thức.</summary>
    public string Pattern
    {
        get => _pattern;
        set
        {
            if (Set(ref _pattern, value)) Recalculate();
        }
    }

    /// <summary>Chuỗi thay thế.</summary>
    public string Replacement
    {
        get => _replacement;
        set
        {
            if (Set(ref _replacement, value)) Recalculate();
        }
    }

    /// <summary>Chuỗi thử.</summary>
    public string Input
    {
        get => _input;
        set
        {
            if (Set(ref _input, value)) Recalculate();
        }
    }

    /// <summary>Bỏ qua phân biệt hoa/thường.</summary>
    public bool IgnoreCase
    {
        get => _ignoreCase;
        set
        {
            if (Set(ref _ignoreCase, value)) Recalculate();
        }
    }

    /// <summary>Chuỗi kết quả sau khi thay.</summary>
    public string Output
    {
        get => _output;
        private set => Set(ref _output, value);
    }

    /// <summary>Thông báo lỗi (rỗng nếu không lỗi).</summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        private set => Set(ref _errorMessage, value);
    }

    /// <summary>True nếu mẫu biểu thức lỗi.</summary>
    public bool HasError
    {
        get => _hasError;
        private set => Set(ref _hasError, value);
    }

    /// <summary>Mô tả trạng thái (khớp / không khớp / lỗi).</summary>
    public string StatusText
    {
        get => _statusText;
        private set => Set(ref _statusText, value);
    }

    /// <summary>Danh sách các đoạn khớp.</summary>
    public ObservableCollection<RegexMatchViewModel> Matches { get; } = new();

    /// <summary>Mẫu đang chọn trong thư viện mẫu.</summary>
    public string? SelectedSampleName
    {
        get => _selectedSampleName;
        set
        {
            if (!Set(ref _selectedSampleName, value)) return;
            ApplySelectedSample();
        }
    }

    /// <summary>Điền chuỗi thử bằng tên tệp đang chọn trong bảng xem trước.</summary>
    public void UseSelectedName(string name)
    {
        Input = name;
    }

    /// <summary>Áp dụng kết quả vào quy tắc Tìm và thay của preset.</summary>
    public bool ApplyToAllFiles()
    {
        if (_applyToAll is null) return false;
        if (string.IsNullOrWhiteSpace(_pattern)) return false;
        if (HasError) return false;

        _applyToAll(_pattern, _replacement, _ignoreCase, true);
        return true;
    }

    /// <summary>Tính lại kết quả (gọi khi người dùng gõ).</summary>
    public void Recalculate()
    {
        var result = RegexProbe.Evaluate(_pattern, _replacement, _input, _ignoreCase);

        _dispatcher.Post(() =>
        {
            if (!result.IsValid)
            {
                HasError = !string.IsNullOrEmpty(result.Error);
                ErrorMessage = HasError
                    ? _strings.F("RegexTesterError", result.Error ?? string.Empty)
                    : string.Empty;
                Output = _input;
                StatusText = _strings.T("RegexTesterNoMatch");
                Matches.Clear();
                return;
            }

            HasError = false;
            ErrorMessage = string.Empty;
            Output = result.Output;
            StatusText = result.HasMatch
                ? _strings.F("RegexTesterMatch", result.Matches.Count.ToString(CultureInfo.InvariantCulture))
                : _strings.T("RegexTesterNoMatch");

            Matches.Clear();
            foreach (var match in result.Matches)
            {
                Matches.Add(new RegexMatchViewModel(match.Value, match.Groups));
            }
        });
    }

    private void ApplySelectedSample()
    {
        var sample = Samples.FirstOrDefault(s =>
            string.Equals(s.Name, _selectedSampleName, StringComparison.Ordinal));
        if (sample is null) return;

        Pattern = sample.Pattern;
        Replacement = sample.Replacement;
        Input = sample.TestInput;
    }

    private static IEnumerable<RegexSample> BuildSamples(IStringProvider s)
    {
        yield return new RegexSample(
            s.T("RegexSampleDatePrefix"),
            @"^(?<date>\d{8})_(?<rest>.+)$",
            "${rest}_${date}",
            "20240501_server.log");

        yield return new RegexSample(
            s.T("RegexSampleSpaces"),
            @"\s+",
            "_",
            "my file  name.txt");

        yield return new RegexSample(
            s.T("RegexSampleExtension"),
            @"\.(?<ext>[^.]+)$",
            ".${ext}.bak",
            "report.pdf");

        yield return new RegexSample(
            s.T("RegexSampleDigits"),
            @"\d+",
            "#",
            "order 2024 item 15");

        yield return new RegexSample(
            s.T("RegexSampleParentheses"),
            @"^\((?<tag>[a-z]+)\)\s*(?<name>.+)$",
            "${tag}_${name}",
            "(draft) my notes.txt");
    }
}
