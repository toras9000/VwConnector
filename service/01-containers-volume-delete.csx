#!/usr/bin/env dotnet-script
#r "nuget: Lestaly, 0.84.0"
#r "nuget: Kokuban, 0.2.0"
#nullable enable
using System.Threading;
using Kokuban;
using Lestaly;
using Lestaly.Cx;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    WriteLine(Chalk.Green["Delete containers & volumes."]);
    await "docker".args("compose", "--file", ThisSource.RelativeFile("compose.yml"), "down", "--remove-orphans", "--volumes");
    ThisSource.RelativeDirectory("maildump").DeleteRecurse();
});
