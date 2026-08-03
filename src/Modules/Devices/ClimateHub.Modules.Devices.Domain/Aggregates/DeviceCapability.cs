using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Devices.Domain.Aggregates;

public class DeviceCapability : Entity<Guid>
{
    public DeviceId DeviceId { get; private set; }
    public string Code { get; private set; }
    public string DataType { get; private set; }
    public string Unit { get; private set; }
    public double? MinValue { get; private set; }
    public double? MaxValue { get; private set; }
    public string Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }

    private DeviceCapability()
    {
        Code = null!;
        DataType = null!;
        Unit = null!;
        Status = null!;
    }

    private DeviceCapability(Guid id, DeviceId deviceId, string code, string dataType, string unit,
        double? minValue, double? maxValue, string status)
    {
        Id = id;
        DeviceId = deviceId;
        Code = code;
        DataType = dataType;
        Unit = unit;
        MinValue = minValue;
        MaxValue = maxValue;
        Status = status;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static DeviceCapability Create(DeviceId deviceId, string code, string dataType, string unit,
        double? minValue = null, double? maxValue = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Capability code cannot be empty", nameof(code));

        return new DeviceCapability(Guid.NewGuid(), deviceId, code, dataType, unit, minValue, maxValue, "active");
    }

    public static DeviceCapability Temperature(DeviceId deviceId) =>
        Create(deviceId, "measure.temperature", "double", "celsius", -50, 100);

    public static DeviceCapability Humidity(DeviceId deviceId) =>
        Create(deviceId, "measure.relative-humidity", "double", "percent", 0, 100);

    public static DeviceCapability Co2(DeviceId deviceId) =>
        Create(deviceId, "measure.co2", "double", "ppm", 0, 100000);
}