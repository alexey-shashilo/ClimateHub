using ClimateHub.Modules.Climate.Application.EffectModel;

namespace ClimateHub.Modules.Climate.UnitTests;

public class CrossSystemEffectModelTests
{
    private readonly CrossSystemEffectModel _model = new();

    [Fact]
    public void AirflowIncrease_ShouldDecreaseTemperature()
    {
        var effects = _model.GetEffects("eng.airflow.increase");
        Assert.Contains(effects, e => e.TargetParameter == "temperature" && e.Direction == EffectDirection.Decrease);
    }

    [Fact]
    public void AirflowIncrease_ShouldDecreaseHumidity()
    {
        var effects = _model.GetEffects("eng.airflow.increase");
        Assert.Contains(effects, e => e.TargetParameter == "humidity" && e.Direction == EffectDirection.Decrease);
    }

    [Fact]
    public void TemperatureIncrease_ShouldDecreaseHumidity()
    {
        var effects = _model.GetEffects("eng.temperature.increase");
        Assert.Contains(effects, e => e.TargetParameter == "humidity" && e.Direction == EffectDirection.Decrease);
    }

    [Fact]
    public void TemperatureDecrease_ShouldIncreaseHumidity()
    {
        var effects = _model.GetEffects("eng.temperature.decrease");
        Assert.Contains(effects, e => e.TargetParameter == "humidity" && e.Direction == EffectDirection.Increase);
    }

    [Fact]
    public void HasNegativeEffect_VentilationOnHeating_ShouldReturnTrue()
    {
        var result = _model.HasNegativeEffect("eng.airflow.increase", "eng.temperature.increase");
        Assert.True(result);
    }

    [Fact]
    public void HasNegativeEffect_AirflowOnAirflow_ShouldReturnFalse()
    {
        var result = _model.HasNegativeEffect("eng.airflow.increase", "eng.airflow.increase");
        Assert.False(result);
    }

    [Fact]
    public void GetEffects_UnknownCapability_ShouldReturnEmpty()
    {
        var effects = _model.GetEffects("eng.unknown.capability");
        Assert.Empty(effects);
    }

    [Fact]
    public void HasNegativeEffect_HeatingOnHumidification_ShouldReturnTrue()
    {
        var result = _model.HasNegativeEffect("eng.temperature.increase", "eng.humidity.increase");
        Assert.True(result);
    }

    [Fact]
    public void HasNegativeEffect_CoolingOnDehumidification_ShouldReturnTrue()
    {
        var result = _model.HasNegativeEffect("eng.temperature.decrease", "eng.humidity.decrease");
        Assert.True(result);
    }

    [Fact]
    public void Co2Reduce_ShouldAffectTemperatureAndHumidity()
    {
        var effects = _model.GetEffects("eng.co2.reduce");
        Assert.Contains(effects, e => e.TargetParameter == "temperature");
        Assert.Contains(effects, e => e.TargetParameter == "humidity");
    }

    [Fact]
    public void IlluminanceIncrease_ShouldIncreaseTemperature()
    {
        var effects = _model.GetEffects("eng.illuminance.increase");
        Assert.Contains(effects, e => e.TargetParameter == "temperature" && e.Direction == EffectDirection.Increase);
    }

    [Fact]
    public void IlluminanceDecrease_ShouldDecreaseTemperature()
    {
        var effects = _model.GetEffects("eng.illuminance.decrease");
        Assert.Contains(effects, e => e.TargetParameter == "temperature" && e.Direction == EffectDirection.Decrease);
    }

    [Fact]
    public void GetEffects_Airflow_ShouldReturnTwoEffects()
    {
        var effects = _model.GetEffects("eng.airflow.increase");
        Assert.Equal(2, effects.Count);
    }
}