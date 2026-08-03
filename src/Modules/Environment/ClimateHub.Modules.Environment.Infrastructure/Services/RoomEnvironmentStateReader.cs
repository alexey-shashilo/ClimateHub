using ClimateHub.Modules.Environment.Contracts;
using ClimateHub.Modules.Environment.Infrastructure.Repositories;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Environment.Infrastructure.Services;

public class RoomEnvironmentStateReader(
    IRoomEnvironmentStateRepository envRepo) : IRoomEnvironmentStateReader
{
    public async Task<IReadOnlyCollection<ParameterStateDto>> GetParametersAsync(RoomId roomId, CancellationToken ct = default)
    {
        var parameters = await envRepo.GetParametersAsync(roomId, ct);
        return parameters.Select(p => new ParameterStateDto(
            p.Parameter, p.Value, p.Unit, p.MeasuredAt, p.ReceivedAt,
            p.Quality ?? "unknown",
            p.SourceDeviceId)).ToList();
    }
}

public class RoomPolicyReader(
    Repositories.IRoomPolicyRepository policyRepo) : IRoomPolicyReader
{
    public async Task<RoomPolicyDto?> GetByRoomAsync(RoomId roomId, CancellationToken ct = default)
    {
        var policy = await policyRepo.GetByRoomAsync(roomId, ct);
        if (policy is null) return null;
        return new RoomPolicyDto(
            roomId,
            policy.TemperatureMin, policy.TemperatureMax, policy.TemperaturePreferred, policy.TemperatureMode.ToString() ?? "monitorOnly",
            policy.HumidityMin, policy.HumidityMax, policy.HumidityPreferred, policy.HumidityMode.ToString() ?? "monitorOnly",
            policy.Co2Min, policy.Co2Max, policy.Co2Preferred, policy.Co2Mode.ToString() ?? "monitorOnly",
            policy.IlluminanceMin, policy.IlluminanceMax, policy.IlluminancePreferred, policy.IlluminanceMode.ToString() ?? "monitorOnly");
    }
}