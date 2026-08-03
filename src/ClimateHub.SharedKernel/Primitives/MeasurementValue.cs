namespace ClimateHub.SharedKernel.Primitives;

public readonly record struct TemperatureC
{
    public double Value { get; }

    public TemperatureC(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException("Temperature must be a finite number", nameof(value));
        if (value < -50 || value > 70)
            throw new ArgumentOutOfRangeException(nameof(value), "Temperature must be between -50 and 70 °C");
        Value = Math.Round(value, 1);
    }

    public static TemperatureC From(double value) => new(value);
    public override string ToString() => $"{Value:F1} °C";
}

public readonly record struct RelativeHumidityPct
{
    public double Value { get; }

    public RelativeHumidityPct(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException("Humidity must be a finite number", nameof(value));
        if (value < 0 || value > 100)
            throw new ArgumentOutOfRangeException(nameof(value), "Relative humidity must be between 0 and 100 %");
        Value = Math.Round(value, 1);
    }

    public static RelativeHumidityPct From(double value) => new(value);
    public override string ToString() => $"{Value:F1} %";
}

public readonly record struct IlluminanceLux
{
    public double Value { get; }

    public IlluminanceLux(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException("Illuminance must be a finite number", nameof(value));
        if (value < 0 || value > 200000)
            throw new ArgumentOutOfRangeException(nameof(value), "Illuminance must be between 0 and 200000 lux");
        Value = Math.Round(value, 0);
    }

    public static IlluminanceLux From(double value) => new(value);
    public override string ToString() => $"{Value:F0} lx";
}

public readonly record struct Co2Ppm
{
    public double Value { get; }

    public Co2Ppm(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException("CO2 must be a finite number", nameof(value));
        if (value < 0 || value > 10000)
            throw new ArgumentOutOfRangeException(nameof(value), "CO2 must be between 0 and 10000 ppm");
        Value = Math.Round(value, 0);
    }

    public static Co2Ppm From(double value) => new(value);
    public override string ToString() => $"{Value:F0} ppm";
}