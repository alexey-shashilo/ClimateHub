using System.Diagnostics;

namespace ClimateHub.Infrastructure.Observability;

public static class Diagnostics
{
    public const string ServiceName = "ClimateHub";
    public const string ServiceVersion = "1.0.0";

    public static readonly ActivitySource ActivitySource = new(ServiceName, ServiceVersion);

    public static class Operations
    {
        public const string MqttMessageReceived = "mqtt.message.received";
        public const string MqttMessageProcessed = "mqtt.message.processed";
        public const string EnvironmentTelemetryIngested = "environment.telemetry.ingested";
        public const string RoomStateUpdated = "room.state.updated";
        public const string InfluxDbWrite = "influxdb.write";
    }

    public static class Tags
    {
        public const string MessageId = "climate.message_id";
        public const string BuildingId = "climate.building_id";
        public const string DeviceId = "climate.device_id";
        public const string RoomId = "climate.room_id";
        public const string BootId = "climate.boot_id";
        public const string SequenceNumber = "climate.sequence_number";
        public const string ProtocolVersion = "climate.protocol_version";
        public const string Quality = "climate.quality";
        public const string ErrorCode = "climate.error_code";
    }

    public static class Meter
    {
        public const string Name = "ClimateHub.Telemetry";
        public const string MessagesReceived = "climate.messages.received";
        public const string MessagesProcessed = "climate.messages.processed";
        public const string MessagesDuplicate = "climate.messages.duplicate";
        public const string MessagesRejected = "climate.messages.rejected";
        public const string InfluxDbWriteErrors = "climate.influxdb.write_errors";
        public const string ProcessingLatency = "climate.processing.latency";
    }
}
