namespace ERGraph.Errors
{
    /// <summary>
    /// Error thrown when an entity is not found in the database.
    /// </summary>
    public class EntityNotFoundException : ERGraphException
    {
        public EntityNotFoundException()
        {

        }

        public EntityNotFoundException(string? message) : base(message)
        {

        }
    }
}