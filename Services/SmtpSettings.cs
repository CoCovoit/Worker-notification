namespace NotificationService.Services;

public record SmtpSettings
{
    public string Host { get; init; } = default!;
    public int Port { get; init; } = 1025;
    public string FromName { get; init; } = default!;
    public string FromAddress { get; init; } = default!;
}