using ClimateHub.Modules.EngineeringSystems.Domain.Thermal;

namespace ClimateHub.Modules.EngineeringSystems.Application.Thermal;

public class DHWPlan
{
    public bool DHWDemandActive { get; init; }
    public bool DHWPriorityActive { get; init; }
    public string PriorityMode { get; init; } = "Balanced";
    public string Action { get; init; } = "None";
    public double RequiredHeatingPowerKw { get; init; }
}

public class DHWPlanner
{
    public DHWPlan Plan(DomesticHotWaterSystem? dhw, double outdoorTemp)
    {
        if (dhw is null)
            return new DHWPlan { Action = "None" };

        var isBelowMin = dhw.CurrentTemperatureC.HasValue && dhw.CurrentTemperatureC.Value < dhw.MinimumTemperatureC;
        if (!isBelowMin)
            return new DHWPlan { Action = "None", DHWDemandActive = false };

        var currentTempC = dhw.CurrentTemperatureC ?? dhw.MinimumTemperatureC;
        if (dhw.PriorityMode == "DhwPriority")
        {
            return new DHWPlan
            {
                DHWDemandActive = true,
                DHWPriorityActive = true,
                PriorityMode = "DhwPriority",
                Action = "Heating",
                RequiredHeatingPowerKw = dhw.StorageVolumeLiters * 4.186 / 3600 * 
                    (dhw.TargetTemperatureC - currentTempC)
            };
        }

        return new DHWPlan
        {
            DHWDemandActive = true,
            DHWPriorityActive = false,
            PriorityMode = "Balanced",
            Action = "Shared",
            RequiredHeatingPowerKw = dhw.StorageVolumeLiters * 4.186 / 3600 * 
                (dhw.TargetTemperatureC - currentTempC)
        };
    }
}