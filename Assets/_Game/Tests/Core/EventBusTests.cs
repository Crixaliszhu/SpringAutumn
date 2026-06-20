using NUnit.Framework;
using SpringAutumn.Core.Engine;
using SpringAutumn.Core.Events;
using SpringAutumn.Runtime;

namespace SpringAutumn.Tests.Core
{
    /// <summary>EventBus 发布订阅与 GameEngine MonthChanged 事件测试（需求 11.1、11.2）。</summary>
    public class EventBusTests
    {
        [Test]
        public void Subscribe_Publish_ReceivesEvent()
        {
            var bus = new EventBus();
            MonthChanged received = default;
            bus.Subscribe<MonthChanged>(e => received = e);

            bus.Publish(new MonthChanged { Year = 3, Month = 7 });

            Assert.AreEqual(3, received.Year);
            Assert.AreEqual(7, received.Month);
        }

        [Test]
        public void Unsubscribe_NoLongerReceives()
        {
            var bus = new EventBus();
            int count = 0;
            System.Action<MonthChanged> handler = _ => count++;
            bus.Subscribe(handler);
            bus.Publish(new MonthChanged());
            Assert.AreEqual(1, count);

            bus.Unsubscribe(handler);
            bus.Publish(new MonthChanged());
            Assert.AreEqual(1, count, "Unsubscribed handler should not fire");
        }

        [Test]
        public void DifferentEventTypes_DoNotInterfere()
        {
            var bus = new EventBus();
            bool monthFired = false;
            bool warFired = false;
            bus.Subscribe<MonthChanged>(_ => monthFired = true);
            bus.Subscribe<WarDeclared>(_ => warFired = true);

            bus.Publish(new MonthChanged { Year = 1, Month = 1 });

            Assert.IsTrue(monthFired);
            Assert.IsFalse(warFired, "WarDeclared should not fire for MonthChanged publish");
        }

        [Test]
        public void GameEngine_NextMonth_PublishesMonthChanged()
        {
            var engine = new GameEngine();
            engine.Initialize(new WorldRuntime(), new SystemManager());

            MonthChanged received = default;
            engine.Events.Subscribe<MonthChanged>(e => received = e);

            engine.NextMonth(); // (1,1) -> (1,2)

            Assert.AreEqual(1, received.Year);
            Assert.AreEqual(2, received.Month);
        }

        [Test]
        public void EventPayload_RegionCaptured_CarriesCorrectData()
        {
            var bus = new EventBus();
            RegionCaptured captured = default;
            bus.Subscribe<RegionCaptured>(e => captured = e);

            bus.Publish(new RegionCaptured
            {
                RegionId = "QIN_R01",
                OldOwnerId = "QIN",
                NewOwnerId = "PLAYER"
            });

            Assert.AreEqual("QIN_R01", captured.RegionId);
            Assert.AreEqual("QIN", captured.OldOwnerId);
            Assert.AreEqual("PLAYER", captured.NewOwnerId);
        }
    }
}
