using System;

namespace eCommerce.API.Models.DTOs;

public record RegisterUserDto
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? DisplayName { get; init; }
    public string? PhoneNumber { get; init; }
}
public record LoginUserDto(string Email, string Password);
public record UserResponseDto
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public string? DisplayName { get; init; }
    public string? PhotoUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; }
}