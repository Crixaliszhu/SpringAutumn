using System.Collections.Generic;
using SpringAutumn.Commands;

namespace SpringAutumn.Core.Engine
{
    /// <summary>
    /// 命令队列。玩家与 AI 生成的命令入队，下一个 Tick 的执行阶段一次性取出执行。
    /// </summary>
    public class CommandQueue
    {
        private readonly List<GameCommand> _pending = new List<GameCommand>();

        public int Count => _pending.Count;

        public void Enqueue(GameCommand command)
        {
            if (command != null)
            {
                _pending.Add(command);
            }
        }

        /// <summary>取出当前所有待执行命令并清空队列。</summary>
        public List<GameCommand> DrainAll()
        {
            var drained = new List<GameCommand>(_pending);
            _pending.Clear();
            return drained;
        }

        public IReadOnlyList<GameCommand> Peek() => _pending;

        public void Clear() => _pending.Clear();
    }
}
