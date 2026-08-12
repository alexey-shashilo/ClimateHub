using System.Globalization;

namespace ClimateHub.SharedKernel.Primitives;

public readonly record struct FloorId(Guid Value) : IParsable<FloorId>
{
    public static FloorId New() => new(Guid.NewGuid());
    public static FloorId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");

    public static FloorId Parse(string s, IFormatProvider? provider = null)
        => new(Guid.Parse(s));

    public static bool TryParse(string? s, IFormatProvider? provider, out FloorId result)
    {
        if (Guid.TryParse(s, out var guid))
        {
            result = new FloorId(guid);
            return true;
        }
        result = default;
        return false;
    }
}
