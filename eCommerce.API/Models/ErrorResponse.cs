namespace eCommerce.API.Models;

/// <summary>
/// Represents the standard error response format
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// The type of error that occurred
    /// </summary>
    public string Type { get; set; } = default!;

    /// <summary>
    /// The error message
    /// </summary>
    public string Message { get; set; } = default!;

    /// <summary>
    /// The HTTP status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// The Firebase error code, if applicable
    /// </summary>
    public string? ErrorCode { get; set; }
}