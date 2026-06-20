using System.Collections.Generic;
using SpringAutumn.Config;
using SpringAutumn.Core.Events;
using SpringAutumn.Runtime;

namespace SpringAutumn.Systems
{
    /// <summary>
    /// 建造系统：每月递减建造队列，到期完成后效果从下月起生效（需求 6.2、6.4、6.5）。
    /// 在 Tick 顺序中执行，位于 EconomySystem 之后。
    /// </summary>
    public class ConstructionSystem : IGameSystem
    {
        private readonly ConfigDatabase _config;
        private readonly EventBus _eventBus;

        public ConstructionSystem(ConfigDatabase config, EventBus eventBus = null)
        {
            _config = config;
            _eventBus = eventBus;
        }

        public void Execute(WorldRuntime world)
        {
            foreach (var settlement in world.Settlements.GetAll())
            {
                ProcessConstruction(settlement);
            }
        }

        private void ProcessConstruction(SettlementState s)
        {
            var completed = new List<ConstructionTask>();

            foreach (var task in s.ConstructionQueue)
            {
                task.RemainingMonths--;

                if (task.RemainingMonths <= 0)
                {
                    completed.Add(task);
                }
            }

            // 移除已完成任务并添加建筑实例
            foreach (var task in completed)
            {
                s.ConstructionQueue.Remove(task);
                CompleteBuilding(s, task.BuildingId);
            }
        }

        private void CompleteBuilding(SettlementState s, string buildingId)
        {
            // 检查是否已有该建筑（升级）
            bool found = false;
            foreach (var b in s.Buildings)
            {
                if (b.BuildingId == buildingId)
                {
                    b.Level++;
                    found = true;
                    break;
                }
            }

            // 新建
            if (!found)
            {
                s.Buildings.Add(new BuildingInstance(buildingId, 1));
            }

            // 广播建筑完成事件
            _eventBus?.Publish(new BuildingFinishedEvent
            {
                SettlementId = s.Id,
                BuildingId = buildingId
            });

            GameLogger.Log($"[Construction] {s.Id} 建造完成: {buildingId}");
        }
    }

    /// <summary>建筑完成事件（过去式命名）。</summary>
    public class BuildingFinishedEvent : IGameEvent
    {
        public string SettlementId;
        public string BuildingId;
    }
}
