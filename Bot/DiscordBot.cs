using CircularBuffer;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace NeuroTemnov.Bot;

public class DiscordBot
{
    private readonly string _name;
    private readonly string _token;
    private readonly IReadOnlyList<string> _triggers;
    private readonly IReadOnlyList<string> _replies;
    private readonly ILogger _logger;
    private readonly DiscordSocketClient _client;
    private readonly Random _rng;
    private readonly CircularBuffer<int> _buffer;

    public DiscordBot(
        string name,
        string token,
        IReadOnlyList<string> triggers,
        IReadOnlyList<string> replies,
        int bufferSize,
        ILogger logger
    )

    {
        _name = name;
        _token = token;
        _triggers = triggers;
        _replies = replies;
        _logger = logger;
        _buffer = new CircularBuffer<int>(Math.Min(bufferSize, _replies.Count / 2));
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            MessageCacheSize = 50,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        });
        _client.MessageReceived += OnMessageReceived;
        _client.Log += Log;
        _rng = new Random();
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
        else if (_triggers.Any(s => message.Content.Contains(s, StringComparison.InvariantCultureIgnoreCase)))
        {
            messageReference = new MessageReference(messageId);
        }

        string messageText = RandomMessage();

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
        switch (message.Severity)
        {
            case LogSeverity.Critical:
            case LogSeverity.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case LogSeverity.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case LogSeverity.Info:
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case LogSeverity.Verbose:
            case LogSeverity.Debug:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                break;
        }

        Console.WriteLine(
            $"{DateTime.Now,-19} [{message.Severity,8}] [{_name} ] {message.Source}: {message.Message} {message.Exception}");
        Console.ResetColor();

        return Task.CompletedTask;
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
        // Wait infinitely so your bot actually stays connected.
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private string RandomMessage()
    {
        int index;
        while (true)
        {
            index = _rng.Next(0, _replies.Count);
            if (!_buffer.Contains(index))
            {
                break;
            }
        }

        _logger.LogError("Buffer is {Buffer}", _buffer.ToArray());
        _buffer.PushBack(index);
        return _replies[index];
    }
}