using SpringAutumn.Runtime;

namespace SpringAutumn.Commands
{
    /// <summary>
    /// 玩家与 AI 共用的行为命令抽象。命令入队后于下一个 Tick 的"执行命令"阶段统一执行，
    /// 保证玩家与 AI 使用同一套规则（需求 6.1）。
    /// </summary>
    public abstract class GameCommand
    {
        /// <summary>发起命令的势力 Id。</summary>
        public string NationId;

        /// <summary>执行前合法性校验；非法命令将被丢弃且不修改世界。</summary>
        public virtual bool Validate(WorldRuntime world) => true;

        /// <summary>对世界状态执行本命令。</summary>
        public abstract void Execute(WorldRuntime world);
    }
}
