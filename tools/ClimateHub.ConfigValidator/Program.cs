using System.Text.Json;

namespace ClimateHub.ConfigValidator;

internal static class Program
{
    private static int _errors;

    public static int Main(string[] args)
    {
        var configPath = args.ElementAtOrDefault(0) ?? "appsettings.json";
        if (!File.Exists(configPath))
        {
            Console.Error.WriteLine("Configuration file not found: {ConfigPath}", configPath);
            return 1;
        }

        var json = File.ReadAllText(configPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        ValidateRequired(root, "Postgres", "ConnectionString");
        ValidateRequired(root, "Jwt", "SigningKey");
        ValidateJwtSigningKey(root);
        ValidateCors(root);
        ValidateMqtt(root);
        ValidateBatchSizes(root);
        ValidateIntervals(root);
        ValidateLeaseTimeout(root);

        if (_errors > 0)
        {
            Console.Error.WriteLine("{ErrorCount} configuration validation error(s) found", _errors);
            return 1;
        }

        Console.WriteLine("All configuration checks passed");
        return 0;
    }

    private static void ValidateRequired(JsonElement root, string section, string key)
    {
        var value = GetNestedValue(root, section, key);
        if (value is not { ValueKind: JsonValueKind.String } s || string.IsNullOrWhiteSpace(s.GetString()))
        {
            Fail("{Section}:{Key} is required but missing or empty", section, key);
        }
        else if (s.GetString()!.Contains("SET_VIA_ENVIRONMENT", StringComparison.OrdinalIgnoreCase))
        {
            Fail("{Section}:{Key} still contains placeholder value", section, key);
        }
    }

    private static void ValidateJwtSigningKey(JsonElement root)
    {
        var key = GetNestedValue(root, "Jwt", "SigningKey")?.GetString();
        if (key != null && key.Length < 32 && !key.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
        {
            Fail("Jwt:SigningKey must be at least 32 characters long (current: {Length})", key.Length);
        }
    }

    private static void ValidateCors(JsonElement root)
    {
        var origins = GetNestedValue(root, "Cors", "AllowedOrigins");
        if (origins is { ValueKind: JsonValueKind.Array } arr)
        {
            foreach (var origin in arr.EnumerateArray())
            {
                if (string.Equals(origin.GetString(), "*", StringComparison.OrdinalIgnoreCase))
                {
                    Fail("Cors:AllowedOrigins contains wildcard '*' which is not allowed in production");
                }
            }
        }
        else
        {
            Fail("Cors:AllowedOrigins is missing or not an array");
        }
    }

    private static void ValidateMqtt(JsonElement root)
    {
        var username = GetNestedValue(root, "Mqtt", "Username")?.GetString();
        var password = GetNestedValue(root, "Mqtt", "Password")?.GetString();

        if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(password))
        {
            Fail("Mqtt:Username and Mqtt:Password are both empty — anonymous connections are not allowed in production");
        }
    }

    private static void ValidateBatchSizes(JsonElement root)
    {
        ValidateNumericGtZero(root, "InternalEvents", "OutboxBatchSize");
        ValidateNumericGtZero(root, "InternalEvents", "InboxBatchSize");
    }

    private static void ValidateIntervals(JsonElement root)
    {
        ValidateNumericGtZero(root, "InternalEvents", "OutboxPollingIntervalMs");
        ValidateNumericGtZero(root, "InternalEvents", "InboxPollingIntervalMs");
    }

    private static void ValidateLeaseTimeout(JsonElement root)
    {
        var lease = GetNestedValue(root, "InternalEvents", "LeaseTimeoutMs");
        var processing = GetNestedValue(root, "InternalEvents", "ProcessingTimeoutMs");

        if (lease is { ValueKind: JsonValueKind.Number } l &&
            processing is { ValueKind: JsonValueKind.Number } p)
        {
            if (l.GetInt64() <= p.GetInt64())
            {
                Fail("InternalEvents:LeaseTimeoutMs ({Lease}) must be greater than ProcessingTimeoutMs ({Processing})",
                    l.GetInt64(), p.GetInt64());
            }
        }
    }

    private static void ValidateNumericGtZero(JsonElement root, string section, string key)
    {
        var value = GetNestedValue(root, section, key);
        if (value is { ValueKind: JsonValueKind.Number } n)
        {
            var num = n.GetInt64();
            if (num <= 0)
            {
                Fail("{Section}:{Key} must be greater than 0 (current: {Value})", section, key, num);
            }
        }
    }

    private static JsonElement? GetNestedValue(JsonElement root, string section, string key)
    {
        if (!root.TryGetProperty(section, out var sectionElement))
            return null;
        return sectionElement.TryGetProperty(key, out var value) ? value : null;
    }

    private static void Fail(string message, params object?[] args)
    {
        _errors++;
        Console.Error.WriteLine(message, args);
    }
}
