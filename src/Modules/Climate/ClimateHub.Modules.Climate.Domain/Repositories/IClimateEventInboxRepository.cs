namespace ClimateHub.Modules.Climate.Domain.Repositories;

public interface IClimateEventInboxRepository
{
    Task AddAsync(ClimateEventInboxEntry entry, CancellationToken ct = default);
    Task UpdateAsync(ClimateEventInboxEntry entry, CancellationToken ct = default);
}
