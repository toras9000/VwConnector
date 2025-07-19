#!/usr/bin/env dotnet-script
#r "nuget: VwConnector, 1.34.1-rev.6"
#r "nuget: Lestaly.General, 0.100.0"
#r "nuget: Kokuban, 0.2.0"
#nullable enable
using System.Web;
using Kokuban;
using Lestaly;
using Lestaly.Cx;
using VwConnector;

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
    using var vaultwarden = new VaultwardenConnector(service);
    var adminToken = await vaultwarden.Admin.GetTokenAsync(TestAdminPass);
    var testUser = await vaultwarden.Admin.InviteAsync(adminToken, TestUser, signal.Token);

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

    WriteLine("Register test user");
    var joinQuery = HttpUtility.ParseQueryString(joinUri.AbsoluteUri.SkipToken('?').ToString());
    var orgUserId = joinQuery["organizationUserId"] ?? "";
    var inviteToken = joinQuery["token"] ?? "";
    await vaultwarden.Account.RegisterUserInviteAsync(new(TestUser, TestPass), orgUserId, inviteToken, signal.Token);
    WriteLine(".. Completed");

});
