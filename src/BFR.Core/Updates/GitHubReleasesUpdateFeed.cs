namespace BFR.Core.Updates;

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Nguồn cập nhật mặc định: API "latest release" của GitHub (dùng cho bản portable).
///
/// Quyền riêng tư: chỉ gọi đúng URL do người dùng cấu hình, không gửi thông tin
/// máy, không gửi tên tệp; chỉ gửi User-Agent để GitHub nhận diện ứng dụng.
/// </summary>
public sealed class GitHubReleasesUpdateFeed : IUpdateFeed, IDisposable
{
    private readonly HttpClient _client;
    private readonly bool _ownsClient;
    private readonly Uri _releasesApiUri;

    public GitHubReleasesUpdateFeed(Uri releasesApiUri, HttpClient? client = null)
    {
        _releasesApiUri = releasesApiUri;
        _ownsClient = client is null;
        _client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        if (!string.IsNullOrWhiteSpace(_client.DefaultRequestHeaders.UserAgent.ToString()))
        {
            _client.DefaultRequestHeaders.UserAgent.Clear();
        }
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("BatchFileRenamer/1.2 (+https://localhost)");
    }

    public async Task<UpdateFeedResult> FetchAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _client.GetAsync(_releasesApiUri, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateFeedResult(false, Error: $"HTTP {(int)response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tag = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : null;
            var url = root.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() : null;
            var body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(tag))
            {
                return new UpdateFeedResult(false, Error: "Phản hồi không có tag_name.");
            }

            return new UpdateFeedResult(true, tag, url, body);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new UpdateFeedResult(false, Error: ex.Message);
        }
    }

    public void Dispose()
    {
        if (_ownsClient) _client.Dispose();
    }
}
