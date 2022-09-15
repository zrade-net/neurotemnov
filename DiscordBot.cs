using Discord;
using Discord.WebSocket;

namespace NeuroTemnov;

public class DiscordBot
{
    private readonly string _name;
    private readonly string _token;
    private readonly IReadOnlyList<string> _triggers;
    private readonly IReadOnlyList<string> _replies;
    private readonly DiscordSocketClient _client;
    private readonly Random _rng;

    public DiscordBot(
        string name,
        string token,
        IReadOnlyList<string> triggers,
        IReadOnlyList<string> replies
    )

    {
        _name = name;
        _token = token;
        _triggers = triggers;
        _replies = replies;
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
        Console.WriteLine("Z1");
        bool parentIsBot = message.Author.IsBot;
        if (parentIsBot)
        {
            return;
        }

        string reply = RandomMessage();

        if (isMentioned)
        {
            bool mentionedInText = message.Content.Contains(_client.CurrentUser.Id.ToString());
            ulong? messageId;
            if (mentionedInText)
            {
                if (message.Reference != null)
                {
                    messageId = message.Reference.MessageId.IsSpecified
                        ? message.Reference.MessageId.Value
                        : message.Id;
                }
                else
                {
                    messageId = message.Id;
                }
            }
            else // reply
            {
                messageId = message.Id;
            }

            await message.Channel.SendMessageAsync(
                reply,
                messageReference: new MessageReference(messageId: messageId)
            );
        }
        else if (_triggers.Any(s => message.Content.Contains(s, StringComparison.InvariantCultureIgnoreCase)))
        {
            await message.Channel.SendMessageAsync(
                reply,
                messageReference: new MessageReference(messageId: message.Id)
            );
        }
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
        return _replies[_rng.Next(0, _replies.Count)];
    }
}