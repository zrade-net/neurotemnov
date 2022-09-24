namespace NeuroTemnov.Bot;

public interface IBot
{
    Task Run(CancellationToken cancellationToken);
}