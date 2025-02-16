using eCommerce.API.Models;
using eCommerce.API.Models.DTOs;
using eCommerce.API.Services;
using Microsoft.Extensions.Logging;
using Moq;

public class ProductServiceTests
{
    private readonly Mock<IFirestoreService> _firestoreMock;
    private readonly Mock<ILogger<ProductService>> _loggerMock;
    private readonly IProductService _productService;

    public ProductServiceTests()
    {
        _firestoreMock = new Mock<IFirestoreService>();
        _loggerMock = new Mock<ILogger<ProductService>>();
        _productService = new ProductService(_firestoreMock.Object, _loggerMock.Object);
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
            ProductType = "Electronics",
            PictureUrl = "http://example.com/image.jpg"
        };

        var expectedProduct = new Product
        {
            Id = "new-id",
            Name = createDto.Name,
            Price = createDto.Price,
            Description = createDto.Description,
            ProductType = createDto.ProductType,
            PictureUrl = createDto.PictureUrl
        };

        _firestoreMock.Setup(f => f.CreateProduct(It.IsAny<Product>()))
                     .ReturnsAsync(expectedProduct);

        // Act
        var result = await _productService.CreateProduct(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedProduct.Id, result.Id);
        Assert.Equal(createDto.Name, result.Name);
        Assert.Equal(createDto.Price, result.Price);
        Assert.Equal(createDto.Description, result.Description);
        Assert.Equal(createDto.ProductType, result.ProductType);
        Assert.Equal(createDto.PictureUrl, result.PictureUrl);
    }

    [Fact]
    public async Task SearchProducts_WithValidName_ReturnsFilteredProducts()
    {
        // Arrange
        var products = new List<Product>
    {
        new Product { Id = "1", Name = "iPhone 12", ProductType = "Electronics" },
        new Product { Id = "2", Name = "Samsung Galaxy", ProductType = "Electronics" },
        new Product { Id = "3", Name = "iPhone 13", ProductType = "Electronics" }
    };

        _firestoreMock.Setup(f => f.GetAllProducts())
                     .ReturnsAsync(products);

        // Act
        var result = await _productService.SearchProducts("iPhone");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Contains("iPhone", p.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateProduct_ValidUpdate_ReturnsUpdatedProduct()
    {
        // Arrange
        var productId = "test-id";
        var existingProduct = new Product
        {
            Id = productId,
            Name = "Original Name",
            Price = 99.99,
            Description = "Original Description",
            ProductType = "Electronics"
        };

        var updateDto = new UpdateProductDto
        {
            Name = "Updated Name",
            Price = 149.99,
            Description = "Updated Description"
        };

        var updatedProduct = new Product
        {
            Id = productId,
            Name = updateDto.Name,
            Price = updateDto.Price.Value,
            Description = updateDto.Description,
            ProductType = existingProduct.ProductType
        };

        _firestoreMock.Setup(f => f.GetProductById(productId))
                     .ReturnsAsync(existingProduct);
        _firestoreMock.Setup(f => f.UpdateProduct(productId, It.IsAny<Product>()))
                     .ReturnsAsync(updatedProduct);

        // Act
        var result = await _productService.UpdateProduct(productId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateDto.Name, result.Name);
        Assert.Equal(updateDto.Price, result.Price);
        Assert.Equal(updateDto.Description, result.Description);
    }
}