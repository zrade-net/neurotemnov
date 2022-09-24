using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NeuroTemnov.Phrases;

namespace NeuroTemnov.Bot;

public class DiscordBot : IBot
{
    private readonly string _token;
    private readonly IPhraseGenerator _phraseGenerator;
    private readonly ILogger _logger;
    private readonly DiscordSocketClient _client;

    public DiscordBot(
        string token,
        IPhraseGenerator phraseGenerator,
        ILogger logger
    )

    {
        _token = token;
        _phraseGenerator = phraseGenerator;
        _logger = logger;
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            MessageCacheSize = 50,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        });
        _client.MessageReceived += OnMessageReceived;
        _client.Log += Log;
    }

    private async Task OnMessageReceived(SocketMessage message)
    {
        bool isMentioned = message.MentionedUsers.Select(r => r.Id).Contains(_client.CurrentUser.Id);
        bool isMentionedInText = message.Content.Contains(_client.CurrentUser.Id.ToString());
        bool parentIsBot = message.Author.IsBot;
        ulong messageId = message.Id;
        ulong? parentMessageId = message.Reference is not null && message.Reference.MessageId.IsSpecified
            ? message.Reference.MessageId.Value
            : null;
        if (parentIsBot)
        {
            return;
        }

        MessageReference? messageReference = null;
        // We called bot by name
        // Trying to answer
        if (isMentioned)
        {
            messageReference = new MessageReference(
                isMentionedInText // we called bot by name 
                    ? parentMessageId ?? messageId // replying to parent
                    : messageId
            ); // if this is a reply to bot's message -- just replying to current)
        }
        else if (_phraseGenerator.MessageContainsTriggers(message.Content))
        {
            messageReference = new MessageReference(messageId);
        }

        string messageText = _phraseGenerator.GetReply();

        if (messageReference is not null)
        {
            await message.Channel.SendMessageAsync(
                messageText,
                messageReference: new MessageReference(messageId)
            );
        }

        _logger.LogInformation(
            "Received message: Mentioned: {IsMentioned}; MentionedInText: {MentionedInText}; Text: {Content}",
            isMentioned,
            isMentionedInText,
            message.Content);
        _logger.LogInformation("Replying with {Text}", messageText);
    }

    private Task Log(LogMessage message)
    {
        LogLevel logLevel = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Debug => LogLevel.Debug,
            LogSeverity.Verbose => LogLevel.Trace,
            _ => LogLevel.Trace
        };
        _logger.Log(
            logLevel,
            message.Exception,
            "{Source}: {Message}",
            message.Source,
            message.Message
        );
        return Task.CompletedTask;
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
        // Wait infinitely so your bot actually stays connected.
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
}