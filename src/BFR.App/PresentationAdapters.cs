namespace BFR.App;

using BFR.Presentation;
using System;
using System.Windows;

/// <summary>
/// Bộ chuyển chuỗi cho ViewModel: đọc trực tiếp từ resource 4 ngôn ngữ của WPF.
/// Nhờ vậy lớp trình bày (BFR.Presentation) không phụ thuộc WPF và vẫn test được.
/// </summary>
public sealed class LocStringProvider : IStringProvider
{
    public string T(string key) => Loc.T(key);

    public string F(string key, params object[] args) => Loc.F(key, args);

    public string L(string vi, string en) => Loc.L(vi, en);
}

/// <summary>Dispatcher WPF: mọi cập nhật danh sách chạy trên luồng giao diện.</summary>
public sealed class WpfUiDispatcher : IUiDispatcher
{
    public void Post(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            action();
            return;
        }

        dispatcher.BeginInvoke(new Action(action));
    }
}
