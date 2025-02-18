using System;
using eCommerce.API.Models;
using eCommerce.API.Models.DTOs;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace eCommerce.API.Services;

public interface IFirebaseAuthService
{
    Task<string> VerifyToken(string token);
    Task<UserResponseDto> CreateUser(RegisterUserDto registerDto);
    Task<AuthResponseDto> SignInWithPassword(LoginUserDto loginDto);
    Task<UserResponseDto?> GetUserInfo(string uid);
    Task RevokeUserSessions(string uid);
}
public class FirebaseAuthService : IFirebaseAuthService
{
    private readonly FirebaseAuth _auth;
    private readonly IFirestoreService _firestore;
    private readonly ILogger<FirebaseAuthService> _logger;
    private readonly IConfiguration _configuration;

    public FirebaseAuthService(IConfiguration configuration,
                     IFirestoreService firestore,
                     ILogger<FirebaseAuthService> logger)
    {
        _configuration = configuration;
        _firestore = firestore;
        _logger = logger;

        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                GoogleCredential credential;
                try
                {
                    // Try to get Application Default Credentials first (works in Cloud Run)
                    credential = GoogleCredential.GetApplicationDefault()
                        .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
                }
                catch
                {
                    // Fall back to local credentials file
                    var credentialsPath = Path.GetFullPath(configuration["Firebase:CredentialsPath"] ??
                        throw new ArgumentException("Firebase:CredentialsPath configuration is required"));

                    if (!File.Exists(credentialsPath))
                    {
                        throw new FileNotFoundException($"Credentials file not found at: {credentialsPath}");
                    }

                    credential = GoogleCredential.FromFile(credentialsPath)
                        .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
                }

                FirebaseApp.Create(new AppOptions
                {
                    Credential = credential,
                    ProjectId = configuration["Firebase:ProjectId"]
                });
            }

            _auth = FirebaseAuth.DefaultInstance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firebase Auth");
            throw;
        }
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

    public async Task<AuthResponseDto> SignInWithPassword(LoginUserDto loginDto)
    {
        try
        {
            _logger.LogInformation("Authenticating user with email: {Email}", loginDto.Email);

            // Make direct HTTP request to Firebase Auth REST API
            using var client = new HttpClient();
            var response = await client.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_configuration["Firebase:WebApiKey"]}",
                new StringContent(JsonSerializer.Serialize(new
                {
                    email = loginDto.Email,
                    password = loginDto.Password,
                    returnSecureToken = true
                }), Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<FirebaseErrorResponse>();
                throw new UnauthorizedAccessException($"Authentication failed: {error?.Error?.Message}");
            }

            var authResult = await response.Content.ReadFromJsonAsync<FirebaseAuthResponse>();

            // Update last login time in Firestore
            try
            {
                var docRef = _firestore.Users.Document(authResult.LocalId);
                await docRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "LastLoginAt", DateTime.UtcNow }
                });
            }
            catch (Exception)
            {
                _logger.LogWarning("Failed to update user's last login time");
            }

            _logger.LogInformation("Successfully authenticated user. UserId: {UserId}", authResult.LocalId);

            return new AuthResponseDto
            {
                IdToken = authResult.IdToken,
                RefreshToken = authResult.RefreshToken,
                ExpiresIn = authResult.ExpiresIn
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for email: {Email}", loginDto.Email);
            throw;
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
