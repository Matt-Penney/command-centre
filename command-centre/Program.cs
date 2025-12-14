using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RazorConsole.Core;
using CommandCentre.Components;
using CommandCentre.Services;
using CommandCentre.Data;

var builder = Host
    .CreateApplicationBuilder(args);

builder.UseRazorConsole<App>();

builder.Services.AddSingleton<RepoService>();
builder.Services.AddSingleton<UtilityService>();
builder.Services.AddSingleton<GitHubService>();
builder.Services.AddSingleton<CommandService>();

builder.Services.AddScoped<GlobalState>();

await builder
    .Build()
    .RunAsync();