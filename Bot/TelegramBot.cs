using Microsoft.Extensions.Logging;
using NeuroTemnov.Phrases;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace NeuroTemnov.Bot;

public class TelegramBot : IBot
{
    private readonly IPhraseGenerator _phraseGenerator;
    private readonly ILogger _logger;
    private readonly TelegramBotClient _client;
    private User _me = null!;

    public TelegramBot(
        string token,
        IPhraseGenerator phraseGenerator,
        ILogger logger
    )
    {
        _phraseGenerator = phraseGenerator;
        _logger = logger;
        _client = new TelegramBotClient(token);
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
        };
        _client.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cancellationToken
        );
        _me = await _client.GetMeAsync(cancellationToken);
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromDays(1), cancellationToken);
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Telegram api error");
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken ct)
    {
        if (update.Type != UpdateType.Message)
        {
            return;
        }

        if (update.Message is null)
        {
            return;
        }

        if (update.Message.From?.IsBot != false)
        {
            return;
        }

        bool isMentioned = IsMentioned(update.Message);

        if (!isMentioned // is mentioned in message
            && update.Message.ReplyToMessage?.From?.Id != _me.Id // is reply to own phrase
            && !_phraseGenerator.MessageContainsTriggers(update.Message.Text ?? "") // triggered
            && update.Message.Chat.Type != ChatType.Private)
        {
            return;
        }

        int replyToMessage = isMentioned
            ? update.Message.ReplyToMessage?.MessageId ?? update.Message.MessageId
            : update.Message.MessageId;
        await Reply(update.Message.Chat.Id, replyToMessage, ct);
    }


    private bool IsMentioned(Message message)
    {
        if (message.Entities is null)
        {
            return false;
        }

        return message.Entities
            .Where(r => r.Type == MessageEntityType.Mention)
            .Select(m => message.Text?.Substring(m.Offset, m.Length) ?? "@")
            .Select(m => m[1..])
            .Any(m => _me.Username == m);
    }

    private async Task Reply(long chatId, int messageId, CancellationToken cancellationToken)
    {
        await _client.SendTextMessageAsync(
            chatId,
            _phraseGenerator.GetReply(),
            replyToMessageId: messageId,
            cancellationToken: cancellationToken
        );
    }
}