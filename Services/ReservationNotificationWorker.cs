using Confluent.Kafka;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using NotificationService.Models;
using System.Text.Json;

namespace NotificationService.Services;


public class ReservationNotificationWorker : BackgroundService
{
    private readonly ILogger<ReservationNotificationWorker> _logger;
    private readonly KafkaSettings _kafka;
    private readonly SmtpSettings _smtp;

    public ReservationNotificationWorker(
        ILogger<ReservationNotificationWorker> logger,
        IOptions<KafkaSettings> kafka,
        IOptions<SmtpSettings> smtp)
    {
        _logger = logger;
        _kafka = kafka.Value;
        _smtp = smtp.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafka.BootstrapServers,
            GroupId = _kafka.GroupId,
            AutoOffsetReset = Enum.Parse<AutoOffsetReset>(_kafka.AutoOffsetReset, true),
            EnablePartitionEof = false
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        consumer.Subscribe(_kafka.Topic);

        _logger.LogInformation("Notification service started. Listening to topic {Topic}", _kafka.Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                var evt = JsonSerializer.Deserialize<ReservationCreatedEvent>(result.Message.Value);

                if (evt is null) continue;
                await SendEmailAsync(evt, stoppingToken);

                consumer.Commit(result);
            }
            catch (OperationCanceledException) { /* graceful shutdown */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
            }
        }
    }

    private async Task SendEmailAsync(ReservationCreatedEvent evt, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromAddress));
        message.To.Add(MailboxAddress.Parse(evt.EmailUtilisateur));
        message.Subject = $"Confirmation de votre réservation Cocovoit";
    
        var bodyText = $"""
                        Bonjour,

                        Votre réservation est confirmée ! 

                        📍 Détails du trajet :
                        • Départ : {evt.DetailsTrajet.AdresseDepart}
                        • Arrivée : {evt.DetailsTrajet.AdresseArrivee}
                        • Date et heure : {evt.DetailsTrajet.DateHeure:dddd dd MMMM yyyy à HH:mm}
                        • Conducteur : {evt.DetailsTrajet.NomConducteur}
                        • Places disponibles : {evt.DetailsTrajet.NombrePlaces}

                        Numéro de réservation : {evt.TrajetId}

                        Si nécessaire, le conducteur vous contactera à l'adresse : {evt.EmailUtilisateur}

                        Merci d'utiliser Cocovoit 🚗

                        Bon voyage !
                        """;

        message.Body = new TextPart("plain")
        {
            Text = bodyText
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.None, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("Mail envoyé à {Email} pour réservation trajet {Id}", 
            evt.EmailUtilisateur, evt.TrajetId);
    }
}