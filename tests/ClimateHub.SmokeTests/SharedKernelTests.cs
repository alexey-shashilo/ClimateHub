using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.SmokeTests;

public class SharedKernelTests
{
    [Fact]
    public void BuildingId_CanBeCreated()
    {
        var id = BuildingId.New();
        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void BuildingId_ToString_ReturnsFormattedGuid()
    {
        var guid = Guid.NewGuid();
        var id = BuildingId.From(guid);
        Assert.Equal(guid.ToString("D"), id.ToString());
    }

    [Fact]
    public void TemperatureC_ValidValue_Succeeds()
    {
        var temp = TemperatureC.From(22.4);
        Assert.Equal(22.4, temp.Value);
    }

    [Fact]
    public void TemperatureC_OutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TemperatureC.From(100));
        Assert.Throws<ArgumentOutOfRangeException>(() => TemperatureC.From(-60));
    }

    [Fact]
    public void RelativeHumidityPct_ValidValue_Succeeds()
    {
        var humidity = RelativeHumidityPct.From(41.7);
        Assert.Equal(41.7, humidity.Value);
    }

    [Fact]
    public void RelativeHumidityPct_OutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RelativeHumidityPct.From(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => RelativeHumidityPct.From(101));
    }

    [Fact]
    public void Co2Ppm_ValidValue_Succeeds()
    {
        var co2 = Co2Ppm.From(735);
        Assert.Equal(735, co2.Value);
    }

    [Fact]
    public void Co2Ppm_OutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Co2Ppm.From(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Co2Ppm.From(10001));
    }

    [Fact]
    public void ValueObjects_WithNaN_Throws()
    {
        Assert.Throws<ArgumentException>(() => TemperatureC.From(double.NaN));
        Assert.Throws<ArgumentException>(() => RelativeHumidityPct.From(double.PositiveInfinity));
        Assert.Throws<ArgumentException>(() => Co2Ppm.From(double.NegativeInfinity));
    }
}