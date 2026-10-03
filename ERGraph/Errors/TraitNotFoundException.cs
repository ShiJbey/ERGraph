namespace ERGraph.Errors
{
    /// <summary>
    /// Exception thrown when a trait is not found in the database.
    /// </summary>
    public class TraitNotFoundException : ERGraphException
    {
        /// <inheritdoc/>
        public TraitNotFoundException()
        {

        }

        /// <inheritdoc/>
        public TraitNotFoundException(string? message) : base(message)
        {

        }
    }
}