using System;
using System.Collections.Generic;

namespace ERGraph
{
    using EntityID = int;

    /// <summary>
    /// An column of data that maps entities to data values. Columns enable the database
    /// to model the "array-of-structs" architecture that helps entity-component systems
    /// be more cache efficient.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class Column<T> : IColumn
    {
        /// <summary>
        /// The number of new entries added to the column's data array when
        /// resizing.
        /// </summary>
        private static readonly int s_columnSizeIncrement = 64;

        /// <summary>
        /// An array containing the data tracked by this column.
        /// </summary>
        private T?[] m_data = new T[s_columnSizeIncrement];
        /// <summary>
        /// A map of entities to their assigned index in the column's data array.
        /// </summary>
        private Dictionary<EntityID, int> m_entityToIndex = new();
        /// <summary>
        /// Indices previously occupied by entities.
        /// </summary>
        private Queue<int> m_freeIndices = new();
        /// <summary>
        /// The next index assigned to an entity if no free indices are available.
        /// </summary>
        private int m_nextIndex = 0;

        /// <summary>
        /// Set the value for an entity.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="value"></param>
        public void Set(EntityID entityId, T? value)
        {
            if (!m_entityToIndex.TryGetValue(entityId, out var index))
            {
                index = GetFreeIndex();
                m_entityToIndex.Add(entityId, index);
            }

            m_data[index] = value;
        }

        /// <summary>
        /// Attempt to retrieve the value for a given entity.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGet(EntityID entityId, out T? value)
        {
            if (m_entityToIndex.TryGetValue(entityId, out var index))
            {
                value = m_data[index];
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Check if the column contains an entry for the given entity.
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public bool Contains(EntityID entityId)
        {
            return m_entityToIndex.ContainsKey(entityId);
        }

        /// <summary>
        /// Remove all data associated with the given entity.
        /// </summary>
        /// <param name="entityId"></param>
        public void Remove(EntityID entityId)
        {
            if (m_entityToIndex.TryGetValue(entityId, out var index))
            {
                m_data[index] = default;
                m_entityToIndex.Remove(entityId);
                m_freeIndices.Enqueue(index);
            }
        }

        /// <summary>
        /// Get the next free index within the data array.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Increase the size of the data array to accommodate more entities.
        /// </summary>
        private void IncreaseDataArrayLength()
        {
            Array.Resize(ref m_data, m_data.Length + s_columnSizeIncrement);
        }
    }
} // namespace ERGraph