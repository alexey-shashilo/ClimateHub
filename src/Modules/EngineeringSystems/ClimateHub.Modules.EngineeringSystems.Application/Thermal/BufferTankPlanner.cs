using ClimateHub.Modules.EngineeringSystems.Domain.Thermal;

namespace ClimateHub.Modules.EngineeringSystems.Application.Thermal;

public class BufferPlan
{
    public bool UseBuffer { get; init; }
    public bool ChargeBuffer { get; init; }
    public string Action { get; init; } = "Direct";
    public double EnergyAvailableKwh { get; init; }
    public string Quality { get; init; } = "Estimated";
}

public class BufferTankPlanner
{
    public BufferPlan Plan(BufferTank? buffer, double requiredPowerKw,
        bool sourceMustRunForMinRuntime, double excessCapacityKw)
    {
        if (buffer is null || buffer.ChargeStatus == ChargeStatus.Unavailable || buffer.ChargeStatus == ChargeStatus.Faulted)
            return new BufferPlan { Action = "Direct", Quality = "Unavailable" };

        if (buffer.StateOfChargePct > 50 && requiredPowerKw < 5)
        {
            return new BufferPlan
            {
                UseBuffer = true,
                ChargeBuffer = false,
                Action = "Discharging",
                EnergyAvailableKwh = buffer.EstimatedStoredEnergyKwh ?? 0,
                Quality = "Estimated"
            };
        }

        if (sourceMustRunForMinRuntime && excessCapacityKw > 0)
        {
            return new BufferPlan
            {
                UseBuffer = true,
                ChargeBuffer = true,
                Action = "Charging",
                EnergyAvailableKwh = buffer.EstimatedStoredEnergyKwh ?? 0,
                Quality = "Estimated"
            };
        }

        return new BufferPlan { Action = "Direct", Quality = "Estimated" };
    }

    public double EstimateStoredEnergy(BufferTank tank)
    {
        var avgTemp = ((tank.CurrentTopTemperatureC ?? 0) + (tank.CurrentBottomTemperatureC ?? 0)) / 2;
        var deltaT = avgTemp - 10;
        if (deltaT <= 0) return 0;
        return tank.VolumeLiters * 4.186 / 3600 * deltaT;
    }
}
