using System.Net.Http.Json;

namespace VwConnector;

public interface IVwAdmin : IVwScope
{
    public async Task<AdminToken> GetTokenAsync(string password, CancellationToken cancelToken = default)
    {
        using var request = CreateUrlEncodedRequest(HttpMethod.Post, "admin", default, new { token = password, });
        using var response = await this.Http.SendAsync(request, cancelToken);
        await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancelToken);
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies)) throw new Exception("failed to get token");
        var token = default(AdminToken);
        foreach (var cookie in cookies)
        {
            var scan = cookie.AsSpan();
            while (!scan.IsEmpty)
            {
                var entry = scan.TakeSkipToken(';', out scan);
                var key = entry.TakeSkipToken('=', out var value);
                if (key.Trim().Equals("VW_ADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    token = new(value.Trim().ToString());
                    break;
                }
            }
            if (token != null) break;
        }
        return token ?? throw new Exception("failed to get token");
    }

    public async Task<VwUserProfile> InviteAsync(AdminToken token, string email, CancellationToken cancelToken = default)
    {
        using var request = CreateJsonRequest(HttpMethod.Post, "admin/invite", default, new { email, });
        request.Headers.Add("Cookie", [$"VW_ADMIN={token.token}"]);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwUserProfile>(cancelToken) ?? throw new Exception("failed to request");
        return result;
    }

    public async Task<VwUserEx[]> UsersAsync(AdminToken token, CancellationToken cancelToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "admin/users", default, default);
        request.Headers.Add("Cookie", [$"VW_ADMIN={token.token}"]);
        using var response = await this.Http.SendAsync(request, cancelToken);
        var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwUserEx[]>(cancelToken) ?? throw new Exception("failed to request");
        return result;
    }
}