namespace NotificationService.Models;

public record ReservationCreatedEvent(
    long TrajetId,
    long UtilisateurId,
    string EmailUtilisateur,
    DateTime DateReservation,
    string DetailsTrajet
);