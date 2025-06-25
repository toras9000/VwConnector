using System.Net.Http.Json;

namespace VwConnector;

public interface IVwIdentity : IVwScope
{
    public async Task<ConnectTokenResult> ConnectTokenAsync<TToken>(TToken data, CancellationToken cancelToken = default) where TToken : ConnectTokenModel
    {
        using var request = CreateUrlEncodedRequest(HttpMethod.Post, "identity/connect/token", default, data);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<ConnectTokenResult>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<ClientCredentialsConnectTokenResult> ConnectTokenAsync(ClientCredentialsConnectTokenModel data, CancellationToken cancelToken = default)
    {
        using var request = CreateUrlEncodedRequest(HttpMethod.Post, "identity/connect/token", default, data);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<ClientCredentialsConnectTokenResult>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<PasswordConnectTokenResult> ConnectTokenAsync(PasswordConnectTokenModel data, CancellationToken cancelToken = default)
    {
        using var request = CreateUrlEncodedRequest(HttpMethod.Post, "identity/connect/token", default, data);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<PasswordConnectTokenResult>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<PreloginResult> PreloginAsync(PreloginArgs args, CancellationToken cancelToken = default)
    {
        using var request = CreateJsonRequest(HttpMethod.Post, "identity/accounts/prelogin", default, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<PreloginResult>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }
}
