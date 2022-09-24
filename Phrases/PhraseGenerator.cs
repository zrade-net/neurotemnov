using CircularBuffer;

namespace NeuroTemnov.Phrases;

public class PhraseGenerator : IPhraseGenerator
{
    private readonly IReadOnlyList<string> _triggers;
    private readonly IReadOnlyList<string> _replies;
    private readonly CircularBuffer<int> _buffer;
    private readonly Random _rng;

    public PhraseGenerator(
        IReadOnlyList<string> triggers,
        IReadOnlyList<string> replies,
        int bufferSize
    )
    {
        _triggers = triggers;
        _replies = replies;
        _buffer = new CircularBuffer<int>(Math.Min(bufferSize, _replies.Count / 2));
        _rng = new Random();
    }

    public bool MessageContainsTriggers(string message)
    {
        return _triggers.Any(s => message.Contains(s, StringComparison.InvariantCultureIgnoreCase));
    }

    public string GetReply()
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

        _buffer.PushBack(index);
        return _replies[index];
    }
}