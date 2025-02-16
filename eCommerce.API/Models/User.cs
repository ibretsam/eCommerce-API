using Google.Cloud.Firestore;

namespace eCommerce.API.Models;

[FirestoreData]
public class User
{
    [FirestoreDocumentId]
    public string Id { get; set; } = default!;

    [FirestoreProperty]
    public required string Email { get; set; }

    [FirestoreProperty]
    public string? DisplayName { get; set; }

    [FirestoreProperty]
    public string? PhotoUrl { get; set; }

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [FirestoreProperty]
    public DateTime? LastLoginAt { get; set; }

    [FirestoreProperty]
    public string? PhoneNumber { get; set; }

    [FirestoreProperty]
    public bool IsActive { get; set; } = true;
}