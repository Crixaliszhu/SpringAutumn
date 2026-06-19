using System.Collections.Generic;

namespace SpringAutumn.Runtime
{
    /// <summary>当前持续生效的世界事件（饥荒/丰收等）。V1.0 预留接口，按月递减时长。</summary>
    public class OngoingEvent
    {
        public string EventId;
        public string TargetId;   // 作用对象（如 RegionId / SettlementId），可空
        public int RemainingMonths;

        public OngoingEvent() { }

        public OngoingEvent(string eventId, string targetId, int remainingMonths)
        {
            EventId = eventId;
            TargetId = targetId;
            RemainingMonths = remainingMonths;
        }
    }

    /// <summary>世界事件状态容器。</summary>
    public class EventState
    {
        public List<OngoingEvent> Active = new List<OngoingEvent>();
    }
}
