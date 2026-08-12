using ClimateHub.SharedKernel.Domain;

namespace ClimateHub.Modules.Devices.Domain.Aggregates;

public class DeviceModel : ValueObject
{
    public string Manufacturer { get; private set; }
    public string ModelName { get; private set; }
    public string? HardwareVersion { get; private set; }

    private DeviceModel()
    {
        Manufacturer = null!;
        ModelName = null!;
    } // EF Core

    public DeviceModel(string manufacturer, string modelName, string? hardwareVersion = null)
    {
        if (string.IsNullOrWhiteSpace(manufacturer))
            throw new ArgumentException("Manufacturer cannot be empty", nameof(manufacturer));
        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("Model name cannot be empty", nameof(modelName));

        Manufacturer = manufacturer.Trim();
        ModelName = modelName.Trim();
        HardwareVersion = hardwareVersion?.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Manufacturer;
        yield return ModelName;
        yield return HardwareVersion;
    }
}
