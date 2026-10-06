namespace ERGraph.Errors
{
    /// <summary>
    /// Exception thrown when attempting to assign a name to an entity, but the name is
    /// already registered to another entity.
    /// </summary>
    public class DuplicateNameException : ERGraphException
    {
        /// <inheritdoc/>
        public DuplicateNameException()
        {

        }

        /// <inheritdoc/>
        public DuplicateNameException(string? message) : base(message)
        {

        }
    }
}