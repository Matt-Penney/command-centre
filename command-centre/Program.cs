using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RazorConsole.Core;
using CommandCentre.Components;

var builder = Host
    .CreateApplicationBuilder(args);

builder.UseRazorConsole<App>();

await builder
    .Build()
    .RunAsync();