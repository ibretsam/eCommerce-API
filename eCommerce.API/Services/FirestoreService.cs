using System;
using eCommerce.API.Middleware.Exceptions;
using eCommerce.API.Models;
using Google.Cloud.Firestore;

namespace eCommerce.API.Services;

public interface IFirestoreService
{
    CollectionReference Products { get; }
    CollectionReference Users { get; }
    Task<Product> GetProductById(string id);
    Task<Product> CreateProduct(Product product);
    Task<Product> UpdateProduct(string id, Product product);
    Task<bool> DeleteProduct(string id);
    Task<List<Product>> GetAllProducts();
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

    public async Task<Product> CreateProduct(Product product)
    {
        var docRef = Products.Document();
        product.Id = docRef.Id;
        await docRef.SetAsync(product);
        return product;
    }

    public Task<bool> DeleteProduct(string id)
    {
        var docRef = Products.Document(id);
        return docRef.DeleteAsync().ContinueWith(task => task.IsCompletedSuccessfully);
    }

    public async Task<List<Product>> GetAllProducts()
    {
        var snapshot = await Products.GetSnapshotAsync();
        return snapshot.Documents.Select(doc => doc.ConvertTo<Product>()).ToList();
    }

    public async Task<Product> GetProductById(string id)
    {
        var docRef = Products.Document(id);
        var snapshot = await docRef.GetSnapshotAsync();
        if (!snapshot.Exists)
            throw new ProductNotFoundException(id);
        return snapshot.ConvertTo<Product>();
    }

    public async Task<Product> UpdateProduct(string id, Product product)
    {
        var docRef = Products.Document(id);
        await docRef.SetAsync(product);
        return product;
    }
}
