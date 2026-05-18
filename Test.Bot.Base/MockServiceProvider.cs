using Moq;
using System;
using System.Collections.Generic;

namespace Test.Bot.Base
{
    public class Mock : IServiceProvider
    {
        private readonly Dictionary<Type, object> m_mapping = new();

        public object? GetService(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (!m_mapping.TryGetValue(serviceType, out var ret))
            {
                // No service yet, create a strict-behaving mock as a placeholder
                var mockType = typeof(Mock<>).MakeGenericType(serviceType);
                var constr = mockType.GetConstructor(new[] { typeof(MockBehavior) });
                Moq.Mock m = (Moq.Mock)constr!.Invoke(new object?[] { MockBehavior.Strict });
                ret = m.Object;
                m_mapping.Add(serviceType, ret);
            }
            return ret;
        }

        public void RegisterService<T>(T service) where T : class
        {
            m_mapping[typeof(T)] = service;
        }
    }

    public class ServiceProvider : IServiceProvider
    {
        private readonly IServiceProvider? m_parent;
        private readonly Dictionary<Type, object> m_services = new();

        public ServiceProvider()
        {
        }

        public ServiceProvider(IServiceProvider parent)
        {
            m_parent = parent;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (m_services.TryGetValue(serviceType, out var service))
            {
                return service;
            }

            return m_parent?.GetService(serviceType);
        }

        public void AddService<T>(T service) where T : class
        {
            m_services[typeof(T)] = service;
        }
    }
}
