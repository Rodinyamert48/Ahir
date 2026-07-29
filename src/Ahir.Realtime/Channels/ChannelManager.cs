using System.Collections.Concurrent;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;

namespace Ahir.Realtime.Channels;

public sealed class ChannelManager
{
    private readonly ConcurrentDictionary<string, Channel> _channels = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _userPresence = new();

    public Channel GetOrCreate(string name)
    {
        return _channels.GetOrAdd(name, _ => new Channel(name));
    }

    public bool Remove(string name)
    {
        return _channels.TryRemove(name, out _);
    }

    public void AddUser(string channel, string userId)
    {
        _userPresence.AddOrUpdate(channel,
            _ => new HashSet<string> { userId },
            (_, set) => { set.Add(userId); return set; });
    }

    public void RemoveUser(string channel, string userId)
    {
        if (_userPresence.TryGetValue(channel, out var users))
        {
            users.Remove(userId);
            if (users.Count == 0)
                _userPresence.TryRemove(channel, out _);
        }
    }

    public IReadOnlySet<string> GetChannelUsers(string channel)
    {
        return _userPresence.TryGetValue(channel, out var users)
            ? users
            : new HashSet<string>();
    }

    public IReadOnlyList<string> GetChannels() => _channels.Keys.ToList();
    public int ChannelCount => _channels.Count;
}

public sealed class Channel
{
    public string Name { get; }
    private readonly ConcurrentBag<ChannelSubscriber> _subscribers = new();

    public Channel(string name)
    {
        Name = name;
    }

    public void Subscribe(ChannelSubscriber subscriber)
    {
        _subscribers.Add(subscriber);
    }

    public IEnumerable<ChannelSubscriber> GetSubscribers() => _subscribers;
}

public sealed class ChannelSubscriber
{
    public string Id { get; init; } = string.Empty;
    public string? UserId { get; init; }
    public Func<RealtimeMessage, Task>? Handler { get; init; }
}
