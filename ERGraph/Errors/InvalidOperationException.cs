namespace ERGraph.Errors
{
    /// <summary>
    /// Error thrown when performing an invalid operation
    /// </summary>
    public class InvalidOperationException : ERGraphException
    {
        public InvalidOperationException()
        {

        }

        public InvalidOperationException(string? message) : base(message)
        {

        }
    }
}