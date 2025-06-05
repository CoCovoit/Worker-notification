namespace NotificationService.Services;

public record KafkaSettings
{
    public string BootstrapServers { get; init; } = default!;
    public string Topic { get; init; } = default!;
    public string GroupId { get; init; } = default!;
    public string AutoOffsetReset { get; init; } = "Earliest";
}