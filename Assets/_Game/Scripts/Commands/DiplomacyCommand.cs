using SpringAutumn.Core.Events;
using SpringAutumn.Runtime;

namespace SpringAutumn.Commands
{
    /// <summary>外交命令：调整两国关系，可用于宣战。</summary>
    public class DiplomacyCommand : GameCommand
    {
        public string TargetNationId;
        public int RelationDelta;
        public bool DeclareWar;

        private EventBus _eventBus;

        public DiplomacyCommand() { }

        public DiplomacyCommand(string nationId, string targetNationId, int relationDelta,
            bool declareWar = false, EventBus eventBus = null)
        {
            NationId = nationId;
            TargetNationId = targetNationId;
            RelationDelta = relationDelta;
            DeclareWar = declareWar;
            _eventBus = eventBus;
        }

        public override bool Validate(WorldRuntime world)
        {
            if (string.IsNullOrEmpty(NationId) || string.IsNullOrEmpty(TargetNationId))
                return false;
            if (NationId == TargetNationId)
                return false;
            return world.Nations.Contains(NationId) && world.Nations.Contains(TargetNationId);
        }

        public override void Execute(WorldRuntime world)
        {
            world.Diplomacy.ChangeRelation(NationId, TargetNationId, RelationDelta);

            if (DeclareWar)
            {
                world.Diplomacy.SetRelation(NationId, TargetNationId, -100);
                world.Nations.Get(NationId).WarStatus = WarStatus.War;
                world.Nations.Get(TargetNationId).WarStatus = WarStatus.War;
                _eventBus?.Publish(new WarDeclared
                {
                    AttackerNationId = NationId,
                    DefenderNationId = TargetNationId
                });
            }
        }
    }
}
