using System.Threading.Channels;

namespace ReturnFlow.Api.Services;

/// <summary>In-memory queue of claim ids waiting for analysis. Replace with RabbitMQ or Redis streams when you scale out.</summary>
public class ClaimQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

    public void Enqueue(int claimId) => _channel.Writer.TryWrite(claimId);

    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}
