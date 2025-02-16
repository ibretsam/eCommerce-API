using System;

namespace eCommerce.API.Models.DTOs;

public record CreateProductDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public double Price { get; init; }
    public string? PictureUrl { get; init; }
    public string? ProductType { get; init; }
}

public record UpdateProductDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public double? Price { get; init; }
    public string? PictureUrl { get; init; }
    public string? ProductType { get; init; }
}