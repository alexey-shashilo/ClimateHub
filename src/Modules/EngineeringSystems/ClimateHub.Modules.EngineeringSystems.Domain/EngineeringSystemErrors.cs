namespace ClimateHub.Modules.EngineeringSystems.Domain;

public static class EngineeringErrors
{
    public const string EngineeringSystemNotFound = "ENGINEERING_SYSTEM_NOT_FOUND";
    public const string EngineeringSystemDisabled = "ENGINEERING_SYSTEM_DISABLED";
    public const string EngineeringSystemUnavailable = "ENGINEERING_SYSTEM_UNAVAILABLE";
    public const string EngineeringCapabilityNotFound = "ENGINEERING_CAPABILITY_NOT_FOUND";
    public const string EngineeringCapabilityNotAvailable = "ENGINEERING_CAPABILITY_NOT_AVAILABLE";
    public const string ZoneNotFound = "ENGINEERING_SYSTEM_ZONE_NOT_FOUND";
    public const string RoomNotCovered = "ROOM_NOT_COVERED_BY_SYSTEM";
    public const string AmbiguousSystem = "AMBIGUOUS_ENGINEERING_SYSTEM";
    public const string StrategyNotFound = "ENGINEERING_STRATEGY_NOT_FOUND";
    public const string StrategyInvalid = "ENGINEERING_STRATEGY_INVALID";
    public const string RequiredDeviceNotFound = "REQUIRED_SYSTEM_DEVICE_NOT_FOUND";
    public const string AmbiguousDevice = "AMBIGUOUS_SYSTEM_DEVICE";
    public const string InsufficientResource = "INSUFFICIENT_ENGINEERING_RESOURCE";
    public const string ResourceReservationConflict = "RESOURCE_RESERVATION_CONFLICT";
    public const string CommandPlanNotFound = "COMMAND_PLAN_NOT_FOUND";
    public const string CommandPlanAlreadyTerminal = "COMMAND_PLAN_ALREADY_TERMINAL";
    public const string CommandPlanExecutionFailed = "COMMAND_PLAN_EXECUTION_FAILED";
    public const string CommandPlanCannotBeCancelled = "COMMAND_PLAN_CANNOT_BE_CANCELLED";
    public const string ConcurrencyConflict = "ENGINEERING_CONCURRENCY_CONFLICT";

    // HVAC-specific error codes
    public const string VentilationConfigurationInvalid = "VENTILATION_CONFIGURATION_INVALID";
    public const string VentilationZoneNotConfigured = "VENTILATION_ZONE_NOT_CONFIGURED";
    public const string SupplyFanNotFound = "SUPPLY_FAN_NOT_FOUND";
    public const string ExhaustFanNotFound = "EXHAUST_FAN_NOT_FOUND";
    public const string SupplyDamperNotFound = "SUPPLY_DAMPER_NOT_FOUND";
    public const string AirflowCapacityInsufficient = "AIRFLOW_CAPACITY_INSUFFICIENT";
    public const string AirflowBalanceUnachievable = "AIRFLOW_BALANCE_UNACHIEVABLE";
    public const string SupplyAirTemperatureUnsafe = "SUPPLY_AIR_TEMPERATURE_UNSAFE";
    public const string FrostProtectionActive = "FROST_PROTECTION_ACTIVE";
    public const string HeatRecoveryUnavailable = "HEAT_RECOVERY_UNAVAILABLE";
    public const string SupplyHeaterUnavailable = "SUPPLY_HEATER_UNAVAILABLE";
    public const string HvacSafeStopFailed = "HVAC_SAFE_STOP_FAILED";
    public const string ConflictingVentilationPlan = "CONFLICTING_VENTILATION_PLAN";
    public const string VentilationSensorDataStale = "VENTILATION_SENSOR_DATA_STALE";
    public const string OutdoorTemperatureUnavailable = "OUTDOOR_TEMPERATURE_UNAVAILABLE";

    // Thermal-specific error codes
    public const string ThermalConfigurationInvalid = "THERMAL_CONFIGURATION_INVALID";
    public const string HeatSourceNotAvailable = "HEAT_SOURCE_NOT_AVAILABLE";
    public const string HeatSourceNotConfigured = "HEAT_SOURCE_NOT_CONFIGURED";
    public const string HeatSourceOverheat = "HEAT_SOURCE_OVERHEAT";
    public const string HydraulicCircuitNotConfigured = "HYDRAULIC_CIRCUIT_NOT_CONFIGURED";
    public const string HydraulicCircuitFlowError = "HYDRAULIC_CIRCUIT_FLOW_ERROR";
    public const string MixingValveNotAvailable = "MIXING_VALVE_NOT_AVAILABLE";
    public const string PumpNotAvailable = "PUMP_NOT_AVAILABLE";
    public const string PumpDryRun = "PUMP_DRY_RUN";
    public const string SupplyTemperatureUnsafe = "SUPPLY_TEMPERATURE_UNSAFE";
    public const string SupplyTemperatureTooLow = "SUPPLY_TEMPERATURE_TOO_LOW";
    public const string ReturnTemperatureTooHigh = "RETURN_TEMPERATURE_TOO_HIGH";
    public const string FreezeProtectionActive = "FREEZE_PROTECTION_ACTIVE";
    public const string ThermalZoneNotFound = "THERMAL_ZONE_NOT_FOUND";
    public const string ThermalZoneNotConfigured = "THERMAL_ZONE_NOT_CONFIGURED";
    public const string WeatherCompensationUnavailable = "WEATHER_COMPENSATION_UNAVAILABLE";
    public const string HeatLossEstimationFailed = "HEAT_LOSS_ESTIMATION_FAILED";
    public const string DefrostCycleActive = "DEFROST_CYCLE_ACTIVE";
    public const string MinimumRuntimeNotSatisfied = "MINIMUM_RUNTIME_NOT_SATISFIED";
    public const string CooldownActive = "COOLDOWN_ACTIVE";
    public const string BufferTankUnavailable = "BUFFER_TANK_UNAVAILABLE";

    // Humidification-specific error codes
    public const string HumidificationConfigurationInvalid = "HUMIDIFICATION_CONFIGURATION_INVALID";
    public const string HumidificationZoneNotConfigured = "HUMIDIFICATION_ZONE_NOT_CONFIGURED";
    public const string HumidifierNotAvailable = "HUMIDIFIER_NOT_AVAILABLE";
    public const string SteamGeneratorNotAvailable = "STEAM_GENERATOR_NOT_AVAILABLE";
    public const string SteamGeneratorOverheat = "STEAM_GENERATOR_OVERHEAT";
    public const string SteamPressureUnsafe = "STEAM_PRESSURE_UNSAFE";
    public const string NozzlePumpNotAvailable = "NOZZLE_PUMP_NOT_AVAILABLE";
    public const string NozzleBlockage = "NOZZLE_BLOCKAGE";
    public const string WaterSupplyInsufficient = "WATER_SUPPLY_INSUFFICIENT";
    public const string WaterQualityUnsuitable = "WATER_QUALITY_UNSUITABLE";
    public const string ROUnavailable = "RO_UNAVAILABLE";
    public const string UVSterilizationFailure = "UV_STERILIZATION_FAILURE";
    public const string ConductivityOutOfRange = "CONDUCTIVITY_OUT_OF_RANGE";
    public const string WaterLeakDetected = "WATER_LEAK_DETECTED";
    public const string LowWaterLevel = "LOW_WATER_LEVEL";
    public const string FrozenPipeRisk = "FROZEN_PIPE_RISK";
    public const string CondensationRiskDetected = "CONDENSATION_RISK_DETECTED";
    public const string DewPointExceeded = "DEW_POINT_EXCEEDED";
    public const string SurfaceTemperatureCritical = "SURFACE_TEMPERATURE_CRITICAL";
    public const string HumidificationEffectNotObserved = "HUMIDIFICATION_EFFECT_NOT_OBSERVED";
    public const string SanitaryCycleOverdue = "SANITARY_CYCLE_OVERDUE";
    public const string StandingWaterTimeout = "STANDING_WATER_TIMEOUT";
    public const string DrainFailure = "DRAIN_FAILURE";
    public const string FlushFailure = "FLUSH_FAILURE";

    // Lighting & Shading-specific error codes
    public const string LightingConfigurationInvalid = "LIGHTING_CONFIGURATION_INVALID";
    public const string LightingZoneNotConfigured = "LIGHTING_ZONE_NOT_CONFIGURED";
    public const string LightingControllerOffline = "LIGHTING_CONTROLLER_OFFLINE";
    public const string DaliGatewayOffline = "DALI_GATEWAY_OFFLINE";
    public const string DimmerNotAvailable = "DIMMER_NOT_AVAILABLE";
    public const string SceneNotFound = "SCENE_NOT_FOUND";
    public const string SceneInvalid = "SCENE_INVALID";
    public const string OccupancySensorFailure = "OCCUPANCY_SENSOR_FAILURE";
    public const string BlindMotorJam = "BLIND_MOTOR_JAM";
    public const string BlindBlocked = "BLIND_BLOCKED";
    public const string CurtainBlocked = "CURTAIN_BLOCKED";
    public const string CurtainMotorJam = "CURTAIN_MOTOR_JAM";
    public const string MotorOvercurrent = "MOTOR_OVERCURRENT";
    public const string ShadingProtectionActive = "SHADING_PROTECTION_ACTIVE";
    public const string WindowOpenProtectionActive = "WINDOW_OPEN_PROTECTION_ACTIVE";
    public const string EmergencyLightingActive = "EMERGENCY_LIGHTING_ACTIVE";
    public const string FireOverrideActive = "FIRE_OVERRIDE_ACTIVE";
    public const string SolarPositionUnavailable = "SOLAR_POSITION_UNAVAILABLE";
    public const string DaylightHarvestingUnavailable = "DAYLIGHT_HARVESTING_UNAVAILABLE";
}