namespace VwConnector;

public interface IVwPublic : IVwScope
{
    public async Task ImportOrgMembersAsync(ConnectTokenResult token, ImportOrgArgs data, CancellationToken cancelToken = default)
    {
        using var request = CreateJsonRequest(HttpMethod.Post, "api/public/organization/import", token, data);
        using var response = await this.Http.SendAsync(request, cancelToken);
        await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancelToken);
    }
}