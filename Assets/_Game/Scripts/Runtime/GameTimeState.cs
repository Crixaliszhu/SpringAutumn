namespace SpringAutumn.Runtime
{
    /// <summary>游戏时间状态。年/月，从第 1 年 1 月开始。</summary>
    public class GameTimeState
    {
        public int Year = 1;
        public int Month = 1;

        public GameTimeState() { }

        public GameTimeState(int year, int month)
        {
            Year = year;
            Month = month;
        }

        /// <summary>推进一个月，满 12 月进位到下一年。</summary>
        public void AdvanceMonth()
        {
            Month++;
            if (Month > 12)
            {
                Month = 1;
                Year++;
            }
        }

        /// <summary>自第 1 年 1 月起的累计月数（用于计算时长）。</summary>
        public int TotalMonths => (Year - 1) * 12 + Month;

        public override string ToString() => $"第{Year}年{Month}月";
    }
}
