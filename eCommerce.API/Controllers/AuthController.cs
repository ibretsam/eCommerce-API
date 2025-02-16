using System;
using eCommerce.API.Models;
using eCommerce.API.Models.DTOs;
using eCommerce.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.API.Controllers;

/// <summary>
/// Controller for handling authentication-related operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IFirebaseAuthService _firebaseAuth;

    public AuthController(IFirebaseAuthService firebaseAuth)
    {
        _firebaseAuth = firebaseAuth;
    }

    /// <summary>
    /// Registers a new user with Firebase Authentication and Firestore
    /// </summary>
    /// <param name="registerDto">User registration details including email, password, display name, and phone number</param>
    /// <returns>Created user information including ID, email, display name, and other profile details</returns>
    /// <response code="200">Returns the newly created user profile</response>
    /// <response code="400">If email/password is missing or password is less than 6 characters</response>
    /// <response code="409">If user with the same email already exists</response>
    /// <response code="500">If an unexpected error occurs during registration</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto registerDto)
    {
        var user = await _firebaseAuth.CreateUser(registerDto);
        return Ok(user);
    }

    /// <summary>
    /// Authenticates user with Firebase and returns a custom token
    /// </summary>
    /// <param name="loginDto">User credentials (email and password)</param>
    /// <returns>Firebase custom authentication token</returns>
    /// <response code="200">Returns the custom authentication token</response>
    /// <response code="401">If credentials are invalid</response>
    /// <response code="500">If an unexpected error occurs during authentication</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> Login([FromBody] LoginUserDto loginDto)
    {
        var token = await _firebaseAuth.SignInWithPassword(loginDto);
        return Ok(new { token });
    }

    /// <summary>
    /// Revokes all refresh tokens for the current user
    /// </summary>
    /// <returns>No content on success</returns>
    /// <response code="200">If sessions were successfully revoked</response>
    /// <response code="401">If user is not authenticated or token is invalid</response>
    /// <response code="400">If user ID is missing</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout()
    {
        var uid = User.FindFirst("user_id")?.Value;

        await _firebaseAuth.RevokeUserSessions(uid);
        return Ok();
    }

    /// <summary>
    /// Verifies and decodes a Firebase ID token
    /// </summary>
    /// <param name="idToken">Firebase ID token to verify</param>
    /// <returns>User ID (UID) associated with the token</returns>
    /// <response code="200">Returns the user ID if token is valid</response>
    /// <response code="400">If token is empty</response>
    /// <response code="401">If token is invalid or expired</response>
    [HttpPost("verify-token")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyToken([FromBody] string idToken)
    {
        var uid = await _firebaseAuth.VerifyToken(idToken);
        return Ok(uid);
    }

    /// <summary>
    /// Retrieves current user's profile from Firestore, creates if doesn't exist
    /// </summary>
    /// <returns>User profile including ID, email, display name, and other details</returns>
    /// <response code="200">Returns the user profile</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If user profile cannot be found or created</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> GetCurrentUser()
    {
        var uid = User.FindFirst("user_id")?.Value;

        var user = await _firebaseAuth.GetUserInfo(uid);
        return Ok(user);
    }
}
