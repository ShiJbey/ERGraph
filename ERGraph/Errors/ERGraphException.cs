using System;

namespace ERGraph.Errors
{
    /// <summary>
    /// Base class inherited by all exceptions produced by ERGraph.
    /// </summary>
    public class ERGraphException : SystemException
    {
        /// <summary>
        /// Initializes a new instance of the exception class with default values.
        /// </summary>
        public ERGraphException() : base()
        {

        }

        /// <summary>
        /// Initializes a new instance of the exception class with a specified.
        /// error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ERGraphException(string? message) : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the exception class with the specified
        /// error message and a reference to the inner exception that is the
        /// cause of this exception.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception. If the
        /// innerException parameter is not null, the current exception is raised
        /// in a catch block that handles the inner exception.
        /// </param>
        public ERGraphException(
            string? message, Exception? innerException) : base(message, innerException)
        {

        }
    }
}