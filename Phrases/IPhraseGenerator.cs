namespace NeuroTemnov.Phrases;

public interface IPhraseGenerator
{
    bool MessageContainsTriggers(string message);
    string GetReply();
}