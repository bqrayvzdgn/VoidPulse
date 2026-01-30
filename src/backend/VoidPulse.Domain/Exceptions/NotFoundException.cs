namespace VoidPulse.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public string EntityName { get; }
    public Guid EntityId { get; }

    public NotFoundException(string entityName, Guid id)
        : base($"{entityName} with ID '{id}' was not found.")
    {
        EntityName = entityName;
        EntityId = id;
    }
}
