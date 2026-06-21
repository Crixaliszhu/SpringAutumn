using SpringAutumn.Core.Events;

namespace SpringAutumn.Presentation.Input
{
    public struct SelectionChanged : IGameEvent
    {
        public string Id;
        public SelectionType Type;
    }
}
