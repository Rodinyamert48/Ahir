using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;
using Ahir.Realtime.Channels;

namespace Ahir.Realtime;

public sealed class RealtimeEngine : IRealtimeEngine
{
    private readonly ChannelManager _channelManager = new();
    private readonly ConcurrentDictionary<string, ChannelSubscriber> _subscribers = new();

    public async Task<bool> PublishAsync(string channel, string eventType, object? data, CancellationToken cancellationToken = default)
    {
        var ch = _channelManager.GetOrCreate(channel);
        var message = new RealtimeMessage
        {
            Channel = channel,
            EventType = eventType,
            Data = data,
            Timestamp = DateTime.UtcNow
        };

        foreach (var subscriber in ch.GetSubscribers())
        {
            try
            {
                if (subscriber.Handler != null)
                    await subscriber.Handler(message);
            }
            catch
            {
                // Log but never throw in publish
            }
        }

        return true;
    }

    public async Task SubscribeAsync(string channel, Func<RealtimeMessage, Task> handler, CancellationToken cancellationToken = default)
    {
        var subscriber = new ChannelSubscriber
        {
            Id = Guid.NewGuid().ToString(),
            Handler = handler
        };

        _subscribers[subscriber.Id] = subscriber;
        _channelManager.GetOrCreate(channel).Subscribe(subscriber);
        await Task.CompletedTask;
    }

    public async Task UnsubscribeAsync(string channel, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public async Task BroadcastAsync(string eventType, object? data, CancellationToken cancellationToken = default)
    {
        foreach (var channel in _channelManager.GetChannels())
        {
            await PublishAsync(channel, eventType, data, cancellationToken);
        }
    }

    public async IAsyncEnumerable<RealtimeMessage> StreamAsync(string channel, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var queue = System.Threading.Channels.Channel.CreateBounded<RealtimeMessage>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        await SubscribeAsync(channel, async msg =>
        {
            await queue.Writer.WriteAsync(msg, cancellationToken);
        }, cancellationToken);

        await foreach (var message in queue.Reader.ReadAllAsync(cancellationToken))
        {
            yield return message;
        }
    }
}
