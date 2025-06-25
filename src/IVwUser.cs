using System.Net.Http.Json;

namespace VwConnector;

public interface IVwUser : IVwScope
{
    public async Task<VwUserProfile> GetProfileAsync(ConnectTokenResult token, CancellationToken cancelToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/accounts/profile", token);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwUserProfile>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<VwApiKey> GetApiKeyAsync(ConnectTokenResult token, PasswordOrOtp args, CancellationToken cancelToken = default)
    {
        using var request = CreateJsonRequest(HttpMethod.Post, $"api/accounts/api-key", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwApiKey>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<UserPublicKey> GetPublicKeyAsync(ConnectTokenResult token, string userId, CancellationToken cancelToken = default)
    {
        var encUserId = Uri.EscapeDataString(userId);
        using var request = CreateRequest(HttpMethod.Get, $"api/users/{encUserId}/public-key", token);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<UserPublicKey>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<GetFoldersResult> GetFoldersAsync(ConnectTokenResult token, CancellationToken cancelToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/folders", token, default);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<GetFoldersResult>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<VwFolder> CreateFolderAsync(ConnectTokenResult token, CreateFolderArgs args, CancellationToken cancelToken = default)
    {
        using var request = CreateJsonRequest(HttpMethod.Post, $"api/folders", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwFolder>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }
}