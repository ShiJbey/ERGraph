namespace ERGraph.Errors
{
    /// <summary>
    /// An exception thrown when a relationship is not found in the database.
    /// </summary>
    public class RelationshipNotFoundException : ERGraphException
    {
        /// <inheritdoc/>
        public RelationshipNotFoundException()
        {

        }

        /// <inheritdoc/>
        public RelationshipNotFoundException(string? message) : base(message)
        {

        }
    }
}