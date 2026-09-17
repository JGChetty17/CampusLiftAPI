namespace CampusLift.API.DTOs
{
    public record SyncUserRequest(
        string FirebaseUid,
        string? Email,
        string? Name,
        string? Surname
    );

    public record UpdateUserRequest(
        string? Name,
        string? Surname,
        string? StudentNumber,
        string? University,
        string? ProfilePicture,
        string? EmergencyContact,
        string? Language,
        bool? DarkMode,
        bool? BiometricEnabled,
        bool? NotificationEnabled
    );
}