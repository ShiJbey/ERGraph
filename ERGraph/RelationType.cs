namespace ERGraph
{
    using EdgeID = int;

    public class RelationType
    {
        private static readonly int s_columnSizeIncrement = 64;

        public readonly string name;
        public readonly bool isDirected;

        private Queue<int> m_freeIndices = new();
        private EntityID[] m_sources = new EntityID[s_columnSizeIncrement];
        private EntityID[] m_targets = new EntityID[s_columnSizeIncrement];
        private EdgeID m_nextEdgeIndex = 0;
        private Dictionary<EdgeID, int> m_edgeToIndex = new();

        private EdgeID m_nextEdgeId = 0;
        private readonly Queue<EdgeID> m_freeEdgeIds = new();
        private readonly HashSet<EdgeID> m_activeEdgeIds = new();

        private readonly Dictionary<string, IColumn> m_fields = new();
        private readonly Dictionary<EntityID, List<EdgeID>> m_outgoingEdges = new();
        private readonly Dictionary<EntityID, List<EdgeID>> m_incomingEdges = new();

        public RelationType(string name, bool isDirected)
        {
            this.name = name;
            this.isDirected = isDirected;
        }

        public EdgeID AddEdge(EntityID source, EntityID target)
        {
            EdgeID edgeId = m_freeEdgeIds.Count > 0
                ? m_freeEdgeIds.Dequeue()
                : m_nextEdgeId++;

            int edgeIndex = GetFreeIndex();

            m_sources[edgeIndex] = source;
            m_targets[edgeIndex] = target;
            m_activeEdgeIds.Add(edgeId);

            AddToEntityEdgeDict(m_outgoingEdges, source, edgeId);

            if (isDirected)
            {
                AddToEntityEdgeDict(m_incomingEdges, target, edgeId);
            }
            else
            {
                AddToEntityEdgeDict(m_outgoingEdges, target, edgeId);
            }

            return edgeId;
        }

        public void RemoveEdge(EdgeID edgeId)
        {
            if (!m_activeEdgeIds.Contains(edgeId))
            {
                return;
            }

            m_activeEdgeIds.Remove(edgeId);

            DeleteAllFieldForEdge(edgeId);


            RemoveEdgeFromEntityEdgeDict(m_incomingEdges, edgeId);
            RemoveEdgeFromEntityEdgeDict(m_outgoingEdges, edgeId);

            m_edgeToIndex.Remove(edgeId);

            m_freeEdgeIds.Enqueue(edgeId);
        }

        public void RemoveEntity(EntityID entityId)
        {
            // Remove all outgoing edges
            if (m_outgoingEdges.TryGetValue(entityId, out var outgoingEdgeIds))
            {
                foreach (var edgeId in outgoingEdgeIds)
                {
                    RemoveEdge(edgeId);
                }
                m_outgoingEdges.Remove(entityId);
            }

            // Remove all incoming edges
            if (m_incomingEdges.TryGetValue(entityId, out var incomingEdgeIds))
            {
                foreach (var edgeId in incomingEdgeIds)
                {
                    RemoveEdge(edgeId);
                }
                m_outgoingEdges.Remove(entityId);
            }
        }

        public (EntityID, EntityID) GetEndpoints(EdgeID edgeId)
        {
            int edgeIndex = m_edgeToIndex[edgeId];
            return (m_sources[edgeIndex], m_targets[edgeIndex]);
        }

        public IEnumerable<EdgeID> GetEdgesFrom(EntityID entityId)
        {
            if (m_outgoingEdges.TryGetValue(entityId, out var edges))
            {
                foreach (var edge in edges)
                {
                    yield return edge;
                }
            }
        }

        public IEnumerable<EdgeID> GetEdgesTo(EntityID entityId)
        {
            var edgeDictionary = isDirected ? m_incomingEdges : m_outgoingEdges;
            if (edgeDictionary.TryGetValue(entityId, out var edges))
            {
                foreach (var edge in edges)
                {
                    yield return edge;
                }
            }
        }

        public IEnumerable<EdgeID> GetAllEdgeIds()
        {
            foreach (var id in m_activeEdgeIds)
            {
                yield return id;
            }
        }

        public void SetField<T>(EdgeID edgeId, string field, T value)
        {
            GetOrCreateColumn<T>(field).Set(edgeId, value);
        }

        public bool TryGetField<T>(EdgeID edgeId, string field, out T? value)
        {
            value = default;
            return m_fields.TryGetValue(field, out var col) && col is Column<T> typed && typed.TryGet(edgeId, out value);
        }

        private Column<T> GetOrCreateColumn<T>(string field)
        {
            if (!m_fields.TryGetValue(field, out var col))
            {
                col = new Column<T>();
                m_fields.Add(field, col);
            }

            return (Column<T>)col;
        }

        private int GetFreeIndex()
        {
            if (m_freeIndices.Count > 0)
            {
                return m_freeIndices.Dequeue();
            }

            if (m_nextEdgeIndex == m_sources.Length)
            {
                IncreaseDataArrayLength();
            }

            int index = m_nextEdgeIndex++;
            return index;
        }

        private void DeleteAllFieldForEdge(EdgeID edgeId)
        {
            foreach (var col in m_fields.Values)
            {
                col.Remove(edgeId);
            }
        }

        private static void AddToEntityEdgeDict(Dictionary<EntityID, List<EdgeID>> dict, EntityID entityId, EdgeID edgeId)
        {
            if (!dict.TryGetValue(entityId, out var edgeList))
            {
                edgeList = new List<EdgeID>();
                dict.Add(entityId, edgeList);
            }

            edgeList.Add(edgeId);
        }

        private static void RemoveEdgeFromEntityEdgeDict(Dictionary<EntityID, List<EdgeID>> dict, EdgeID edgeId)
        {
            foreach (var edgeIdList in dict.Values)
            {
                if (edgeIdList != null)
                {
                    edgeIdList.Remove(edgeId);
                }
            }
        }

        private void IncreaseDataArrayLength()
        {
            Array.Resize(ref m_sources, m_sources.Length + s_columnSizeIncrement);
            Array.Resize(ref m_targets, m_targets.Length + s_columnSizeIncrement);
        }
    }
} // namespace ERGraph