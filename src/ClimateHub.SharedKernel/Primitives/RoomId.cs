using System.Globalization;

namespace ClimateHub.SharedKernel.Primitives;

public readonly record struct RoomId(Guid Value) : IParsable<RoomId>
{
    public static RoomId New() => new(Guid.NewGuid());
    public static RoomId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");

    public static RoomId Parse(string s, IFormatProvider? provider = null)
        => new(Guid.Parse(s));

    public static bool TryParse(string? s, IFormatProvider? provider, out RoomId result)
    {
        if (Guid.TryParse(s, out var guid))
        {
            result = new RoomId(guid);
            return true;
        }
        result = default;
        return false;
    }
}