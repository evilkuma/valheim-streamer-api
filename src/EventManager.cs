using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ValheimStreamerApi
{
    public class EventManager
    {
        private readonly Dictionary<string, EventRegistration> _events = new();

        private class EventRegistration
        {
            public Delegate Handler { get; set; }
            public Type ReturnType { get; set; }
        }

        private void RegisterEvent(string eventName, Delegate handler, Type returnType)
        {
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentNullException(nameof(eventName));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _events[eventName] = new EventRegistration
            {
                Handler = handler,
                ReturnType = returnType
            };

            Log.LogInfo($"EventManager Register Event: {eventName}");
        }

        private Delegate GetHandler(string eventName)
        {
            return _events.TryGetValue(eventName, out var registration) ? registration.Handler : null;
        }

        private EventRegistration GetRegistration(string eventName)
        {
            return _events.TryGetValue(eventName, out var registration) ? registration : null;
        }

        public void Add(string name, Func<object, EventArgs, Task> handler)
        {
            RegisterEvent(name, handler, typeof(void));
        }

        public void Add<TResult>(string name, Func<object, EventArgs, Task<TResult>> handler)
        {
            RegisterEvent(name, handler, typeof(TResult));
        }

        public void Remove(string name)
        {
            if (!string.IsNullOrEmpty(name))
                _events.Remove(name);
        }

        public bool HasEvent(string name)
        {
            return _events.ContainsKey(name);
        }

        public async Task Dispatch(string eventName, object sender, EventArgs args)
        {
            var handler = GetHandler(eventName);
            if (handler == null) return;

            if (handler is Func<object, EventArgs, Task> func)
                await func(sender, args).ConfigureAwait(false);
            else if (handler is Func<object, EventArgs, Task<object>> funcWithResult)
                await funcWithResult(sender, args).ConfigureAwait(false);
            else
                throw new InvalidOperationException($"Неподдерживаемый тип обработчика для '{eventName}'");
        }

        public async Task<TResult> Dispatch<TResult>(string eventName, object sender, EventArgs args)
        {
            var handler = GetHandler(eventName);
            if (handler == null) return default;

            var data = GetRegistration(eventName);
            if (data?.ReturnType != typeof(TResult))
                throw new InvalidOperationException(
                    $"Тип возврата не соответствует. Ожидается: {data?.ReturnType.Name}, получен: {typeof(TResult).Name}");

            if (handler is Func<object, EventArgs, Task<TResult>> typedFunc)
                return await typedFunc(sender, args).ConfigureAwait(false);

            throw new InvalidOperationException($"Обработчик '{eventName}' не возвращает тип {typeof(TResult).Name}");
        }
    }
}
