using System;
using System.Collections.Generic;

namespace SpringAutumn.Core.Events
{
    /// <summary>
    /// 轻量级事件总线（纯 C#，位于 Core）。内核系统发布事件，表现层订阅刷新。
    /// 支持泛型类型安全的发布/订阅/取消订阅（需求 11.1）。
    /// </summary>
    public class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        public void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) return;
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _handlers[type] = list;
            }
            list.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) return;
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var list))
            {
                list.Remove(handler);
            }
        }

        public void Publish<T>(T evt) where T : IGameEvent
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var list))
            {
                // 遍历副本避免回调中修改列表
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (i < list.Count && list[i] is Action<T> action)
                    {
                        action(evt);
                    }
                }
            }
        }

        /// <summary>清除所有订阅（用于场景卸载或测试清理）。</summary>
        public void Clear() => _handlers.Clear();
    }
}
