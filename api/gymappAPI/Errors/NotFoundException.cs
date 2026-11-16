namespace gymappAPI.Errors;

/// <summary>Thrown when a requested resource doesn't exist; GlobalExceptionHandler turns it into a 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
