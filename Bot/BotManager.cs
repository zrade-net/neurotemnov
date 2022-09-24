using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeuroTemnov.Options;
using NeuroTemnov.Phrases;

namespace NeuroTemnov.Bot;

public class BotManager : IDisposable
{
    private readonly ServiceProvider _provider;

    public BotManager()
    {
        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("bot.json", true)
            .AddJsonFile("bot.custom.json", true)
            .AddEnvironmentVariables();
        IConfigurationRoot configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddLogging(o => o.AddConsole().AddConfiguration(configuration));
        services.AddSingleton(configuration);
        services.Configure<BotSetOptions>(configuration.GetSection("Bots"));
        _provider = services.BuildServiceProvider();
    }

    public async Task Run()
    {
        IOptions<BotSetOptions> options = _provider.GetRequiredService<IOptions<BotSetOptions>>();
        ILoggerFactory loggingFactory = _provider.GetRequiredService<ILoggerFactory>();
        var bots = new List<IBot>();
        foreach ((string botName, BotOptions botOptions) in options.Value)
        {
            var phraseGenerator = new PhraseGenerator(
                botOptions.Triggers,
                botOptions.Phrases,
                10
            );
            if (!string.IsNullOrWhiteSpace(botOptions.DiscordToken))
            {
                bots.Add(new DiscordBot(
                    botOptions.DiscordToken,
                    phraseGenerator,
                    loggingFactory.CreateLogger(botName)
                ));
            }

            if (!string.IsNullOrWhiteSpace(botOptions.TelegramToken))
            {
                bots.Add(new TelegramBot(
                    botOptions.TelegramToken,
                    phraseGenerator,
                    loggingFactory.CreateLogger(botName)
                ));
            }
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