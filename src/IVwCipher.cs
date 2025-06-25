using System.Net.Http.Json;

namespace VwConnector;

public interface IVwCipher : IVwScope
{
    public async Task<CipherItemsResult> GetItemsAsync(ConnectTokenResult token, CancellationToken cancelToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/ciphers", token, default);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<CipherItemsResult>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<CipherItem> GetItemAsync(ConnectTokenResult token, string id, CancellationToken cancelToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/ciphers/{id}", token, default);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<CipherItem>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<CipherItem> CreateItemAsync(ConnectTokenResult token, CreateItemArgs args, CancellationToken cancelToken = default)
    {
        using var request = CreateJsonRequest(HttpMethod.Post, $"api/ciphers/create", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<CipherItem>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }
}
