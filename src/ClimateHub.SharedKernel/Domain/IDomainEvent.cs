namespace ClimateHub.SharedKernel.Domain;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
    Guid EventId { get; }
    Guid? CorrelationId { get; }
    Guid? CausationId { get; }
}
