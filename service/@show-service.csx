#!/usr/bin/env dotnet-script
#r "nuget: Lestaly.General, 0.100.0"
#r "nuget: Kokuban, 0.2.0"
#nullable enable
using System.Threading;
using Kokuban;
using Lestaly;
using Lestaly.Cx;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    var composeFile = ThisSource.RelativeFile("./compose.yml");
    var port = (await "docker".args("compose", "--file", composeFile, "port", "app", "80").silent().result().success().output(trim: true)).SkipToken(':');
    var service = $"http://localhost:{port}";
    WriteLine();
    WriteLine($" {Poster.Link[$"{service}", /* */ $"Vaultwarden       - {service}"]}");
    WriteLine($" {Poster.Link[$"{service}/admin", $"Vaultwarden admin - {service}/admin"]}");
    WriteLine();
});
