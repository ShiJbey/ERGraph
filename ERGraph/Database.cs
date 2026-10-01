namespace ERGraph
{
    public class Database
    {
        private int m_nextEntityId = 0;
        private readonly Queue<EntityID> m_freeEntityIds = new();
        private readonly HashSet<EntityID> m_activeEntityIds = new();
        private readonly Dictionary<string, IColumn> m_columns = new();
        private readonly Dictionary<string, RelationType> m_relations = new();

        public EntityID CreateEntity()
        {
            EntityID entityId = m_freeEntityIds.Count > 0
                ? m_freeEntityIds.Dequeue()
                : new EntityID(m_nextEntityId++);
            m_activeEntityIds.Add(entityId);
            return entityId;
        }

        public bool DeleteEntity(EntityID entityId)
        {
            if (!m_activeEntityIds.Contains(entityId))
            {
                return false;
            }

            m_activeEntityIds.Remove(entityId);

            foreach (var col in m_columns.Values)
            {
                col.Remove(entityId.Value);
            }

            foreach (var relation in m_relations.Values)
            {
                relation.RemoveEntity(entityId);
            }

            m_freeEntityIds.Enqueue(entityId);

            return true;
        }

        public RelationType GetRelation(string name)
        {
            return m_relations[name];
        }

        public RelationType GetOrCreateRelation(string name, bool isDirected = true)
        {
            if (!m_relations.TryGetValue(name, out var relation))
            {
                relation = new RelationType(name, isDirected);
                m_relations.Add(name, relation);
            }

            return relation;
        }
    }
} // namespace ERGraph