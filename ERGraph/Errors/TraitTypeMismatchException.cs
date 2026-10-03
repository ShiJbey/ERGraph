namespace ERGraph.Errors
{
    /// <summary>
    /// An exception thrown when attempting to set a trait value that
    /// does not match the datatype of the column.
    /// </summary>
    public class TraitTypeMismatchException : ERGraphException
    {
        /// <inheritdoc/>
        public TraitTypeMismatchException()
        {

        }

        /// <inheritdoc/>
        public TraitTypeMismatchException(string? message) : base(message)
        {

        }
    }
}