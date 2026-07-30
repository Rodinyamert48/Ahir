using System.Net.Http.Json;
using System.Text.Json;
using Ahir.Core.Models;

namespace Ahir.SDK;

public sealed class AhirClient : IDisposable
{
    private readonly HttpClient _http;
    private string? _token;

    public AhirClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
    }

    public void SetToken(string token) { _token = token; }

    private void ApplyAuth(HttpRequestMessage request)
    {
        if (_token != null)
            request.Headers.Authorization = new("Bearer", _token);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        ApplyAuth(request);
        if (body != null) request.Content = JsonContent.Create(body);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<AhirResult<T>> SendResultAsync<T>(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        ApplyAuth(request);
        if (body != null) request.Content = JsonContent.Create(body);
        var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            var data = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return AhirResult<T>.Ok(data!);
        }
        try
        {
            var err = JsonSerializer.Deserialize<AhirResult<T>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return err ?? AhirResult<T>.Fail("ERROR", content);
        }
        catch { return AhirResult<T>.Fail("HTTP_ERROR", $"{response.StatusCode}: {content}"); }
    }

    public Task<AhirResult<AuthToken>> LoginAsync(string username, string password)
        => SendResultAsync<AuthToken>(HttpMethod.Post, "api/v1/auth/login", new { username, password });

    public Task<AhirResult<AuthToken>> RegisterAsync(string username, string password, string? email = null)
        => SendResultAsync<AuthToken>(HttpMethod.Post, "api/v1/auth/register", new { username, password, email });

    public Task<AhirResult<IReadOnlyList<DatabaseInfo>>> ListDatabasesAsync()
        => SendResultAsync<IReadOnlyList<DatabaseInfo>>(HttpMethod.Get, "api/v1/databases");

    public Task<AhirResult<DatabaseInfo>> CreateDatabaseAsync(string name)
        => SendResultAsync<DatabaseInfo>(HttpMethod.Post, "api/v1/databases", new { name });

    public Task<AhirResult<DatabaseInfo>> GetDatabaseAsync(string name)
        => SendResultAsync<DatabaseInfo>(HttpMethod.Get, $"api/v1/databases/{name}");

    public Task<AhirResult<bool>> DropDatabaseAsync(string name)
        => SendResultAsync<bool>(HttpMethod.Delete, $"api/v1/databases/{name}");

    public Task<AhirResult<PageResult<AhirRecord>>> QueryAsync(string database, string collection, QueryOptions options)
        => SendResultAsync<PageResult<AhirRecord>>(HttpMethod.Post, $"api/v1/databases/{database}/collections/{collection}/records/query", options);

    public Task<AhirResult<AhirRecord>> InsertAsync(string database, string collection, Dictionary<string, object?> fields)
        => SendResultAsync<AhirRecord>(HttpMethod.Post, $"api/v1/databases/{database}/collections/{collection}/records", fields);

    public Task<AhirResult<AhirRecord>> GetAsync(string database, string collection, string id)
        => SendResultAsync<AhirRecord>(HttpMethod.Get, $"api/v1/databases/{database}/collections/{collection}/records/{id}");

    public Task<AhirResult<AhirRecord>> UpdateAsync(string database, string collection, string id, Dictionary<string, object?> fields)
        => SendResultAsync<AhirRecord>(HttpMethod.Put, $"api/v1/databases/{database}/collections/{collection}/records/{id}", fields);

    public Task<AhirResult<bool>> DeleteAsync(string database, string collection, string id)
        => SendResultAsync<bool>(HttpMethod.Delete, $"api/v1/databases/{database}/collections/{collection}/records/{id}");

    public Task<AhirResult<long>> CountAsync(string database, string collection)
        => SendResultAsync<long>(HttpMethod.Get, $"api/v1/databases/{database}/collections/{collection}/count");

    public Task<AhirResult<BackupInfo>> CreateBackupAsync(string? database = null)
        => SendResultAsync<BackupInfo>(HttpMethod.Post, "api/v1/backup", new { database });

    public Task<AhirResult<IReadOnlyList<BackupInfo>>> ListBackupsAsync()
        => SendResultAsync<IReadOnlyList<BackupInfo>>(HttpMethod.Get, "api/v1/backup");

    public Task<AhirResult<bool>> RestoreBackupAsync(string id)
        => SendResultAsync<bool>(HttpMethod.Post, $"api/v1/backup/{id}/restore");

    public Task<AhirMetrics> GetMetricsAsync()
        => SendAsync<AhirMetrics>(HttpMethod.Get, "api/v1/metrics");

    public void Dispose() => _http.Dispose();
}
