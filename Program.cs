using NotificationService;
using NotificationService.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configuration pour Docker
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Services
builder.Services.AddHostedService<Worker>();

// Bind configuration sections
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));

// Background service
builder.Services.AddHostedService<ReservationNotificationWorker>();

// Logging
builder.Logging.ClearProviders().AddConsole();

var host = builder.Build();
host.Run();