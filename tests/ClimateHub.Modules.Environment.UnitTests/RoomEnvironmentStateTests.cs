using ClimateHub.Modules.Environment.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Environment.UnitTests;

public class RoomEnvironmentStateTests
{
    private static readonly RoomId TestRoomId = RoomId.New();
    private static readonly DeviceId TestDeviceId = DeviceId.New();

    [Fact]
    public void InitialState_HasNoValues()
    {
        var state = new RoomEnvironmentState(TestRoomId);

        Assert.Equal(TestRoomId, state.RoomId);
        Assert.Null(state.TemperatureC);
        Assert.Null(state.RelativeHumidityPct);
        Assert.Null(state.Co2Ppm);
        Assert.Equal(MeasurementQuality.Unknown, state.TemperatureQuality);
    }

    [Fact]
    public void Update_WithValidMeasurement_UpdatesValues()
    {
        var state = new RoomEnvironmentState(TestRoomId);
        var measuredAt = DateTimeOffset.UtcNow.AddSeconds(-10);

        var result = state.Update(measuredAt, temperatureC: 22.5, relativeHumidityPct: 41.7, co2Ppm: 735,
            quality: MeasurementQuality.Valid, TestDeviceId);

        Assert.Equal(MeasurementUpdateResult.Updated, result);
        Assert.Equal(22.5, state.TemperatureC);
        Assert.Equal(41.7, state.RelativeHumidityPct);
        Assert.Equal(735, state.Co2Ppm);
    }

    [Fact]
    public void Update_OlderMeasurement_IsSkipped()
    {
        var state = new RoomEnvironmentState(TestRoomId);
        var older = DateTimeOffset.UtcNow.AddMinutes(-10);
        var newer = DateTimeOffset.UtcNow.AddMinutes(-5);

        state.Update(newer, temperatureC: 22.5);
        var result = state.Update(older, temperatureC: 20.0);

        Assert.Equal(MeasurementUpdateResult.Skipped, result);
        Assert.Equal(22.5, state.TemperatureC);
    }

    [Fact]
    public void Update_SameTime_BetterQuality_Wins()
    {
        var state = new RoomEnvironmentState(TestRoomId);
        var measuredAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        state.Update(measuredAt, temperatureC: 22.5, quality: MeasurementQuality.Estimated);
        var result = state.Update(measuredAt, temperatureC: 23.0, quality: MeasurementQuality.Valid);

        Assert.Equal(MeasurementUpdateResult.Updated, result);
        Assert.Equal(23.0, state.TemperatureC);
    }

    [Fact]
    public void Update_SameTime_WorseQuality_Skipped()
    {
        var state = new RoomEnvironmentState(TestRoomId);
        var measuredAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        state.Update(measuredAt, temperatureC: 23.0, quality: MeasurementQuality.Valid);
        var result = state.Update(measuredAt, temperatureC: 22.0, quality: MeasurementQuality.Estimated);

        Assert.Equal(MeasurementUpdateResult.Skipped, result);
        Assert.Equal(23.0, state.TemperatureC);
    }

    [Fact]
    public void Update_PartialMeasurement_OnlyUpdatesProvidedFields()
    {
        var state = new RoomEnvironmentState(TestRoomId);
        var measuredAt = DateTimeOffset.UtcNow.AddSeconds(-10);

        state.Update(measuredAt, temperatureC: 22.5);

        Assert.Equal(22.5, state.TemperatureC);
        Assert.Null(state.RelativeHumidityPct);
        Assert.Null(state.Co2Ppm);

        state.Update(measuredAt.AddSeconds(5), relativeHumidityPct: 40.0);

        Assert.Equal(22.5, state.TemperatureC);
        Assert.Equal(40.0, state.RelativeHumidityPct);
        Assert.Null(state.Co2Ppm);
    }

    [Fact]
    public void MultipleDevices_UpdateIndependently()
    {
        var state = new RoomEnvironmentState(TestRoomId);
        var deviceA = DeviceId.New();
        var deviceB = DeviceId.New();
        var t1 = DateTimeOffset.UtcNow.AddSeconds(-20);
        var t2 = DateTimeOffset.UtcNow.AddSeconds(-10);

        state.Update(t1, temperatureC: 22.0, sourceDeviceId: deviceA);
        state.Update(t2, relativeHumidityPct: 41.0, sourceDeviceId: deviceB);

        Assert.Equal(22.0, state.TemperatureC);
        Assert.Equal(41.0, state.RelativeHumidityPct);
        Assert.Equal(deviceA, state.TemperatureSourceDeviceId);
        Assert.Equal(deviceB, state.HumiditySourceDeviceId);
    }
}