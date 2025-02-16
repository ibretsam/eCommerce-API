using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using eCommerce.API.Exceptions;
using eCommerce.API.Models;
using eCommerce.API.Models.DTOs;
using eCommerce.API.Services;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace eCommerce.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IFirestoreService> _firestoreMock;
        private readonly Mock<ILogger<ProductService>> _loggerMock;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _firestoreMock = new Mock<IFirestoreService>();
            _loggerMock = new Mock<ILogger<ProductService>>();
            _productService = new ProductService(_firestoreMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetProductById_ValidId_ReturnsProduct()
        {
            // Arrange
            var productId = "test-id";
            var expectedProduct = new Product { Id = productId, Name = "Test Product" };

            // Mock FirestoreService to return the expected product snapshot
            var firestoreMock = new Mock<IFirestoreService>();
            var collectionMock = new Mock<CollectionReference>();

            var docRefMock = new Mock<DocumentReference>();
            var snapshotMock = new Mock<DocumentSnapshot>();

            snapshotMock.Setup(s => s.Exists).Returns(true);
            snapshotMock.Setup(s => s.ConvertTo<Product>()).Returns(expectedProduct);
            docRefMock.Setup(d => d.GetSnapshotAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(snapshotMock.Object);
            collectionMock.Setup(c => c.Document(productId)).Returns(docRefMock.Object);
            firestoreMock.Setup(f => f.Products).Returns(collectionMock.Object);

            var service = new ProductService(firestoreMock.Object, _loggerMock.Object);

            // Act
            var result = await service.GetProductById(productId);

            // Assert
            Assert.Equal(expectedProduct.Id, result.Id);
            Assert.Equal(expectedProduct.Name, result.Name);
        }

        [Fact]
        public async Task GetProductById_NonexistentId_ThrowsProductNotFoundException()
        {
            // Arrange
            var productId = "nonexistent-id";
            var documentRef = new Mock<DocumentReference>();
            var documentSnapshot = new Mock<DocumentSnapshot>();

            documentSnapshot.Setup(d => d.Exists).Returns(false);

            _firestoreMock.Setup(f => f.Products.Document(productId))
                         .Returns(documentRef.Object);
            documentRef.Setup(d => d.GetSnapshotAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(documentSnapshot.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ProductNotFoundException>(() =>
                _productService.GetProductById(productId));
        }

        [Fact]
        public async Task CreateProduct_ValidProduct_ReturnsCreatedProduct()
        {
            // Arrange
            var createDto = new CreateProductDto
            {
                Name = "New Product",
                Price = 129.99,
                Description = "New Description",
                ProductType = "Electronics"
            };

            var documentRef = new Mock<DocumentReference>();
            documentRef.Setup(d => d.Id).Returns("new-id");

            _firestoreMock.Setup(f => f.Products.Document())
                         .Returns(documentRef.Object);

            // Act
            var result = await _productService.CreateProduct(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("new-id", result.Id);
            Assert.Equal(createDto.Name, result.Name);
            Assert.Equal(createDto.Price, result.Price);
        }

        [Fact]
        public async Task SearchProducts_WithValidName_ReturnsFilteredProducts()
        {
            // Arrange
            var products = new List<Product>
    {
        new Product { Id = "1", Name = "iPhone 12" },
        new Product { Id = "2", Name = "Samsung Galaxy" },
        new Product { Id = "3", Name = "iPhone 13" }
    };

            var firestoreMock = new Mock<IFirestoreService>();
            var collectionMock = new Mock<CollectionReference>();
            var querySnapshot = new Mock<QuerySnapshot>();

            // Setup the snapshot to return the test products
            var documents = products.Select(p =>
            {
                var docSnapshot = new Mock<DocumentSnapshot>();
                docSnapshot.Setup(d => d.ConvertTo<Product>()).Returns(p);
                return docSnapshot.Object;
            }).ToList();

            querySnapshot.Setup(q => q.Documents).Returns(documents);
            collectionMock.Setup(c => c.GetSnapshotAsync(It.IsAny<CancellationToken>()))
                         .ReturnsAsync(querySnapshot.Object);
            firestoreMock.Setup(f => f.Products).Returns(collectionMock.Object);

            var service = new ProductService(firestoreMock.Object, _loggerMock.Object);

            // Act
            var result = await service.SearchProducts("iPhone");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Contains("iPhone", p.Name));
        }
    }
}