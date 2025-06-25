using System.Net.Http.Json;
using System.Text.Json;

namespace VwConnector;

public interface IVwRaw : IVwScope
{
    public async Task<JsonElement> GetJsonAsync(ConnectTokenResult token, string endpoint, CancellationToken cancelToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, endpoint, token, default);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<JsonElement>(cancelToken);
        return result;
    }

    public async Task<JsonElement> PostJsonAsync(ConnectTokenResult token, string endpoint, JsonElement args, CancellationToken cancelToken = default)
    {
        using var request = CreateJsonRequest(HttpMethod.Post, endpoint, token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<JsonElement>(cancelToken);
        return result;
    }
}
