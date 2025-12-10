using CommandCentre.Components;
using Microsoft.Extensions.Hosting;
using RazorConsole.Core;

var builder = Host.CreateDefaultBuilder(args)
    .UseRazorConsole<Dashboard>();

var host = builder.Build();

await host.RunAsync();