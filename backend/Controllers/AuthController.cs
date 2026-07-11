using BillingSystem.DTOs;
using BillingSystem.Infrastructure;
using BillingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BillingSystem.Controllers;

/// <summary>
/// User authentication and session management.
/// </summary>
[Tags("Authentication")]
[AllowAnonymous]
public class AuthController : BaseApiController
{
    private readonly MongoDbContext _db;
    private readonly IConfiguration _config;
    private readonly PasswordHasher<User> _hasher;

    public AuthController(MongoDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
        _hasher = new PasswordHasher<User>();
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequestResponse("Username and password are required");

        try
        {
            var usernameLower = request.Username.Trim().ToLower();
            var user = await _db.Users.Find(u => u.Username == usernameLower).FirstOrDefaultAsync();

            if (user == null)
                return BadRequestResponse("Invalid username or password");

            var verificationResult = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
                return BadRequestResponse("Invalid username or password");

            var token = GenerateJwtToken(user);

            var response = new AuthResponse
            {
                Token = token,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role
            };

            return OkResponse(response, "Login successful");
        }
        catch (Exception ex)
        {
            return ServerErrorResponse(ex.Message);
        }
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequestResponse("Invalid registration details");

        try
        {
            var usernameLower = request.Username.Trim().ToLower();
            var existingUser = await _db.Users.Find(u => u.Username == usernameLower).FirstOrDefaultAsync();

            if (existingUser != null)
                return BadRequestResponse("Username is already taken");

            var user = new User
            {
                Username = usernameLower,
                FullName = request.FullName,
                Role = "Admin",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _hasher.HashPassword(user, request.Password);

            await _db.Users.InsertOneAsync(user);

            var token = GenerateJwtToken(user);

            var response = new AuthResponse
            {
                Token = token,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role
            };

            return CreatedResponse(response, "User registered successfully");
        }
        catch (Exception ex)
        {
            return ServerErrorResponse(ex.Message);
        }
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var secretKey = jwtSettings["Key"] ?? "SUPER_SECRET_KEY_AQUA_BILLING_SYSTEM_2026_GOLD_COMPLIANT";
        var issuer = jwtSettings["Issuer"] ?? "AquaIssuer";
        var audience = jwtSettings["Audience"] ?? "AquaAudience";
        var expiryMinutesStr = jwtSettings["ExpiryMinutes"] ?? "1440"; // Default: 24 hours (1440 minutes)

        if (secretKey.Length < 32)
        {
            // Ensure minimum key length for HMAC256
            secretKey = secretKey.PadRight(32, '0');
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("fullName", user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var expiryMinutes = int.Parse(expiryMinutesStr);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
