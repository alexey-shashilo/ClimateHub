using ClimateHub.Modules.Climate.Domain.ClimateGoals;
using ClimateHub.Modules.Environment.Contracts;
using ClimateHub.Modules.Needs.Contracts;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Climate.Application.GoalPlanning;

public class GoalPlanner
{
    private readonly IRoomEnvironmentStateReader _envReader;
    private readonly IRoomPolicyReader _policyReader;
    private readonly IRoomBuildingResolver _roomBuildingResolver;

    public GoalPlanner(IRoomEnvironmentStateReader envReader, IRoomPolicyReader policyReader,
        IRoomBuildingResolver roomBuildingResolver)
    {
        _envReader = envReader;
        _policyReader = policyReader;
        _roomBuildingResolver = roomBuildingResolver;
    }

    public async Task<ClimateGoal> CreateOrUpdateGoalAsync(RoomId roomId,
        StrategyProfile profile = StrategyProfile.Comfort,
        CancellationToken ct = default)
    {
        var buildingId = await _roomBuildingResolver.ResolveBuildingIdAsync(roomId, ct);
        if (buildingId is null)
            throw new InvalidOperationException($"No building found for room {roomId}");

        var parameters = await _envReader.GetParametersAsync(roomId, ct);
        var policy = await _policyReader.GetByRoomAsync(roomId, ct);

        var (temp, tempMin, tempMax) = GetTemperatureTargets(profile, policy);
        var (hum, humMin, humMax) = GetHumidityTargets(profile, policy);
        var (co2, co2Max) = GetCo2Targets(profile, policy);
        var illuminance = GetIlluminanceTarget(profile, policy);

        var goal = ClimateGoal.Create(
            buildingId.Value, roomId,
            temp, tempMin, tempMax,
            hum, humMin, humMax,
            co2, co2Max, illuminance,
            profile);

        var tempParam = parameters.FirstOrDefault(p => p.Parameter == "temperature");
        var humParam = parameters.FirstOrDefault(p => p.Parameter == "humidity");
        var co2Param = parameters.FirstOrDefault(p => p.Parameter == "co2");

        goal.UpdateCurrentConditions(
            tempParam?.Value, humParam?.Value, co2Param?.Value, null);

        return goal;
    }

    private static (double target, double min, double max) GetTemperatureTargets(
        StrategyProfile profile, RoomPolicyDto? policy) => profile switch
    {
        StrategyProfile.EnergySaving => (19, 18, 24),
        StrategyProfile.Night => (18, 17, 22),
        StrategyProfile.Away => (16, 15, 28),
        StrategyProfile.Sleep => (20, 19, 23),
        StrategyProfile.Vacation => (14, 12, 30),
        _ => (policy?.TemperaturePreferred ?? 23,
              policy?.TemperatureMin ?? 20,
              policy?.TemperatureMax ?? 26)
    };

    private static (double target, double min, double max) GetHumidityTargets(
        StrategyProfile profile, RoomPolicyDto? policy) => profile switch
    {
        StrategyProfile.EnergySaving => (45, 30, 60),
        _ => (policy?.HumidityPreferred ?? 45,
              policy?.HumidityMin ?? 30,
              policy?.HumidityMax ?? 60)
    };

    private static (double target, double max) GetCo2Targets(
        StrategyProfile profile, RoomPolicyDto? policy) => profile switch
    {
        StrategyProfile.MaximumAirQuality => (400, 800),
        StrategyProfile.Sleep => (500, 800),
        _ => (policy?.Co2Preferred ?? 600,
              policy?.Co2Max ?? 1000)
    };

    private static double GetIlluminanceTarget(StrategyProfile profile, RoomPolicyDto? policy) => profile switch
    {
        StrategyProfile.Night => 0,
        StrategyProfile.Sleep => 0,
        _ => policy?.IlluminancePreferred ?? 500
    };
}