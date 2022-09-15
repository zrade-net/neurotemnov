﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NeuroTemnov.Options;

namespace NeuroTemnov;

public class DiscordBotManager : IDisposable
{
    private readonly ServiceProvider _provider;

    public DiscordBotManager()
    {
        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("bot.json", true)
            .AddJsonFile("bot.custom.json", true)
            .AddEnvironmentVariables();
        IConfigurationRoot configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.Configure<BotSetOptions>(configuration.GetSection("Bots"));
        _provider = services.BuildServiceProvider();
    }

    public async Task Run()
    {
        IOptions<BotSetOptions> options = _provider.GetRequiredService<IOptions<BotSetOptions>>();
        var bots = new List<DiscordBot>();
        foreach ((string botName, BotOptions botOptions) in options.Value)
        {
            bots.Add(new DiscordBot(botName, botOptions.Token,
                botOptions.Triggers,
                botOptions.Phrases));
        }

        var cs = new CancellationTokenSource();
        await Task.WhenAll(bots.Select(bot => bot.Run(cs.Token)));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _provider.Dispose();
    }
}