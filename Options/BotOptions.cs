namespace NeuroTemnov.Options;

public class BotOptions
{
    public string Token { get; set; } = "";

    public List<string> Triggers { get; set; } = new();
    public List<string> Phrases { get; set; } = new();
}