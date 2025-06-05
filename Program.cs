using NotificationService;
using NotificationService.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

// Bind configuration sections
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));

// Background service
builder.Services.AddHostedService<ReservationNotificationWorker>();

// Logging (Serilog, console, etc.) – optionnel
builder.Logging.ClearProviders().AddConsole();

var host = builder.Build();
host.Run();
