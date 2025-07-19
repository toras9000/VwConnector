#!/usr/bin/env dotnet-script
#r "nuget: VwConnector, 1.34.1-rev.6"
#r "nuget: Lestaly.General, 0.100.0"
#r "nuget: Kokuban, 0.2.0"
#nullable enable
using Kokuban;
using Lestaly;
using Lestaly.Cx;
using VwConnector.Agent;

return await Paved.ProceedAsync(async () =>
{
    var ServiceUrl = new Uri("http://localhost:8190");
    var TestUser = "tester@myserver.home";
    var TestPass = "tester-password";

    using var signal = new SignalCancellationPeriod();
    using var vaultwarden = await VaultwardenAgent.CreateAsync(ServiceUrl, new(TestUser, TestPass));

    var items = await vaultwarden.GetItemsAsync();
    foreach (var item in items)
    {
        var info = item.Deleted ? " (deleted)" : "";
        var color = item.Deleted ? Chalk.Gray : Chalk.White;
        if (item.OrgName == null)
        {
            WriteLine(color[$"- [{item.Type}] {item.Name}{info}"]);
        }
        else
        {
            WriteLine(color[$"- [{item.Type}] {item.Name}{info} : {item.OrgName}"]);
        }
    }
});
