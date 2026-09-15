namespace EfCoreWrapper.Data.Exceptions;

/// <summary>
/// Represents an error raised by the repository or unit of work layer.
/// </summary>
public class RepositoryException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryException"/> class.
    /// </summary>
    public RepositoryException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryException"/> class with a message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public RepositoryException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public RepositoryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
