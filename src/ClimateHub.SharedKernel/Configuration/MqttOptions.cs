using System.ComponentModel.DataAnnotations;

namespace ClimateHub.SharedKernel.Configuration;

public class MqttOptions
{
    public const string SectionName = "Mqtt";

    [Required(AllowEmptyStrings = false)]
    public string Host { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; set; } = 1883;

    public string? ClientId { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }

    public bool UseTls { get; set; }
    public int ReconnectBaseDelayMs { get; set; } = 1000;
    public int ReconnectMaxDelayMs { get; set; } = 60000;
}
