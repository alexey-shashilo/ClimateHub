using System.Globalization;

namespace ClimateHub.SharedKernel.Primitives;

public readonly record struct DeviceId(Guid Value) : IParsable<DeviceId>
{
    public static DeviceId New() => new(Guid.NewGuid());
    public static DeviceId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");

    public static DeviceId Parse(string s, IFormatProvider? provider = null)
        => new(Guid.Parse(s));

    public static bool TryParse(string? s, IFormatProvider? provider, out DeviceId result)
    {
        if (Guid.TryParse(s, out var guid))
        {
            result = new DeviceId(guid);
            return true;
        }
        result = default;
        return false;
    }
}