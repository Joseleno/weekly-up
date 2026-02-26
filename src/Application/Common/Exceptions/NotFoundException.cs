namespace WeeklyUp.Application.Common.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException()
        : base("The requested entity was not found.")
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with key '{key}' was not found.")
    {
    }
}
