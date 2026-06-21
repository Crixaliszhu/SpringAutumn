using SpringAutumn.Runtime;

namespace SpringAutumn.Battle
{
    public class BattleResult
    {
        public BattleOutcome Outcome;
        public int AttackerLosses;
        public int DefenderLosses;
        public float PowerRatio;

        public bool AttackerWon => Outcome == BattleOutcome.AttackerWon;
    }
}
