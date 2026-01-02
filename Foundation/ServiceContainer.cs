using System;
using System.Collections.Generic;

namespace AetherGon.Foundation;

public class ServiceContainer : IDisposable
{
    private readonly Dictionary<Type, object> _services = new();

    // Registers a singleton instance
    public void Register<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    // Retrieves a registered service
    public T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }
        throw new InvalidOperationException($"Service {typeof(T).Name} is not registered.");
    }

    // Disposes all disposable services
    public void Dispose()
    {
        foreach (var service in _services.Values)
        {
            if (service is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _services.Clear();
    }
}
