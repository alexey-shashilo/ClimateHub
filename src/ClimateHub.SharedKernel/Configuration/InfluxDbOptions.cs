using System.ComponentModel.DataAnnotations;

namespace ClimateHub.SharedKernel.Configuration;

public class InfluxDbOptions
{
    public const string SectionName = "InfluxDb";

    [Required(AllowEmptyStrings = false)]
    public string Url { get; set; } = "http://localhost:8086";

    public string Token { get; set; } = "climate-hub-dev-token";

    [Required(AllowEmptyStrings = false)]
    public string Organization { get; set; } = "climate-hub";

    [Required(AllowEmptyStrings = false)]
    public string Bucket { get; set; } = "climate-hub";
}