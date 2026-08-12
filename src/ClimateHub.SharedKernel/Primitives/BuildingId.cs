using System.Globalization;

namespace ClimateHub.SharedKernel.Primitives;

public readonly record struct BuildingId(Guid Value) : IParsable<BuildingId>
{
    public static BuildingId New() => new(Guid.NewGuid());
    public static BuildingId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");

    public static BuildingId Parse(string s, IFormatProvider? provider = null)
        => new(Guid.Parse(s));

    public static bool TryParse(string? s, IFormatProvider? provider, out BuildingId result)
    {
        if (Guid.TryParse(s, out var guid))
        {
            result = new BuildingId(guid);
            return true;
        }
        result = default;
        return false;
    }
}
