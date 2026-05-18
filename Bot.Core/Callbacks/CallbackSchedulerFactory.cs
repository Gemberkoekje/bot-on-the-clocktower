using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core.Callbacks
{
    public class CallbackSchedulerFactory : ICallbackSchedulerFactory
    {
        private readonly IDateTime m_dateTime;
        private readonly ITask m_task;

        public CallbackSchedulerFactory(IDateTime dateTime, ITask task)
        {
            m_dateTime = dateTime;
            m_task = task;
        }

        public ICallbackScheduler<TKey> CreateScheduler<TKey>(Func<TKey, Task> callback, TimeSpan checkPeriod) where TKey : notnull
        {
            return new CallbackScheduler<TKey>(m_dateTime, m_task, callback, checkPeriod);
        }

        public ICallbackScheduler CreateScheduler(Func<Task> callback, TimeSpan checkPeriod)
        {
            return new CallbackScheduler(m_dateTime, m_task, callback, checkPeriod);
        }
    }
}
