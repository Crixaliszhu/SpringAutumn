using System.Collections.Generic;
using SpringAutumn.Config;
using SpringAutumn.Core.Events;
using SpringAutumn.Runtime;

namespace SpringAutumn.Systems
{
    /// <summary>
    /// 征兵系统：每月递减训练队列，到期完成后士兵入守军（需求 6.3）。
    /// 新兵完成当月不可参战（由 BattleSystem 检查标记）。
    /// </summary>
    public class RecruitSystem : IGameSystem
    {
        private readonly ConfigDatabase _config;
        private readonly EventBus _eventBus;

        public RecruitSystem(ConfigDatabase config, EventBus eventBus = null)
        {
            _config = config;
            _eventBus = eventBus;
        }

        public void Execute(WorldRuntime world)
        {
            foreach (var settlement in world.Settlements.GetAll())
            {
                ProcessRecruit(settlement);
            }
        }

        private void ProcessRecruit(SettlementState s)
        {
            var completed = new List<RecruitTask>();

            foreach (var task in s.RecruitQueue)
            {
                task.RemainingMonths--;

                if (task.RemainingMonths <= 0)
                {
                    completed.Add(task);
                }
            }

            // 移除已完成任务，士兵入守军
            foreach (var task in completed)
            {
                s.RecruitQueue.Remove(task);
                s.Garrison += task.Count;

                // 广播征兵完成事件
                _eventBus?.Publish(new RecruitFinishedEvent
                {
                    SettlementId = s.Id,
                    Count = task.Count
                });

                GameLogger.Log($"[Recruit] {s.Id} 征兵完成: +{task.Count} 士兵");
            }
        }
    }

    /// <summary>征兵完成事件（过去式命名）。</summary>
    public class RecruitFinishedEvent : IGameEvent
    {
        public string SettlementId;
        public int Count;
    }
}
