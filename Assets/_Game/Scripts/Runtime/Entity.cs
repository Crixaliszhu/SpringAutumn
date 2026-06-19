namespace SpringAutumn.Runtime
{
    /// <summary>所有世界运行时实体的基类，统一以字符串 Id 标识。</summary>
    public abstract class Entity
    {
        public string Id;

        protected Entity() { }

        protected Entity(string id)
        {
            Id = id;
        }
    }
}
