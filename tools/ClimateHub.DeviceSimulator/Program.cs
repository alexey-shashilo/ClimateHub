using System.Diagnostics;
using ClimateHub.DeviceSimulator;

var stopwatch = Stopwatch.StartNew();
try
{
    if (args.Contains("--actuator"))
    {
        await using var sim = new ActuatorSimulatorHost(args);
        await sim.RunAsync();
    }
    else
    {
        await using var sim = new SimulatorHost(args);
        await sim.RunAsync();
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Simulator failed: {ex.Message}");
    return 1;
}
finally
{
    Console.WriteLine($"Exited after {stopwatch.Elapsed}");
}
return 0;