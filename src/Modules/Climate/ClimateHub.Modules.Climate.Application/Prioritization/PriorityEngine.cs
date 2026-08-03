using ClimateHub.Modules.Climate.Domain.ClimateGoals;

namespace ClimateHub.Modules.Climate.Application.Prioritization;

public enum PriorityLevel { Critical, High, Medium, Low, Optional }

public static class ClimatePriorityCategories
{
    public const string Safety = "Safety";
    public const string EquipmentProtection = "EquipmentProtection";
    public const string IndoorAirQuality = "IndoorAirQuality";
    public const string Temperature = "Temperature";
    public const string Humidity = "Humidity";
    public const string Comfort = "Comfort";
    public const string EnergySaving = "EnergySaving";
}

public class PriorityEngine
{
    private static readonly Dictionary<string, int> CategoryOrder = new()
    {
        [ClimatePriorityCategories.Safety] = 0,
        [ClimatePriorityCategories.EquipmentProtection] = 1,
        [ClimatePriorityCategories.IndoorAirQuality] = 2,
        [ClimatePriorityCategories.Temperature] = 3,
        [ClimatePriorityCategories.Humidity] = 4,
        [ClimatePriorityCategories.Comfort] = 5,
        [ClimatePriorityCategories.EnergySaving] = 6,
    };

    public string GetCapabilityCategory(string capabilityCode) => capabilityCode switch
    {
        "eng.co2.reduce" => ClimatePriorityCategories.IndoorAirQuality,
        "eng.airflow.increase" => ClimatePriorityCategories.IndoorAirQuality,
        "eng.temperature.increase" or "eng.temperature.decrease" => ClimatePriorityCategories.Temperature,
        "eng.humidity.increase" or "eng.humidity.decrease" => ClimatePriorityCategories.Humidity,
        "eng.illuminance.increase" or "eng.illuminance.decrease" => ClimatePriorityCategories.Comfort,
        _ => ClimatePriorityCategories.Comfort
    };

    public int GetCategoryPriority(string category) =>
        CategoryOrder.GetValueOrDefault(category, 99);

    public List<string> SortByPriority(List<string> capabilityCodes, StrategyProfile profile)
    {
        var profileAdjustment = profile switch
        {
            StrategyProfile.EnergySaving => new Dictionary<string, int> { [ClimatePriorityCategories.EnergySaving] = -3, [ClimatePriorityCategories.Comfort] = 2 },
            StrategyProfile.MaximumAirQuality => new Dictionary<string, int> { [ClimatePriorityCategories.IndoorAirQuality] = -2, [ClimatePriorityCategories.EnergySaving] = 3 },
            StrategyProfile.Sleep => new Dictionary<string, int> { [ClimatePriorityCategories.Comfort] = 1, [ClimatePriorityCategories.Humidity] = -1 },
            _ => new Dictionary<string, int>()
        };

        return capabilityCodes
            .OrderBy(c => GetCategoryPriority(GetCapabilityCategory(c)) +
                          profileAdjustment.GetValueOrDefault(GetCapabilityCategory(c), 0))
            .ToList();
    }

    public bool IsHigherPriorityThan(string capabilityA, string capabilityB, StrategyProfile profile)
    {
        var sorted = SortByPriority(new List<string> { capabilityA, capabilityB }, profile);
        return sorted[0] == capabilityA;
    }
}