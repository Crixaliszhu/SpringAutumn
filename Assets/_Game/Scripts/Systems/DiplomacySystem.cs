using SpringAutumn.Runtime;

namespace SpringAutumn.Systems
{
    /// <summary>维护外交关系档位与国家战争状态。</summary>
    public class DiplomacySystem : IGameSystem
    {
        public void Execute(WorldRuntime world)
        {
            foreach (var a in world.Nations.GetAll())
            {
                bool atWar = false;
                foreach (var b in world.Nations.GetAll())
                {
                    if (a.Id == b.Id)
                        continue;
                    if (world.Diplomacy.GetStatus(a.Id, b.Id) == RelationStatus.War)
                    {
                        atWar = true;
                        break;
                    }
                }
                a.WarStatus = atWar ? WarStatus.War : WarStatus.Peace;
            }
        }
    }
}
