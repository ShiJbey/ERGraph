namespace ERGraph
{
    public class Column<T> : IColumn
    {
        private static readonly int s_columnSizeIncrement = 64;

        private T?[] m_data = new T[s_columnSizeIncrement];
        private Dictionary<int, int> m_entityToIndex = new();
        private Queue<int> m_freeIndices = new();
        private int m_nextIndex = 0;

        public void Set(int entityId, T? value)
        {
            if (!m_entityToIndex.TryGetValue(entityId, out var index))
            {
                index = GetFreeIndex();
                m_entityToIndex.Add(entityId, index);
            }

            m_data[index] = value;
        }

        public bool TryGet(int entityId, out T? value)
        {
            if (m_entityToIndex.TryGetValue(entityId, out var index))
            {
                value = m_data[index];
                return true;
            }

            value = default;
            return false;
        }

        public void Remove(int entityId)
        {
            if (m_entityToIndex.TryGetValue(entityId, out var index))
            {
                m_data[index] = default;
                m_entityToIndex.Remove(entityId);
                m_freeIndices.Enqueue(index);
            }
        }

        private int GetFreeIndex()
        {
            if (m_freeIndices.Count > 0)
            {
                return m_freeIndices.Dequeue();
            }

            if (m_nextIndex == m_data.Length)
            {
                IncreaseDataArrayLength();
            }

            int index = m_nextIndex++;
            return index;
        }

        private void IncreaseDataArrayLength()
        {
            Array.Resize(ref m_data, m_data.Length + s_columnSizeIncrement);
        }
    }
} // namespace ERGraph