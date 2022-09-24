namespace NeuroTemnov.Options;

public class BotOptions
{
    public string DiscordToken { get; set; } = "";
    public string TelegramToken { get; set; } = "";

    public List<string> Triggers { get; set; } = new();
    public List<string> Phrases { get; set; } = new();
}