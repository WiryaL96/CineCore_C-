using System;
using System.Collections.Generic;

namespace CineCore.Services
{
    /// <summary>
    /// Simple service locator for DI without external frameworks.
    /// ponytail: upgrade to Microsoft.Extensions.DependencyInjection when testability needed.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T instance) where T : class
            => _services[typeof(T)] = instance;

        public static T Get<T>() where T : class
            => (T)_services[typeof(T)];
    }
}
