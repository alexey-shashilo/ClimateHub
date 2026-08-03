using ClimateHub.Modules.Commands.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Workers.UnitTests;

public class CommandOutboxWorkerTests
{
    [Fact]
    public void CommandOutbox_DefaultStatus_IsPending()
    {
        var outbox = new CommandOutbox
        {
            Id = 1,
            CommandId = CommandId.New(),
            DeviceId = DeviceId.From(Guid.NewGuid()),
            Topic = "test/topic",
            Payload = "{}",
            CreatedAt = DateTimeOffset.UtcNow
        };

        Assert.Equal("Pending", outbox.Status);
    }

    [Fact]
    public void CommandOutbox_CanTransitionToProcessing()
    {
        var outbox = new CommandOutbox
        {
            Id = 1,
            CommandId = CommandId.New(),
            DeviceId = DeviceId.From(Guid.NewGuid()),
            Topic = "test/topic",
            Payload = "{}",
            Status = "Pending",
            CreatedAt = DateTimeOffset.UtcNow
        };

        outbox.Status = "Processing";
        outbox.ProcessingStartedAt = DateTimeOffset.UtcNow;

        Assert.Equal("Processing", outbox.Status);
        Assert.NotNull(outbox.ProcessingStartedAt);
    }

    [Fact]
    public void CommandOutbox_MarkPublished_SetsStatus()
    {
        var outbox = new CommandOutbox
        {
            Id = 1,
            CommandId = CommandId.New(),
            DeviceId = DeviceId.From(Guid.NewGuid()),
            Topic = "test/topic",
            Payload = "{}",
            Status = "Pending",
            CreatedAt = DateTimeOffset.UtcNow
        };

        outbox.Status = "Published";
        outbox.PublishedAt = DateTimeOffset.UtcNow;

        Assert.Equal("Published", outbox.Status);
    }

    [Fact]
    public void CommandOutbox_MarkRetryable_SetsFailureCode()
    {
        var outbox = new CommandOutbox
        {
            Id = 1,
            CommandId = CommandId.New(),
            DeviceId = DeviceId.From(Guid.NewGuid()),
            Topic = "test/topic",
            Payload = "{}",
            Status = "Pending",
            CreatedAt = DateTimeOffset.UtcNow
        };

        outbox.Status = "Retryable";
        outbox.LastFailureCode = "PUBLISH_FAILED";

        Assert.Equal("Retryable", outbox.Status);
        Assert.Equal("PUBLISH_FAILED", outbox.LastFailureCode);
    }
}