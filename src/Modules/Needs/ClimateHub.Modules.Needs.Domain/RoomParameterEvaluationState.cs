using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Needs.Domain;

public class RoomParameterEvaluationState
{
    public long Id { get; private set; }
    public BuildingId BuildingId { get; private set; }
    public RoomId RoomId { get; private set; }
    public string ParameterCode { get; private set; } = string.Empty;
    public string? PolicyVersion { get; private set; }
    public string? ViolationDirection { get; private set; }
    public DateTimeOffset? ViolationSince { get; private set; }
    public double? LastValue { get; private set; }
    public DateTimeOffset? LastMeasuredAt { get; private set; }
    public string? LastQuality { get; private set; }
    public DateTimeOffset? LastEvaluationAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public long Version { get; private set; }

    private RoomParameterEvaluationState() { }

    public static RoomParameterEvaluationState Create(
        BuildingId buildingId, RoomId roomId, string parameterCode,
        string? policyVersion = null)
    {
        return new RoomParameterEvaluationState
        {
            BuildingId = buildingId,
            RoomId = roomId,
            ParameterCode = parameterCode,
            PolicyVersion = policyVersion,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };
    }

    public void RecordViolation(DateTimeOffset now)
    {
        ViolationSince ??= now;
        UpdatedAt = now;
        Version++;
    }

    public void ClearViolation(DateTimeOffset now)
    {
        ViolationSince = null;
        ViolationDirection = null;
        UpdatedAt = now;
        Version++;
    }

    public void UpdateEvaluation(double value, string quality, DateTimeOffset measuredAt, DateTimeOffset now)
    {
        LastValue = value;
        LastMeasuredAt = measuredAt;
        LastQuality = quality;
        LastEvaluationAt = now;
        UpdatedAt = now;
        Version++;
    }

    public bool IsViolationConfirmed(TimeSpan minDuration, DateTimeOffset now)
    {
        if (!ViolationSince.HasValue) return false;
        return now - ViolationSince.Value >= minDuration;
    }
}
