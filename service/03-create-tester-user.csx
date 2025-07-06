#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Playwright, 1.53.0"
#r "nuget: Lestaly.General, 0.100.0"
#r "nuget: Kokuban, 0.2.0"
#nullable enable
using System.Text.Json;
using Microsoft.Playwright;
using Kokuban;
using Lestaly;
using Lestaly.Cx;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Json;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    using var signal = new SignalCancellationPeriod();

    WriteLine("Detection compose service");
    var composeFile = ThisSource.RelativeFile("./compose.yml");
    var port = (await "docker".args("compose", "--file", composeFile, "port", "app", "80").silent().result().success().output(trim: true)).SkipToken(':');
    var service = new Uri($"http://localhost:{port}");

    WriteLine("Invite test user");
    const string TestAdminPass = "admin-pass";
    const string TestUser = "tester@myserver.home";
    const string TestPass = "tester-password";
    using var http = new HttpClient();
    var adminToken = await http.GetAdminTokenAsync(service, TestAdminPass, signal.Token);
    var testUser = await http.InviteAsync(service, adminToken, TestUser, signal.Token);

    WriteLine("Detection invite mail");
    var joinUri = default(Uri);
    var mailDir = ThisSource.RelativeDirectory("maildump");
    using (var breaker = signal.Token.CreateLink(TimeSpan.FromSeconds(30)))
    {
        var encUser = Uri.EscapeDataString(TestUser);
        while (true)
        {
            var lastMail = mailDir.GetFiles("*-text.txt").OrderByDescending(f => f.Name).FirstOrDefault();
            var joinLine = Try.Func(() => lastMail?.ReadAllLines().FirstOrDefault(l => l.StartsWith("Click here to join:") && l.Contains(encUser)), _ => default);
            if (joinLine != null) { joinUri = new Uri(joinLine.SkipToken(':').Trim().ToString()); break; }
            await Task.Delay(TimeSpan.FromMilliseconds(500), breaker.Token);
        }
    }

    WriteLine("Prepare playwright");
    var packageVer = typeof(Microsoft.Playwright.Program).Assembly.GetName()?.Version?.ToString(3) ?? "*";
    var packageDir = SpecialFolder.UserProfile().FindPathDirectory([".nuget", "packages", "Microsoft.Playwright", packageVer], MatchCasing.CaseInsensitive);
    Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", packageDir?.FullName);
    Microsoft.Playwright.Program.Main(["install", "chromium", "--with-deps"]);

    WriteLine("Register test user");
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, });
    var page = await browser.NewPageAsync();
    {
        var response = await page.GotoAsync(joinUri.AbsoluteUri) ?? throw new PavedMessageException("Cannot access register page");
        await page.Locator("input[id='input-password-form_new-password']").FillAsync(TestPass);
        await page.Locator("input[id='input-password-form_confirm-new-password']").FillAsync(TestPass);
        await page.Locator("button[type='submit']").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
    WriteLine(".. Completed");

});

public static async Task<string> GetAdminTokenAsync(this HttpClient self, Uri serivice, string password, CancellationToken cancelToken = default)
{
    using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(serivice, "admin"));
    message.Content = new FormUrlEncodedContent([KeyValuePair.Create("token", password)]);
    using var response = await self.SendAsync(message, cancelToken);
    await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancelToken);
    if (!response.Headers.TryGetValues("Set-Cookie", out var cookies)) throw new Exception("failed to get token");
    var token = cookies.SelectMany(cookie => cookie.Split(';'))
        .Select(entry => entry.Split('='))
        .Where(entry => entry.Length == 2 && entry[0].Trim().Equals("VW_ADMIN", StringComparison.OrdinalIgnoreCase))
        .Select(entry => entry[1])
        .FirstOrDefault();
    return token ?? throw new Exception("failed to get token");
}

public record VwUser(string id, string name, string email);

public static async Task<VwUser> InviteAsync(this HttpClient self, Uri serivice, string token, string email, CancellationToken cancelToken = default)
{
    using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(serivice, "admin/invite"));
    message.Content = JsonContent.Create(new { email, });
    message.Headers.Add("Cookie", [$"VW_ADMIN={token}"]);
    using var response = await self.SendAsync(message, cancelToken);
    var result = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<VwUser>(cancelToken) ?? throw new Exception("failed to request");
    return result;
}
