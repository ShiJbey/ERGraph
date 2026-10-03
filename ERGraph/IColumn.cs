namespace ERGraph
{
    using EntityID = int;

    /// <summary>
    /// Interface implemented by all data columns.
    /// This interface allows for type-erased columns at the database-level.
    /// </summary>
    internal interface IColumn
    {
        /// <summary>
        /// Check if the column contains an entry for the given entity.
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public bool Contains(EntityID entityId);

        /// <summary>
        /// Remove all data associated with the given entity.
        /// </summary>
        /// <param name="entityId"></param>
        public void Remove(EntityID entityId);
    }
} // namespace ERGraph