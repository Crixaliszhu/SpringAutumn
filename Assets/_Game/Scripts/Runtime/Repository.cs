using System.Collections.Generic;

namespace SpringAutumn.Runtime
{
    /// <summary>
    /// 实体仓库，以 Id 字典提供 O(1) 查询。用于 WorldRuntime 统一管理各类实体。
    /// </summary>
    public class Repository<T> where T : Entity
    {
        private readonly Dictionary<string, T> _data = new Dictionary<string, T>();

        public int Count => _data.Count;

        /// <summary>按 Id 取实体，不存在则抛 KeyNotFoundException。</summary>
        public T Get(string id) => _data[id];

        public bool TryGet(string id, out T entity) => _data.TryGetValue(id, out entity);

        public bool Contains(string id) => _data.ContainsKey(id);

        /// <summary>新增实体；Id 为空或重复时抛异常。</summary>
        public void Add(T entity)
        {
            if (entity == null)
            {
                throw new System.ArgumentNullException(nameof(entity));
            }
            if (string.IsNullOrEmpty(entity.Id))
            {
                throw new System.ArgumentException("Entity.Id 不能为空");
            }
            if (_data.ContainsKey(entity.Id))
            {
                throw new System.ArgumentException($"重复的实体 Id: '{entity.Id}'");
            }
            _data.Add(entity.Id, entity);
        }

        public bool Remove(string id) => _data.Remove(id);

        public IEnumerable<T> GetAll() => _data.Values;
    }
}
