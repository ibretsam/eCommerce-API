using Google.Cloud.Firestore;

namespace eCommerce.API.Models;

[FirestoreData]
public class Product
{
    [FirestoreDocumentId]
    public required string Id { get; set; }

    [FirestoreProperty]
    public required string Name { get; set; }

    [FirestoreProperty]
    public string? Description { get; set; }

    [FirestoreProperty]
    public double Price { get; set; }

    [FirestoreProperty]
    public string? PictureUrl { get; set; }

    [FirestoreProperty]
    public string? ProductType { get; set; }
}