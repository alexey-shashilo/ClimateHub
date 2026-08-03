namespace ClimateHub.SharedKernel.Primitives;

public readonly record struct BootId(Guid Value)
{
    public static BootId New() => new(Guid.NewGuid());
    public static BootId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}