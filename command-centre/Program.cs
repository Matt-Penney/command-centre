using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RazorConsole.Core;
using CommandCentre.Components;
using CommandCentre.Services;

var builder = Host
    .CreateApplicationBuilder(args);

builder.UseRazorConsole<App>();

builder.Services.AddSingleton<RepoService>();
builder.Services.AddSingleton<UtilityService>();

await builder
    .Build()
    .RunAsync();