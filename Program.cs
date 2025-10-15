#pragma warning disable SA1200 // Using directives should be placed correctly
using System.Runtime.InteropServices;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SampleCode;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddDiscordHost((config, _) =>
{
    config.SocketConfig = new DiscordSocketConfig
    {
        LogLevel = LogSeverity.Verbose,
        AlwaysDownloadUsers = true,
        MessageCacheSize = 1024,
        GatewayIntents = GatewayIntents.All,
    };

    config.Token = "";
});


builder.Services.AddHostedService<SampleCommandService>();

builder.Services.AddInteractionService((config, _) =>
{
    config.LogLevel = LogSeverity.Debug;
    config.UseCompiledLambda = true;
});

var host = builder.Build();
await host.RunAsync();