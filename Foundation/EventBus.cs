using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AetherGon.Foundation;

public class EventBus
{
    private readonly ConcurrentDictionary<Type, List<object>> _subscribers = new();

    public void Publish<T>(T eventMessage)
    {
        var type = typeof(T);
        if (_subscribers.TryGetValue(type, out var handlers))
        {
            var currentHandlers = handlers.ToList();
            foreach (var handler in currentHandlers)
            {
                ((Action<T>)handler)(eventMessage);
            }
        }
    }

    public void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        _subscribers.AddOrUpdate(type,
            _ => new List<object> { handler },
            (_, list) => { lock (list) { list.Add(handler); } return list; });
    }

    public void Unsubscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (_subscribers.TryGetValue(type, out var list))
        {
            lock (list)
            {
                list.Remove(handler);
            }
        }
    }
}
