using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using eCommerce.API.Services;
using eCommerce.API.Middleware.Exceptions;

namespace eCommerce.Tests.Services
{
    public class FirestoreServiceTests
    {
        [Fact]
        public void Constructor_ThrowsException_WhenProjectIdMissing()
        {
            // Arrange
            var configData = new Dictionary<string, string>();
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();
            var loggerMock = new Mock<ILogger<FirestoreService>>();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new FirestoreService(configuration, loggerMock.Object));
            Assert.Equal("Firebase:ProjectId configuration is required", exception.Message);
        }

        [Fact]
        public void Constructor_CreatesInstance_WhenProjectIdProvided()
        {
            // Arrange
            var configData = new Dictionary<string, string>
            {
                { "Firebase:ProjectId", "test-project" }
            };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();
            var loggerMock = new Mock<ILogger<FirestoreService>>();

            // Act
            var service = new FirestoreService(configuration, loggerMock.Object);

            // Assert
            Assert.NotNull(service.Products);
            Assert.NotNull(service.Users);
        }

        [Fact]
        public void Constructor_LogsInformation_WhenInitialized()
        {
            // Arrange
            var configData = new Dictionary<string, string>
            {
                { "Firebase:ProjectId", "test-project" }
            };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();
            var loggerMock = new Mock<ILogger<FirestoreService>>();

            // Act
            var service = new FirestoreService(configuration, loggerMock.Object);

            // Assert
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Initializing Firestore with project ID")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        // [Fact]
        // public async Task GetProductById_ThrowsNotFoundException_WhenProductDoesNotExist()
        // {
        //     // Arrange
        //     var service = CreateService();
        //     var nonExistentId = "non-existent-id";

        //     // Act & Assert
        //     await Assert.ThrowsAsync<ProductNotFoundException>(() =>
        //         service.GetProductById(nonExistentId));
        // }

        [Fact]
        public async Task DeleteProduct_ReturnsFalse_WhenProductDoesNotExist()
        {
            // Arrange
            var service = CreateService();
            var nonExistentId = "non-existent-id";

            // Act
            var result = await service.DeleteProduct(nonExistentId);

            // Assert
            Assert.False(result);
        }

        private FirestoreService CreateService()
        {
            var configData = new Dictionary<string, string>
        {
            { "Firebase:ProjectId", "test-project" }
        };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();
            var loggerMock = new Mock<ILogger<FirestoreService>>();
            return new FirestoreService(configuration, loggerMock.Object);
        }
    }
}