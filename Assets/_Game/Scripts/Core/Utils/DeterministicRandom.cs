namespace SpringAutumn.Core.Utils
{
    /// <summary>
    /// 可确定性复现的随机数发生器（xorshift128）。战斗与 AI 统一通过它取随机，
    /// 其内部状态可被存档/读档，保证相同种子 + 相同操作序列结果一致（需求 8.7、非功能 3）。
    /// </summary>
    public sealed class DeterministicRandom
    {
        private uint _x, _y, _z, _w;

        public DeterministicRandom(uint seed)
        {
            Reseed(seed);
        }

        /// <summary>用于读档时恢复完整内部状态。</summary>
        public DeterministicRandom(uint x, uint y, uint z, uint w)
        {
            _x = x; _y = y; _z = z; _w = w;
        }

        public void Reseed(uint seed)
        {
            // 避免全零状态；用 splitmix 风格散开种子。
            _x = seed != 0 ? seed : 0x9E3779B9u;
            _y = _x * 1812433253u + 1u;
            _z = _y * 1812433253u + 1u;
            _w = _z * 1812433253u + 1u;
        }

        private uint NextUInt()
        {
            uint t = _x ^ (_x << 11);
            _x = _y; _y = _z; _z = _w;
            _w = _w ^ (_w >> 19) ^ (t ^ (t >> 8));
            return _w;
        }

        /// <summary>返回 [0,1) 双精度。</summary>
        public double NextDouble()
        {
            // 取高 53 位组合，保证均匀分布于 [0,1)。
            ulong hi = NextUInt();
            ulong lo = NextUInt();
            ulong bits = (hi << 21) ^ lo; // 53 位有效
            return (bits & ((1UL << 53) - 1)) / (double)(1UL << 53);
        }

        /// <summary>返回 [0,1) 单精度。</summary>
        public float NextFloat() => (float)NextDouble();

        /// <summary>返回 [minInclusive, maxExclusive) 区间的整数。</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }
            uint range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        /// <summary>以概率 p（[0,1]）返回 true。</summary>
        public bool Chance(double p) => NextDouble() < p;

        /// <summary>导出内部状态，用于序列化存档。</summary>
        public (uint x, uint y, uint z, uint w) GetState() => (_x, _y, _z, _w);
    }
}
