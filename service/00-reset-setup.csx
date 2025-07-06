#!/usr/bin/env dotnet-script
#r "nuget: Lestaly.General, 0.100.0"
#r "nuget: Kokuban, 0.2.0"
#nullable enable
using System.Threading;
using Kokuban;
using Lestaly;
using Lestaly.Cx;

return await Paved.ProceedAsync(async () =>
{
    await "dotnet".args("script", ThisSource.RelativeFile("01-containers-volume-delete.csx"), "--no-pause").echo();
    await "dotnet".args("script", ThisSource.RelativeFile("02-containers-restart.csx"), "--no-pause").echo();
    await "dotnet".args("script", ThisSource.RelativeFile("03-create-tester-user.csx"), "--no-pause").echo();
    await "dotnet".args("script", ThisSource.RelativeFile("@show-service.csx"), "--no-pause").echo();
});
