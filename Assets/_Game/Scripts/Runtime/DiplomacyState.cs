using System.Collections.Generic;

namespace SpringAutumn.Runtime
{
    /// <summary>外交关系状态。存储国家两两之间的关系值（-100~100）。</summary>
    public class DiplomacyState
    {
        public const int MinRelation = -100;
        public const int MaxRelation = 100;

        private readonly Dictionary<string, int> _relations = new Dictionary<string, int>();

        /// <summary>生成与顺序无关的稳定键（A|B == B|A）。</summary>
        public static string PairKey(string a, string b)
        {
            return string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;
        }

        public int GetRelation(string a, string b)
        {
            return _relations.TryGetValue(PairKey(a, b), out int v) ? v : 0;
        }

        public void SetRelation(string a, string b, int value)
        {
            _relations[PairKey(a, b)] = Clamp(value);
        }

        public void ChangeRelation(string a, string b, int delta)
        {
            SetRelation(a, b, GetRelation(a, b) + delta);
        }

        /// <summary>导出全部关系（用于存档）。</summary>
        public IReadOnlyDictionary<string, int> All => _relations;

        private static int Clamp(int v)
        {
            if (v < MinRelation) return MinRelation;
            if (v > MaxRelation) return MaxRelation;
            return v;
        }
    }
}
