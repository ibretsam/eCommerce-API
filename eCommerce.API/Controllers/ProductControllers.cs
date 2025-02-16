using System;
using System.Threading.Tasks;
using eCommerce.API.Models;
using eCommerce.API.Models.DTOs;
using eCommerce.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.API.Controllers;

/// <summary>
/// Controller for managing product-related operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductController> _logger;

    public ProductController(IProductService productService, ILogger<ProductController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with sample products
    /// </summary>
    /// <returns>Success message if seeding was successful</returns>
    /// <response code="200">Products seeded successfully</response>
    /// <response code="500">If an error occurs during seeding</response>
    [HttpPost("seed")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Seed()
    {
        _logger.LogInformation("Starting product seeding process");
        await _productService.SeedSampleProducts();
        _logger.LogInformation("Product seeding completed successfully");
        return Ok("Products seeded successfully");
    }

    /// <summary>
    /// Retrieves all products
    /// </summary>
    /// <returns>List of all products</returns>
    /// <response code="200">Returns the list of products</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<Product>>> GetProducts()
    {
        _logger.LogInformation("Retrieving all products");
        var products = await _productService.GetAllProducts();
        _logger.LogInformation("Retrieved {Count} products", products.Count);
        return Ok(products);
    }

    /// <summary>
    /// Retrieves a specific product by ID
    /// </summary>
    /// <param name="id">The ID of the product</param>
    /// <returns>The requested product</returns>
    /// <response code="200">Returns the requested product</response>
    /// <response code="404">If product is not found</response>
    /// <response code="400">If product ID is empty</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Product>> GetProduct(string id)
    {
        _logger.LogInformation("Retrieving product with ID: {ProductId}", id);
        var product = await _productService.GetProductById(id);
        _logger.LogInformation("Retrieved product: {ProductName}", product.Name);
        return Ok(product);
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    /// <param name="productDto">The product information</param>
    /// <returns>The created product</returns>
    /// <response code="201">Returns the newly created product</response>
    /// <response code="400">If the product data is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(Product), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductDto productDto)
    {
        _logger.LogInformation("Creating new product: {ProductName}", productDto.Name);
        var product = await _productService.CreateProduct(productDto);
        _logger.LogInformation("Created product with ID: {ProductId}", product.Id);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    /// <param name="id">The ID of the product to update</param>
    /// <param name="productDto">The updated product information</param>
    /// <returns>The updated product</returns>
    /// <response code="200">Returns the updated product</response>
    /// <response code="404">If product is not found</response>
    /// <response code="400">If product ID is empty or data is invalid</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Product>> UpdateProduct(string id, [FromBody] UpdateProductDto productDto)
    {
        _logger.LogInformation("Updating product with ID: {ProductId}", id);
        var updatedProduct = await _productService.UpdateProduct(id, productDto);
        _logger.LogInformation("Updated product: {ProductName}", updatedProduct.Name);
        return Ok(updatedProduct);
    }

    /// <summary>
    /// Deletes a product
    /// </summary>
    /// <param name="id">The ID of the product to delete</param>
    /// <returns>True if deletion was successful</returns>
    /// <response code="200">If product was successfully deleted</response>
    /// <response code="404">If product is not found</response>
    /// <response code="400">If product ID is empty</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> DeleteProduct(string id)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId}", id);
        var deleted = await _productService.DeleteProduct(id);
        _logger.LogInformation("Product deletion status: {Status}", deleted);
        return Ok(deleted);
    }

    /// <summary>
    /// Searches for products by name
    /// </summary>
    /// <param name="name">The search term (case-insensitive)</param>
    /// <returns>List of products matching the search term</returns>
    /// <response code="200">Returns the list of matching products</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<Product>>> SearchProducts([FromQuery] string name)
    {
        _logger.LogInformation("Searching for products with name: {ProductName}", name);
        var products = await _productService.SearchProducts(name);
        _logger.LogInformation("Found {Count} products matching the search term", products.Count);
        return Ok(products);
    }
}