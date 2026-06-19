namespace SpringAutumn.Presentation
{
    /// <summary>
    /// 表现层程序集标记类型，确保 SpringAutumn.View 程序集成立。
    /// 表现层依赖 SpringAutumn.Core，仅通过 Command 写入、EventBus 读取。
    /// </summary>
    public static class ViewAssemblyMarker
    {
        public const string AssemblyName = "SpringAutumn.View";
    }
}
