using System;
using Google.Cloud.Firestore;

namespace eCommerce.API.Services;

public interface IFirestoreService
{
    CollectionReference Products { get; }
    CollectionReference Users { get; }
}
public class FirestoreService : IFirestoreService
{
    private readonly FirestoreDb _db;
    private readonly ILogger<FirestoreService> _logger;

    public FirestoreService(IConfiguration configuration, ILogger<FirestoreService> logger)
    {
        _logger = logger;
        try
        {
            string projectId = configuration["Firebase:ProjectId"] ??
                throw new ArgumentException("Firebase:ProjectId configuration is required");
            _logger.LogInformation("Initializing Firestore with project ID: {ProjectId}", projectId);
            _db = FirestoreDb.Create(projectId);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to initialize Firestore");
            throw;
        }
    }

    public CollectionReference Products => _db.Collection("Products");
    public CollectionReference Users => _db.Collection("Users");
}
