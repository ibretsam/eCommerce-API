namespace eCommerce.API.Middleware.Exceptions;

public class ProductNotFoundException : Exception
{
    public string ProductId { get; }

    public ProductNotFoundException(string productId)
        : base($"Product with ID {productId} not found")
    {
        ProductId = productId;
    }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}