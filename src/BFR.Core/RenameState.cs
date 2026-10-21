namespace BFR.Core;

using System.Collections.Generic;

/// <summary>
/// Trạng thái làm việc của một tệp khi đi qua chuỗi quy tắc.
/// </summary>
public sealed record RenameState(
    string Name,              // tên đang xử lý (không gồm đuôi)
    string Extension,         // đuôi đang xử lý
    int Index,                // chỉ số trong danh sách đã sắp xếp (bắt đầu từ 0)
    int GroupIndex,           // chỉ số trong nhóm (ví dụ theo thư mục)
    IReadOnlyList<string> Trace);   // vết sau từng quy tắc (Rule Trace)
