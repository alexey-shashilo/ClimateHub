using System.Text.Json;

namespace ClimateHub.HardwareAbstractionTests;

public class HardwareAbstractionLayerTests
{
    public abstract record DeviceCommand
    {
        public string DeviceId { get; init; } = "";
        public string CapabilityCode { get; init; } = "";
        public string Operation { get; init; } = "";
        public string ParametersJson { get; init; } = "{}";
        public DateTimeOffset IssuedAt { get; init; }
    }

    public abstract record DeviceState
    {
        public string DeviceId { get; init; } = "";
        public string Status { get; init; } = "offline";
        public DateTimeOffset LastReportedAt { get; init; }
    }

    public interface IDeviceRuntime
    {
        string DeviceType { get; }
        string[] SupportedCapabilities { get; }
        Task<DeviceCommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default);
        Task<DeviceState> GetStateAsync(CancellationToken ct = default);
        Task<bool> IsConnectedAsync(CancellationToken ct = default);
    }

    public record DeviceCommandResult
    {
        public bool Success { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }
        public string? ReportedStateJson { get; init; }
    }

    public class RelayRuntime : IDeviceRuntime
    {
        public string DeviceType => "relay";
        public string[] SupportedCapabilities => ["actuate.relay.on", "actuate.relay.off", "actuate.relay.toggle"];
        public bool IsOn { get; private set; }

        public Task<DeviceCommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
        {
            switch (command.Operation)
            {
                case "on":
                    IsOn = true;
                    return Task.FromResult(new DeviceCommandResult
                    {
                        Success = true,
                        ReportedStateJson = JsonSerializer.Serialize(new { relayState = "on", switchedAt = DateTimeOffset.UtcNow })
                    });
                case "off":
                    IsOn = false;
                    return Task.FromResult(new DeviceCommandResult
                    {
                        Success = true,
                        ReportedStateJson = JsonSerializer.Serialize(new { relayState = "off", switchedAt = DateTimeOffset.UtcNow })
                    });
                case "toggle":
                    IsOn = !IsOn;
                    return Task.FromResult(new DeviceCommandResult
                    {
                        Success = true,
                        ReportedStateJson = JsonSerializer.Serialize(new { relayState = IsOn ? "on" : "off", switchedAt = DateTimeOffset.UtcNow })
                    });
                default:
                    return Task.FromResult(new DeviceCommandResult
                    {
                        Success = false,
                        ErrorCode = "UNSUPPORTED_OPERATION",
                        ErrorMessage = $"Relay does not support operation: {command.Operation}"
                    });
            }
        }

        public Task<DeviceState> GetStateAsync(CancellationToken ct = default) =>
            Task.FromResult(new RelayState { DeviceId = "", Status = IsOn ? "active" : "inactive", IsOn = IsOn } as DeviceState);

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(true);
    }

    public record RelayState : DeviceState { public bool IsOn { get; init; } }

    public class FanRuntime : IDeviceRuntime
    {
        public string DeviceType => "fan";
        public string[] SupportedCapabilities => ["actuate.fan.on", "actuate.fan.off", "actuate.fan.set-speed"];
        public int Speed { get; private set; }
        public bool IsOn => Speed > 0;

        public Task<DeviceCommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
        {
            switch (command.Operation)
            {
                case "on":
                    Speed = 100;
                    return Task.FromResult(new DeviceCommandResult { Success = true, ReportedStateJson = "{\"speed\":100,\"state\":\"on\"}" });
                case "off":
                    Speed = 0;
                    return Task.FromResult(new DeviceCommandResult { Success = true, ReportedStateJson = "{\"speed\":0,\"state\":\"off\"}" });
                case "set-speed":
                    var speed = JsonSerializer.Deserialize<JsonElement>(command.ParametersJson).GetProperty("speed").GetInt32();
                    Speed = Math.Clamp(speed, 0, 100);
                    return Task.FromResult(new DeviceCommandResult { Success = true, ReportedStateJson = $"{{\"speed\":{Speed},\"state\":\"{(Speed > 0 ? "on" : "off")}\"}}" });
                default:
                    return Task.FromResult(new DeviceCommandResult { Success = false, ErrorCode = "UNSUPPORTED_OPERATION", ErrorMessage = $"Fan does not support operation: {command.Operation}" });
            }
        }

        public Task<DeviceState> GetStateAsync(CancellationToken ct = default) =>
            Task.FromResult(new FanState { DeviceId = "", Status = IsOn ? "active" : "inactive", Speed = Speed } as DeviceState);

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(true);
    }

    public record FanState : DeviceState { public int Speed { get; init; } }

    public class DamperRuntime : IDeviceRuntime
    {
        public string DeviceType => "damper";
        public string[] SupportedCapabilities => ["actuate.damper.open", "actuate.damper.close", "actuate.damper.set-position"];
        public int Position { get; private set; }

        public Task<DeviceCommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
        {
            switch (command.Operation)
            {
                case "open":
                    Position = 100;
                    return Task.FromResult(new DeviceCommandResult { Success = true, ReportedStateJson = "{\"position\":100,\"state\":\"open\"}" });
                case "close":
                    Position = 0;
                    return Task.FromResult(new DeviceCommandResult { Success = true, ReportedStateJson = "{\"position\":0,\"state\":\"closed\"}" });
                case "set-position":
                    var pos = JsonSerializer.Deserialize<JsonElement>(command.ParametersJson).GetProperty("position").GetInt32();
                    Position = Math.Clamp(pos, 0, 100);
                    return Task.FromResult(new DeviceCommandResult { Success = true, ReportedStateJson = $"{{\"position\":{Position},\"state\":\"{(Position > 0 ? "open" : "closed")}\"}}" });
                default:
                    return Task.FromResult(new DeviceCommandResult { Success = false, ErrorCode = "UNSUPPORTED_OPERATION", ErrorMessage = $"Damper does not support operation: {command.Operation}" });
            }
        }

        public Task<DeviceState> GetStateAsync(CancellationToken ct = default) =>
            Task.FromResult(new DamperState { DeviceId = "", Status = "active", Position = Position } as DeviceState);

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(true);
    }

    public record DamperState : DeviceState { public int Position { get; init; } }

    public class ValveRuntime : IDeviceRuntime
    {
        public string DeviceType => "valve";
        public string[] SupportedCapabilities => ["actuate.valve.open", "actuate.valve.close", "actuate.valve.set-position"];
        public int Position { get; private set; }

        public Task<DeviceCommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
        {
            switch (command.Operation)
            {
                case "open":
                    Position = 100;
                    return Task.FromResult(new DeviceCommandResult { Success = true, ReportedStateJson = "{\"position\":100,\"state\":\"open\"}" });
                case "close":
                    Position = 0;
                    return Task.FromResult(new DeviceCommandResult { Success = true, ReportedStateJson = "{\"position\":0,\"state\":\"closed\"}" });
                case "set-position":
                    var pos = JsonSerializer.Deserialize<JsonElement>(command.ParametersJson).GetProperty("position").GetInt32();
                    Position = Math.Clamp(pos, 0, 100);
                    return Task.FromResult(new DeviceCommandResult { Success = true, ReportedStateJson = $"{{\"position\":{Position},\"state\":\"{(Position > 0 ? "open" : "closed")}\"}}" });
                default:
                    return Task.FromResult(new DeviceCommandResult { Success = false, ErrorCode = "UNSUPPORTED_OPERATION" });
            }
        }

        public Task<DeviceState> GetStateAsync(CancellationToken ct = default) =>
            Task.FromResult(new ValveState { DeviceId = "", Status = "active", Position = Position } as DeviceState);

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(true);
    }

    public record ValveState : DeviceState { public int Position { get; init; } }

    public class HumidifierRuntime : IDeviceRuntime
    {
        public string DeviceType => "humidifier";
        public string[] SupportedCapabilities => ["actuate.humidifier.on", "actuate.humidifier.off", "actuate.humidifier.set-level"];
        public int Level { get; private set; }

        public Task<DeviceCommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
        {
            switch (command.Operation)
            {
                case "on":
                    Level = 50;
                    return Task.FromResult(new DeviceCommandResult { Success = true, ReportedStateJson = "{\"level\":50,\"state\":\"on\"}" });
                case "off":
                    Level = 0;
                    return Task.FromResult(new DeviceCommandResult { Success = true, ReportedStateJson = "{\"level\":0,\"state\":\"off\"}" });
                case "set-level":
                    var lvl = JsonSerializer.Deserialize<JsonElement>(command.ParametersJson).GetProperty("level").GetInt32();
                    Level = Math.Clamp(lvl, 0, 100);
                    return Task.FromResult(new DeviceCommandResult { Success = true, ReportedStateJson = $"{{\"level\":{Level},\"state\":\"{(Level > 0 ? "on" : "off")}\"}}" });
                default:
                    return Task.FromResult(new DeviceCommandResult { Success = false, ErrorCode = "UNSUPPORTED_OPERATION" });
            }
        }

        public Task<DeviceState> GetStateAsync(CancellationToken ct = default) =>
            Task.FromResult(new HumidifierState { DeviceId = "", Status = "active", Level = Level } as DeviceState);

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(true);
    }

    public record HumidifierState : DeviceState { public int Level { get; init; } }

    public class PumpRuntime : IDeviceRuntime
    {
        public string DeviceType => "pump";
        public string[] SupportedCapabilities => ["actuate.pump.on", "actuate.pump.off", "actuate.pump.set-speed"];
        public int Speed { get; private set; }

        public Task<DeviceCommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
        {
            switch (command.Operation)
            {
                case "on": Speed = 100; return Task.FromResult(new DeviceCommandResult { Success = true });
                case "off": Speed = 0; return Task.FromResult(new DeviceCommandResult { Success = true });
                case "set-speed":
                    var speed = JsonSerializer.Deserialize<JsonElement>(command.ParametersJson).GetProperty("speed").GetInt32();
                    Speed = Math.Clamp(speed, 0, 100);
                    return Task.FromResult(new DeviceCommandResult { Success = true });
                default:
                    return Task.FromResult(new DeviceCommandResult { Success = false, ErrorCode = "UNSUPPORTED_OPERATION" });
            }
        }

        public Task<DeviceState> GetStateAsync(CancellationToken ct = default) =>
            Task.FromResult(new PumpState { DeviceId = "", Status = "active", Speed = Speed } as DeviceState);

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(true);
    }

    public record PumpState : DeviceState { public int Speed { get; init; } }

    public class HeaterRuntime : IDeviceRuntime
    {
        public string DeviceType => "heater";
        public string[] SupportedCapabilities => ["actuate.heater.on", "actuate.heater.off", "actuate.heater.set-power"];
        public int Power { get; private set; }

        public Task<DeviceCommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
        {
            switch (command.Operation)
            {
                case "on": Power = 100; return Task.FromResult(new DeviceCommandResult { Success = true });
                case "off": Power = 0; return Task.FromResult(new DeviceCommandResult { Success = true });
                case "set-power":
                    var pwr = JsonSerializer.Deserialize<JsonElement>(command.ParametersJson).GetProperty("power").GetInt32();
                    Power = Math.Clamp(pwr, 0, 100);
                    return Task.FromResult(new DeviceCommandResult { Success = true });
                default:
                    return Task.FromResult(new DeviceCommandResult { Success = false, ErrorCode = "UNSUPPORTED_OPERATION" });
            }
        }

        public Task<DeviceState> GetStateAsync(CancellationToken ct = default) =>
            Task.FromResult(new HeaterState { DeviceId = "", Status = "active", Power = Power } as DeviceState);

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(true);
    }

    public record HeaterState : DeviceState { public int Power { get; init; } }

    public class CoolerRuntime : IDeviceRuntime
    {
        public string DeviceType => "cooler";
        public string[] SupportedCapabilities => ["actuate.cooler.on", "actuate.cooler.off", "actuate.cooler.set-power"];
        public int Power { get; private set; }

        public Task<DeviceCommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
        {
            switch (command.Operation)
            {
                case "on": Power = 100; return Task.FromResult(new DeviceCommandResult { Success = true });
                case "off": Power = 0; return Task.FromResult(new DeviceCommandResult { Success = true });
                case "set-power":
                    var pwr = JsonSerializer.Deserialize<JsonElement>(command.ParametersJson).GetProperty("power").GetInt32();
                    Power = Math.Clamp(pwr, 0, 100);
                    return Task.FromResult(new DeviceCommandResult { Success = true });
                default:
                    return Task.FromResult(new DeviceCommandResult { Success = false, ErrorCode = "UNSUPPORTED_OPERATION" });
            }
        }

        public Task<DeviceState> GetStateAsync(CancellationToken ct = default) =>
            Task.FromResult(new CoolerState { DeviceId = "", Status = "active", Power = Power } as DeviceState);

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(true);
    }

    public record CoolerState : DeviceState { public int Power { get; init; } }

    public class LightingRuntime : IDeviceRuntime
    {
        public string DeviceType => "lighting";
        public string[] SupportedCapabilities => ["actuate.lighting.on", "actuate.lighting.off", "actuate.lighting.set-brightness", "actuate.lighting.set-color"];
        public int Brightness { get; private set; }
        public string Color { get; private set; } = "daylight";

        public Task<DeviceCommandResult> ExecuteAsync(DeviceCommand command, CancellationToken ct = default)
        {
            switch (command.Operation)
            {
                case "on": Brightness = 80; return Task.FromResult(new DeviceCommandResult { Success = true });
                case "off": Brightness = 0; return Task.FromResult(new DeviceCommandResult { Success = true });
                case "set-brightness":
                    var b = JsonSerializer.Deserialize<JsonElement>(command.ParametersJson).GetProperty("brightness").GetInt32();
                    Brightness = Math.Clamp(b, 0, 100);
                    return Task.FromResult(new DeviceCommandResult { Success = true });
                case "set-color":
                    Color = JsonSerializer.Deserialize<JsonElement>(command.ParametersJson).GetProperty("color").GetString() ?? "daylight";
                    return Task.FromResult(new DeviceCommandResult { Success = true });
                default:
                    return Task.FromResult(new DeviceCommandResult { Success = false, ErrorCode = "UNSUPPORTED_OPERATION" });
            }
        }

        public Task<DeviceState> GetStateAsync(CancellationToken ct = default) =>
            Task.FromResult(new LightingState { DeviceId = "", Status = "active", Brightness = Brightness, Color = Color } as DeviceState);

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(true);
    }

    public record LightingState : DeviceState { public int Brightness { get; init; } public string Color { get; init; } = ""; }

    public class DeviceRuntimeFactory
    {
        private static readonly Dictionary<string, Func<IDeviceRuntime>> _runtimes = new()
        {
            ["relay"] = () => new RelayRuntime(),
            ["fan"] = () => new FanRuntime(),
            ["damper"] = () => new DamperRuntime(),
            ["valve"] = () => new ValveRuntime(),
            ["humidifier"] = () => new HumidifierRuntime(),
            ["pump"] = () => new PumpRuntime(),
            ["heater"] = () => new HeaterRuntime(),
            ["cooler"] = () => new CoolerRuntime(),
            ["lighting"] = () => new LightingRuntime(),
        };

        public static string[] SupportedDeviceTypes => _runtimes.Keys.ToArray();

        public static IDeviceRuntime Create(string deviceType)
        {
            if (_runtimes.TryGetValue(deviceType, out var factory))
                return factory();
            throw new ArgumentException($"Unsupported device type: {deviceType}", nameof(deviceType));
        }

        public static string[] GetCapabilitiesFor(string deviceType)
        {
            if (_runtimes.TryGetValue(deviceType, out var factory))
                return factory().SupportedCapabilities;
            return [];
        }
    }

    [Fact]
    public void Relay_SupportsOnOffAndToggle()
    {
        var runtime = new RelayRuntime();
        Assert.Contains("actuate.relay.on", runtime.SupportedCapabilities);
        Assert.Contains("actuate.relay.off", runtime.SupportedCapabilities);
        Assert.Contains("actuate.relay.toggle", runtime.SupportedCapabilities);
    }

    [Fact]
    public async Task Relay_TurnOn_Success()
    {
        var runtime = new RelayRuntime();
        var result = await runtime.ExecuteAsync(new RelayCommand { Operation = "on" });
        Assert.True(result.Success);
        Assert.Contains("\"relayState\":\"on\"", result.ReportedStateJson);
    }

    [Fact]
    public async Task Relay_TurnOff_Success()
    {
        var runtime = new RelayRuntime();
        await runtime.ExecuteAsync(new RelayCommand { Operation = "on" });
        var result = await runtime.ExecuteAsync(new RelayCommand { Operation = "off" });
        Assert.True(result.Success);
        Assert.Contains("\"relayState\":\"off\"", result.ReportedStateJson);
    }

    [Fact]
    public async Task Relay_Toggle_FlipsState()
    {
        var runtime = new RelayRuntime();
        var s1 = await runtime.ExecuteAsync(new RelayCommand { Operation = "toggle" });
        Assert.Contains("\"relayState\":\"on\"", s1.ReportedStateJson);
        var s2 = await runtime.ExecuteAsync(new RelayCommand { Operation = "toggle" });
        Assert.Contains("\"relayState\":\"off\"", s2.ReportedStateJson);
    }

    [Fact]
    public async Task Relay_UnsupportedOperation_Fails()
    {
        var runtime = new RelayRuntime();
        var result = await runtime.ExecuteAsync(new RelayCommand { Operation = "set-speed" });
        Assert.False(result.Success);
        Assert.Equal("UNSUPPORTED_OPERATION", result.ErrorCode);
    }

    [Fact]
    public async Task Fan_SetSpeed_ClampsToRange()
    {
        var runtime = new FanRuntime();
        var result = await runtime.ExecuteAsync(new FanCommand { Operation = "set-speed", ParametersJson = "{\"speed\":150}" });
        Assert.True(result.Success);
        var state = (FanState)await runtime.GetStateAsync();
        Assert.Equal(100, state.Speed);
    }

    [Fact]
    public async Task Damper_SetPosition_WorksCorrectly()
    {
        var runtime = new DamperRuntime();
        var result = await runtime.ExecuteAsync(new DamperCommand { Operation = "set-position", ParametersJson = "{\"position\":75}" });
        Assert.True(result.Success);
        var state = (DamperState)await runtime.GetStateAsync();
        Assert.Equal(75, state.Position);
    }

    [Fact]
    public async Task Valve_OpenClose_Works()
    {
        var runtime = new ValveRuntime();
        await runtime.ExecuteAsync(new ValveCommand { Operation = "open" });
        var state = (ValveState)await runtime.GetStateAsync();
        Assert.Equal(100, state.Position);
        await runtime.ExecuteAsync(new ValveCommand { Operation = "close" });
        state = (ValveState)await runtime.GetStateAsync();
        Assert.Equal(0, state.Position);
    }

    [Fact]
    public async Task Humidifier_SetLevel_ClampsCorrectly()
    {
        var runtime = new HumidifierRuntime();
        await runtime.ExecuteAsync(new HumidifierCommand { Operation = "set-level", ParametersJson = "{\"level\":-10}" });
        var state = (HumidifierState)await runtime.GetStateAsync();
        Assert.Equal(0, state.Level);
    }

    [Fact]
    public async Task Pump_SetSpeed_Works()
    {
        var runtime = new PumpRuntime();
        await runtime.ExecuteAsync(new PumpCommand { Operation = "set-speed", ParametersJson = "{\"speed\":60}" });
        var state = (PumpState)await runtime.GetStateAsync();
        Assert.Equal(60, state.Speed);
    }

    [Fact]
    public async Task Heater_SetPower_Works()
    {
        var runtime = new HeaterRuntime();
        await runtime.ExecuteAsync(new HeaterCommand { Operation = "set-power", ParametersJson = "{\"power\":80}" });
        var state = (HeaterState)await runtime.GetStateAsync();
        Assert.Equal(80, state.Power);
    }

    [Fact]
    public async Task Cooler_SetPower_Works()
    {
        var runtime = new CoolerRuntime();
        await runtime.ExecuteAsync(new CoolerCommand { Operation = "set-power", ParametersJson = "{\"power\":60}" });
        var state = (CoolerState)await runtime.GetStateAsync();
        Assert.Equal(60, state.Power);
    }

    [Fact]
    public async Task Lighting_SetBrightness_Works()
    {
        var runtime = new LightingRuntime();
        await runtime.ExecuteAsync(new LightingCommand { Operation = "set-brightness", ParametersJson = "{\"brightness\":45}" });
        var state = (LightingState)await runtime.GetStateAsync();
        Assert.Equal(45, state.Brightness);
    }

    [Fact]
    public async Task Lighting_SetColor_Works()
    {
        var runtime = new LightingRuntime();
        await runtime.ExecuteAsync(new LightingCommand { Operation = "set-color", ParametersJson = "{\"color\":\"warm\"}" });
        var state = (LightingState)await runtime.GetStateAsync();
        Assert.Equal("warm", state.Color);
    }

    [Fact]
    public void DeviceRuntimeFactory_CreatesAllTypes()
    {
        var types = DeviceRuntimeFactory.SupportedDeviceTypes;
        Assert.Contains("relay", types);
        Assert.Contains("fan", types);
        Assert.Contains("damper", types);
        Assert.Contains("valve", types);
        Assert.Contains("humidifier", types);
        Assert.Contains("pump", types);
        Assert.Contains("heater", types);
        Assert.Contains("cooler", types);
        Assert.Contains("lighting", types);
    }

    [Fact]
    public void DeviceRuntimeFactory_UnknownType_Throws()
    {
        Assert.Throws<ArgumentException>(() => DeviceRuntimeFactory.Create("unknown_device"));
    }

    [Fact]
    public async Task AllDeviceRuntimes_AreConnected()
    {
        foreach (var type in DeviceRuntimeFactory.SupportedDeviceTypes)
        {
            var runtime = DeviceRuntimeFactory.Create(type);
            Assert.True(await runtime.IsConnectedAsync(), $"{type} should report connected");
        }
    }

    [Fact]
    public void AllDeviceRuntimes_HaveAtLeastOneCapability()
    {
        foreach (var type in DeviceRuntimeFactory.SupportedDeviceTypes)
        {
            var runtime = DeviceRuntimeFactory.Create(type);
            Assert.NotEmpty(runtime.SupportedCapabilities);
        }
    }

    [Fact]
    public void DeviceRuntimeFactory_CapabilitiesMatchRuntime()
    {
        foreach (var type in DeviceRuntimeFactory.SupportedDeviceTypes)
        {
            var fromFactory = DeviceRuntimeFactory.GetCapabilitiesFor(type);
            var fromRuntime = DeviceRuntimeFactory.Create(type).SupportedCapabilities;
            Assert.Equal(fromRuntime.OrderBy(c => c), fromFactory.OrderBy(c => c));
        }
    }

    [Fact]
    public async Task AllRuntimes_RejectUnknownOperations()
    {
        foreach (var type in DeviceRuntimeFactory.SupportedDeviceTypes)
        {
            var runtime = DeviceRuntimeFactory.Create(type);
            var result = await runtime.ExecuteAsync(new UnknownCommand { Operation = "unknown_operation_" + type });
            Assert.False(result.Success, $"{type} should reject unknown operation");
        }
    }

    [Fact]
    public void NoEngineeringLogic_InDeviceRuntime()
    {
        foreach (var type in DeviceRuntimeFactory.SupportedDeviceTypes)
        {
            var runtime = DeviceRuntimeFactory.Create(type);
            var hasEngLogic = runtime.GetType().GetMethods().Any(m =>
                m.Name.Contains("Calculate") || m.Name.Contains("Plan") || m.Name.Contains("Evaluate") ||
                m.Name.Contains("Aggregate") || m.Name.Contains("Optimize"));
            Assert.False(hasEngLogic, $"{type} runtime should not contain engineering logic methods");
        }
    }

    public record RelayCommand : DeviceCommand { public string DeviceType => "relay"; }
    public record FanCommand : DeviceCommand { public string DeviceType => "fan"; }
    public record DamperCommand : DeviceCommand { public string DeviceType => "damper"; }
    public record ValveCommand : DeviceCommand { public string DeviceType => "valve"; }
    public record HumidifierCommand : DeviceCommand { public string DeviceType => "humidifier"; }
    public record PumpCommand : DeviceCommand { public string DeviceType => "pump"; }
    public record HeaterCommand : DeviceCommand { public string DeviceType => "heater"; }
    public record CoolerCommand : DeviceCommand { public string DeviceType => "cooler"; }
    public record LightingCommand : DeviceCommand { public string DeviceType => "lighting"; }
    public record UnknownCommand : DeviceCommand { }
}