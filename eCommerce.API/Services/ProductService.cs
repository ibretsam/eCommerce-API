using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using eCommerce.API.Middleware.Exceptions;
using eCommerce.API.Models;
using eCommerce.API.Models.DTOs;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using ValidationException = eCommerce.API.Middleware.Exceptions.ValidationException;

namespace eCommerce.API.Services
{
    public class ProductService
    {
        private readonly IFirestoreService _firestore;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IFirestoreService firestore, ILogger<ProductService> logger)
        {
            _firestore = firestore;
            _logger = logger;
        }

        public async Task SeedSampleProducts()
        {
            _logger.LogInformation("Seeding sample products into Firestore");
            var snapshot = await _firestore.Products.Limit(1).GetSnapshotAsync();
            if (snapshot.Count > 0)
                return;

            var batch = _firestore.Products.Database.StartBatch();

            var products = new[]
            {
                new Dictionary<string, object>
                {
                    { "Name", "Nike Air Max 270" },
                    { "Description", "Men's Running Shoes with Air cushioning" },
                    { "Price", 150.0 },
                    { "PictureUrl", "https://static.nike.com/a/images/t_PDP_1728_v1/f_auto,q_auto:eco/covmdnim1rkbkbxtm23v/NIKE+AIR+MAX+270+%28GS%29.png" },
                    { "ProductType", "Footwear" }
                },
                new Dictionary<string, object>
                {
                    { "Name", "Samsung Galaxy S25" },
                    { "Description", "5G Smartphone with 6.2\" Display" },
                    { "Price", 799.0 },
                    { "PictureUrl", "https://images.samsung.com/vn/smartphones/galaxy-s25/buy/kv_comparison_Inch_PC.jpg?imbypass=true" },
                    { "ProductType", "Electronics" }
                },
                new Dictionary<string, object>
                {
                    { "Name", "Levi's 501 Original" },
                    { "Description", "Classic straight fit jeans" },
                    { "Price", 69.50 },
                    { "PictureUrl", "https://lsco.scene7.com/is/image/lsco/005010101-front-pdp-ld?fmt=jpeg&qlt=70&resMode=sharp2&fit=crop,1&op_usm=0.6,0.6,8&wid=880&hei=880" },
                    { "ProductType", "Apparel" }
                },
                new Dictionary<string, object>
                {
                    { "Name", "Sony WH-1000XM5" },
                    { "Description", "Wireless Noise Cancelling Headphones" },
                    { "Price", 349.99 },
                    { "PictureUrl", "https://www.sony.com.vn/image/1faff1a8d2f9b518cb2ef53f2c1d5af3?fmt=png-alpha&wid=1440" },
                    { "ProductType", "Electronics" }
                }
            };

            foreach (var product in products)
            {
                var docRef = _firestore.Products.Document();
                batch.Create(docRef, product);
                _logger.LogInformation("Added product: {ProductName}", product["Name"]);
            }

            await batch.CommitAsync();
            _logger.LogInformation("Sample products seeded successfully");
        }

        public async Task<List<Product>> GetAllProducts()
        {
            try
            {
                _logger.LogInformation("Fetching all products from Firestore");
                var snapshot = await _firestore.Products.GetSnapshotAsync();
                var products = snapshot.Select(doc => doc.ConvertTo<Product>()).ToList();
                _logger.LogInformation($"Retrieved {products.Count} products from Firestore");
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products from Firestore");
                throw;
            }
        }

        public async Task<Product> GetProductById(string id)
        {
            try
            {
                _logger.LogInformation("Retrieving product with ID: {ProductId}", id);
                if (string.IsNullOrEmpty(id))
                    throw new ValidationException("Product ID cannot be empty");

                var docRef = _firestore.Products.Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                    throw new ProductNotFoundException(id);

                return snapshot.ConvertTo<Product>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product with ID: {ProductId}", id);
                throw;
            }
        }

        public async Task<Product> CreateProduct(CreateProductDto productDto)
        {
            try
            {
                _logger.LogInformation("Creating new product: {ProductName}", productDto.Name);
                var docRef = _firestore.Products.Document();
                var product = new Product
                {
                    Id = docRef.Id,
                    Name = productDto.Name,
                    Description = productDto.Description,
                    Price = productDto.Price,
                    PictureUrl = productDto.PictureUrl,
                    ProductType = productDto.ProductType
                };

                await docRef.SetAsync(product);
                _logger.LogInformation("Created product with ID: {ProductId}", product.Id);
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {ProductName}", productDto.Name);
                throw;
            }
        }

        public async Task<Product?> UpdateProduct(string id, UpdateProductDto productDto)
        {
            try
            {
                _logger.LogInformation("Updating product with ID: {ProductId}", id);
                if (string.IsNullOrEmpty(id))
                    throw new ValidationException("Product ID cannot be empty");

                var docRef = _firestore.Products.Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                    throw new ProductNotFoundException(id);

                var updates = new Dictionary<string, object?>();
                if (productDto.Name != null) updates["Name"] = productDto.Name;
                if (productDto.Description != null) updates["Description"] = productDto.Description;
                if (productDto.Price.HasValue) updates["Price"] = productDto.Price;
                if (productDto.PictureUrl != null) updates["PictureUrl"] = productDto.PictureUrl;
                if (productDto.ProductType != null) updates["ProductType"] = productDto.ProductType;

                await docRef.UpdateAsync(updates);

                var updatedSnapshot = await docRef.GetSnapshotAsync();
                _logger.LogInformation("Updated product: {ProductName}", updatedSnapshot.ConvertTo<Product>().Name);
                return updatedSnapshot.ConvertTo<Product>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product with ID: {ProductId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteProduct(string id)
        {
            try
            {
                _logger.LogInformation("Deleting product with ID: {ProductId}", id);
                if (string.IsNullOrEmpty(id))
                    throw new ValidationException("Product ID cannot be empty");

                var docRef = _firestore.Products.Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                    throw new ProductNotFoundException(id);

                await docRef.DeleteAsync();
                _logger.LogInformation("Deleted product with ID: {ProductId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID: {ProductId}", id);
                throw;
            }
        }

        public async Task<List<Product>> SearchProducts(string name)
        {
            try
            {
                _logger.LogInformation("Searching for products with name: {ProductName}", name);
                var snapshot = await _firestore.Products.GetSnapshotAsync();
                var products = snapshot.Documents.Select(doc => doc.ConvertTo<Product>()).ToList();

                if (string.IsNullOrEmpty(name))
                    return products;

                _logger.LogInformation("Filtering products by name: {ProductName}", name);
                return products.Where(p => p.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for products with name: {ProductName}", name);
                throw;
            }
        }
    }
}
