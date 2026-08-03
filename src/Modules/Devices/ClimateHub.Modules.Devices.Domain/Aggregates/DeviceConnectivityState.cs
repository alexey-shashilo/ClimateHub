namespace ClimateHub.Modules.Devices.Domain.Aggregates;

public enum DeviceConnectivityState
{
    Offline,
    Online,
    Suspected,
    Synchronizing,
    Operational
}