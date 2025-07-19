using System.Net.Http.Json;

namespace VwConnector;

public interface IVwOrganization : IVwScope
{
    public async Task<OrgPublicKey> GetPublicKeyAsync(ConnectTokenResult token, string orgId, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        using var request = CreateRequest(HttpMethod.Get, $"api/organizations/{encOrgId}/public-key", token);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<OrgPublicKey>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<VwApiKey> GetApiKeyAsync(ConnectTokenResult token, string orgId, PasswordOrOtp args, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        using var request = CreateJsonRequest(HttpMethod.Post, $"api/organizations/{encOrgId}/api-key", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwApiKey>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<VwOrganizationProfile> GetDetailAsync(ConnectTokenResult token, string orgId, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        using var request = CreateRequest(HttpMethod.Get, $"api/organizations/{encOrgId}", token);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwOrganizationProfile>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<CreateOrgResult> CreateAsync(ConnectTokenResult token, CreateOrgArgs? args = default, CancellationToken cancelToken = default)
    {
        using var request = CreateJsonRequest(HttpMethod.Post, $"api/organizations", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<CreateOrgResult>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task AcceptInviteAsync(ConnectTokenResult token, string orgId, string memberId, AcceptInviteArgs args, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        var encMemberId = Uri.EscapeDataString(memberId);
        using var request = CreateJsonRequest(HttpMethod.Post, $"api/organizations/{encOrgId}/users/{encMemberId}/accept", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancelToken);
    }

    public async Task ConfirmMemberAsync(ConnectTokenResult token, string orgId, string memberId, ConfirmMemberArgs args, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        var encMemberId = Uri.EscapeDataString(memberId);
        using var request = CreateJsonRequest(HttpMethod.Post, $"api/organizations/{encOrgId}/users/{encMemberId}/confirm", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancelToken);
    }

    public async Task<OrgMembersResult> GetMembersAsync(ConnectTokenResult token, string orgId, OrgMembersArgs? args = default, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        using var request = CreateUrlEncodedRequest(HttpMethod.Get, $"api/organizations/{encOrgId}/users", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<OrgMembersResult>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<OrgMembersUser> GetMemberAsync(ConnectTokenResult token, string orgId, string memberId, OrgMembersArgs? args = default, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        var encMemberId = Uri.EscapeDataString(memberId);
        using var request = CreateRequest(HttpMethod.Get, $"api/organizations/{encOrgId}/users/{encMemberId}", token);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<OrgMembersUser>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task EditMemberAsync(ConnectTokenResult token, string orgId, string memberId, EditOrgMemberArgs? args = default, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        var encMemberId = Uri.EscapeDataString(memberId);
        using var request = CreateJsonRequest(HttpMethod.Post, $"api/organizations/{encOrgId}/users/{encMemberId}", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancelToken);
    }

    public async Task<OrgCollectionsResult> GetCollectionsAsync(ConnectTokenResult token, string orgId, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        using var request = CreateRequest(HttpMethod.Get, $"api/organizations/{encOrgId}/collections", token, default);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<OrgCollectionsResult>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }

    public async Task<OrgCollectionDetail> CreateCollectionAsync(ConnectTokenResult token, string orgId, CreateCollectionArgs? args, CancellationToken cancelToken = default)
    {
        var encOrgId = Uri.EscapeDataString(orgId);
        using var request = CreateJsonRequest(HttpMethod.Post, $"api/organizations/{encOrgId}/collections", token, args);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<OrgCollectionDetail>(cancelToken) ?? throw new Exception("failed to get result");
        return result;
    }
}