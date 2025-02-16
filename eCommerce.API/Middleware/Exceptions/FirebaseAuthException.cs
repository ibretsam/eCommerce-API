namespace eCommerce.API.Middleware.Exceptions;

public class FirebaseAuthException : Exception
{
    public string ErrorCode { get; }

    public FirebaseAuthException(string message, string errorCode = "") : base(message)
    {
        ErrorCode = errorCode;
    }

    public FirebaseAuthException(string message, Exception innerException, string errorCode = "")
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException(string message = "Invalid email or password")
        : base(message) { }
}

public class UserAlreadyExistsException : Exception
{
    public UserAlreadyExistsException(string email)
        : base($"User with email {email} already exists") { }
}

public class TokenValidationException : Exception
{
    public TokenValidationException(string message = "Invalid or expired token")
        : base(message) { }
}