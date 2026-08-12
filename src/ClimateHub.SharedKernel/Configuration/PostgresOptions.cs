using System.ComponentModel.DataAnnotations;

namespace ClimateHub.SharedKernel.Configuration;

public class PostgresOptions
{
    public const string SectionName = "Postgres";

    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = string.Empty;
}
