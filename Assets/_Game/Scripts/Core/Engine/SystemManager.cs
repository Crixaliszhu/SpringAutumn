using System.Collections.Generic;
using SpringAutumn.Runtime;
using SpringAutumn.Systems;

namespace SpringAutumn.Core.Engine
{
    /// <summary>
    /// 维护所有 IGameSystem 并按注册顺序逐一执行（需求 4.2）。
    /// 执行顺序固定，不支持运行时重排。
    /// </summary>
    public class SystemManager
    {
        private readonly List<IGameSystem> _systems = new List<IGameSystem>();

        public int Count => _systems.Count;

        /// <summary>按注册顺序追加系统。</summary>
        public void Register(IGameSystem system)
        {
            if (system != null)
            {
                _systems.Add(system);
            }
        }

        /// <summary>按注册顺序对 WorldRuntime 执行所有系统。</summary>
        public void ExecuteTick(WorldRuntime world)
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].Execute(world);
            }
        }

        /// <summary>获取已注册系统的只读列表（用于测试断言顺序）。</summary>
        public IReadOnlyList<IGameSystem> Systems => _systems;
    }
}
