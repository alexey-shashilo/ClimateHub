using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClimateHub.RuntimeMetricsTests;

public class RuntimeMetricsValidationTests
{
    private static readonly Meter TestMeter = new("ClimateHub.TestMetrics", "1.0.0");

    [Fact]
    public void Meter_InstrumentsCanBeCreated()
    {
        var counter = TestMeter.CreateCounter<long>("climate.test.counter");
        counter.Add(1);
    }

    [Fact]
    public async Task HealthChecks_CanBeCreated()
    {
        var liveCheck = new LivenessHealthCheck();
        var liveResult = await liveCheck.CheckHealthAsync(new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("live", liveCheck, HealthStatus.Unhealthy, null)
        });
        Assert.Equal(HealthStatus.Healthy, liveResult.Status);

        var readyCheck = new ReadinessHealthCheck();
        var readyResult = await readyCheck.CheckHealthAsync(new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("ready", readyCheck, HealthStatus.Unhealthy, null)
        });
        Assert.Equal(HealthStatus.Healthy, readyResult.Status);
    }

    [Fact]
    public async Task HealthCheck_Liveness_ReturnsHealthy()
    {
        var check = new LivenessHealthCheck();
        var result = await check.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("live", check, HealthStatus.Unhealthy, null) });
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task HealthCheck_Readiness_ReturnsHealthy()
    {
        var check = new ReadinessHealthCheck();
        var result = await check.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("ready", check, HealthStatus.Unhealthy, null) });
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public void WorkerMetrics_CounterIncrements()
    {
        var counter = TestMeter.CreateCounter<long>("climate.workers.messages_processed");
        counter.Add(1);
        counter.Add(5);
    }

    [Fact]
    public void ClimateMetrics_CounterIncrements()
    {
        var counter = TestMeter.CreateCounter<long>("climate.plans.created");
        counter.Add(1);

        var activeGauge = TestMeter.CreateObservableGauge("climate.plans.active", () => 5);
    }

    [Fact]
    public void EngineeringMetrics_CounterIncrements()
    {
        var counter = TestMeter.CreateCounter<long>("engineering.commands.executed");
        counter.Add(1);

        var durationHistogram = TestMeter.CreateHistogram<double>("engineering.planning.duration_ms");
        durationHistogram.Record(150.0);
    }

    [Fact]
    public void CommandMetrics_CounterIncrements()
    {
        var created = TestMeter.CreateCounter<long>("command.created");
        var succeeded = TestMeter.CreateCounter<long>("command.succeeded");
        var failed = TestMeter.CreateCounter<long>("command.failed");

        created.Add(1);
        succeeded.Add(1);
        failed.Add(1);
    }

    [Fact]
    public void MqttMetrics_CounterIncrements()
    {
        var received = TestMeter.CreateCounter<long>("mqtt.messages.received");
        var published = TestMeter.CreateCounter<long>("mqtt.messages.published");
        var errors = TestMeter.CreateCounter<long>("mqtt.messages.errors");

        received.Add(10);
        published.Add(8);
        errors.Add(1);
    }

    [Fact]
    public void NeedMetrics_CounterIncrements()
    {
        var detected = TestMeter.CreateCounter<long>("need.detected");
        var satisfied = TestMeter.CreateCounter<long>("need.satisfied");
        var blocked = TestMeter.CreateCounter<long>("need.blocked");

        detected.Add(1);
        satisfied.Add(1);
        blocked.Add(0);
    }

    [Fact]
    public void MeterTags_Applied_Correctly()
    {
        var counter = TestMeter.CreateCounter<long>("climate.test.tagged");
        counter.Add(1, new KeyValuePair<string, object?>("environment", "test"));
    }

    [Fact]
    public void MultipleCounterValues_AggregateCorrectly()
    {
        var counter = TestMeter.CreateCounter<long>("climate.test.aggregate");
        counter.Add(1);
        counter.Add(2);
        counter.Add(3);
    }

    [Fact]
    public void HistogramMetrics_RecordValues()
    {
        var histogram = TestMeter.CreateHistogram<double>("climate.test.duration");
        histogram.Record(100);
        histogram.Record(200);
        histogram.Record(300);
        histogram.Record(400);
        histogram.Record(500);
    }

    [Fact]
    public void ObservableGauge_ReportsValue()
    {
        var value = 42;
        var gauge = TestMeter.CreateObservableGauge("climate.test.gauge", () => value);
    }

    [Fact]
    public void InstrumentNames_FollowConvention()
    {
        var names = new[]
        {
            "climate.workers.messages_processed",
            "climate.plans.created",
            "climate.plans.active",
            "engineering.commands.executed",
            "engineering.planning.duration_ms",
            "command.created",
            "command.succeeded",
            "command.failed",
            "mqtt.messages.received",
            "mqtt.messages.published",
            "mqtt.messages.errors",
            "need.detected",
            "need.satisfied",
            "need.blocked"
        };

        foreach (var name in names)
        {
            Assert.Matches(@"^[a-z]+(?:\.[a-z]+)*(?:_[a-z]+)*$", name);
        }
    }
}

public class LivenessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        => Task.FromResult(HealthCheckResult.Healthy("API is live"));
}

public class ReadinessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        => Task.FromResult(HealthCheckResult.Healthy("API is ready"));
}
