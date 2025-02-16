using System;
using eCommerce.API.Models;
using eCommerce.API.Models.DTOs;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace eCommerce.API.Services;

public interface IFirebaseAuthService
{
    Task<string> VerifyToken(string token);
    Task<UserResponseDto> CreateUser(RegisterUserDto registerDto);
    Task<string> SignInWithPassword(LoginUserDto loginDto);
    Task<UserResponseDto?> GetUserInfo(string uid);
    Task RevokeUserSessions(string uid);
}
public class FirebaseAuthService : IFirebaseAuthService
{
    private readonly FirebaseAuth _auth;
    private readonly IFirestoreService _firestore;
    private readonly ILogger<FirebaseAuthService> _logger;

    public FirebaseAuthService(IConfiguration configuration,
                             IFirestoreService firestore,
                             ILogger<FirebaseAuthService> logger)
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile("firebase-config.json")
            });
        }

        _auth = FirebaseAuth.DefaultInstance;
        _firestore = firestore;
        _logger = logger;
    }

    public async Task<UserResponseDto> CreateUser(RegisterUserDto registerDto)
    {
        try
        {
            _logger.LogInformation("Creating new user with email: {Email}", registerDto.Email);

            if (string.IsNullOrEmpty(registerDto.Email) || string.IsNullOrEmpty(registerDto.Password))
                throw new ArgumentException("Email and password are required");

            if (registerDto.Password.Length < 6)
                throw new ArgumentException("Password must be at least 6 characters");

            var args = new UserRecordArgs
            {
                Email = registerDto.Email,
                Password = registerDto.Password,
                EmailVerified = false,
                DisplayName = registerDto.DisplayName,
                PhoneNumber = registerDto.PhoneNumber
            };

            var userRecord = await _auth.CreateUserAsync(args);

            var user = new User
            {
                Id = userRecord.Uid,
                Email = userRecord.Email,
                DisplayName = userRecord.DisplayName,
                PhotoUrl = userRecord.PhotoUrl,
                CreatedAt = DateTime.UtcNow,
                PhoneNumber = userRecord.PhoneNumber,
                IsActive = true
            };

            await _firestore.Users.Document(user.Id).SetAsync(user);

            _logger.LogInformation("Successfully created user. UserId: {UserId}", user.Id);
            return MapToUserResponse(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user with email: {Email}", registerDto.Email);
            throw;
        }
    }

    public async Task<string> SignInWithPassword(LoginUserDto loginDto)
    {
        try
        {
            _logger.LogInformation("Authenticating user with email: {Email}", loginDto.Email);
            var user = await _auth.GetUserByEmailAsync(loginDto.Email);

            var docRef = _firestore.Users.Document(user.Uid);
            try
            {
                await docRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "LastLoginAt", DateTime.UtcNow }
                });
            }
            catch (Exception)
            {
                _logger.LogWarning("Failed to update user's last login time");
            }

            var customToken = await _auth.CreateCustomTokenAsync(user.Uid);
            _logger.LogInformation("Successfully authenticated user. UserId: {UserId}", user.Uid);
            return customToken;
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogWarning("Authentication failed: {Message}", ex.Message);
            throw new UnauthorizedAccessException($"Authentication failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred");
            throw new Exception($"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<string> VerifyToken(string idToken)
    {
        _logger.LogInformation("Verifying token");
        if (string.IsNullOrEmpty(idToken))
        {
            _logger.LogWarning("Token cannot be empty");
            throw new ArgumentException("Token cannot be empty");
        }

        try
        {
            var decodedToken = await _auth.VerifyIdTokenAsync(idToken);
            return decodedToken.Uid;
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogWarning("Invalid token: {Message}", ex.Message);
            throw new UnauthorizedAccessException($"Invalid token: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token verification failed");
            throw new Exception($"Token verification failed: {ex.Message}");
        }
    }

    public async Task<UserResponseDto?> GetUserInfo(string uid)
    {
        _logger.LogInformation("Retrieving user information for ID: {UserId}", uid);
        if (string.IsNullOrEmpty(uid))
        {
            _logger.LogWarning("User ID cannot be empty");
            throw new ArgumentException("User ID cannot be empty");
        }

        try
        {
            var docRef = _firestore.Users.Document(uid);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                try
                {
                    var userRecord = await _auth.GetUserAsync(uid);
                    var user = new User
                    {
                        Id = userRecord.Uid,
                        Email = userRecord.Email,
                        DisplayName = userRecord.DisplayName,
                        PhotoUrl = userRecord.PhotoUrl,
                        CreatedAt = DateTime.UtcNow,
                        PhoneNumber = userRecord.PhoneNumber,
                        IsActive = true
                    };
                    await docRef.SetAsync(user);
                    return MapToUserResponse(user);
                }
                catch (FirebaseAuthException)
                {
                    return null; // User not found in both Firestore and Firebase Auth
                }
            }

            var existingUser = snapshot.ConvertTo<User>();
            _logger.LogInformation("Retrieved user information for ID: {UserId}", uid);
            return MapToUserResponse(existingUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user information");
            throw new Exception($"Error retrieving user information: {ex.Message}");
        }
    }

    public async Task RevokeUserSessions(string uid)
    {
        _logger.LogInformation("Revoking sessions for user ID: {UserId}", uid);
        if (string.IsNullOrEmpty(uid))
        {
            _logger.LogWarning("User ID cannot be empty");
            throw new ArgumentException("User ID cannot be empty");
        }

        try
        {
            await _auth.RevokeRefreshTokensAsync(uid);
            _logger.LogInformation("Successfully revoked sessions for user ID: {UserId}", uid);
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogWarning("Failed to revoke sessions: {Message}", ex.Message);
            throw new UnauthorizedAccessException($"Failed to revoke sessions: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while revoking sessions");
            throw new Exception($"An error occurred while revoking sessions: {ex.Message}");
        }
    }

    private UserResponseDto MapToUserResponse(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            PhotoUrl = user.PhotoUrl,
            CreatedAt = user.CreatedAt,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt
        };
    }
}
