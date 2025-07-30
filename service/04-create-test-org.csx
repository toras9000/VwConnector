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
using VwConnector.Agent;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    using var signal = new SignalCancellationPeriod();

    WriteLine("Detection compose service");
    var composeFile = ThisSource.RelativeFile("./compose.yml");
    var port = (await "docker".args("compose", "--file", composeFile, "port", "app", "80").silent().result().success().output(trim: true)).SkipToken(':');
    var service = new Uri($"http://localhost:{port}");

    WriteLine("Login test user");
    const string TestUser = "tester@myserver.home";
    const string TestPass = "tester-password";
    using var vaultwarden = await VaultwardenAgent.CreateAsync(service, new(TestUser, TestPass));

    WriteLine("Create test org");
    var TestOrg = "TestOrg";
    if (vaultwarden.Profile.organizations.Any(o => o.name == TestOrg))
    {
        WriteLine(".. Already exists");
    }
    await vaultwarden.Affect.CreateOrganizationAsync(TestOrg, "DefaultCollection", signal.Token);
    WriteLine(".. Completed");

});
