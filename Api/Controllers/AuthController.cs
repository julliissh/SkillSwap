using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using Api.DTOs;
using Api.Services;
using Common.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Persistence;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    public const string RateLimitPolicy = "auth";

    private const int MaxFailedAttempts = 5;
    private const int ResendCooldownSeconds = 60;
    private const int DefaultJwtExpirationMinutes = 60;
    private const int SecondsPerMinute = 60;

    private readonly SkillSwapDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly EmailCodeService _emailCodeService;
    private readonly IEmailSender _emailSender;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public AuthController(
        SkillSwapDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        EmailCodeService emailCodeService,
        IEmailSender emailSender,
        JwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _emailCodeService = emailCodeService;
        _emailSender = emailSender;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    [EnableRateLimiting(RateLimitPolicy)]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();

        if (firstName.Length < Profile.MinNameLength || firstName.Length > Profile.MaxNameLength)
        {
            return BadRequest(new { message = $"First name must be between {Profile.MinNameLength} and {Profile.MaxNameLength} characters." });
        }

        if (lastName.Length < Profile.MinNameLength || lastName.Length > Profile.MaxNameLength)
        {
            return BadRequest(new { message = $"Last name must be between {Profile.MinNameLength} and {Profile.MaxNameLength} characters." });
        }

        var email = NormalizeEmail(request.Email);

        var emailExists = await _dbContext.Users.AnyAsync(u => u.Email == email);
        if (emailExists)
        {
            return Conflict(new { message = "Email is already registered." });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Profile = new Profile
            {
                Id = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName
            }
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        var code = _emailCodeService.GenerateCode();
        var emailToken = _emailCodeService.CreateEmailToken(user.Id, EmailToken.EmailVerificationPurpose, code);

        _dbContext.Users.Add(user);
        _dbContext.EmailTokens.Add(emailToken);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_Users_Email"
            })
        {
            return Conflict(new { message = "Email is already registered." });
        }

        try
        {
            await _emailSender.SendVerificationCodeAsync(user.Email, code);
        }
        catch (Exception ex) when (ex is SmtpException || ex is InvalidOperationException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "Account created, but verification email could not be sent." });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            message = "User registered successfully.",
            userId = user.Id
        });
    }

    [EnableRateLimiting(RateLimitPolicy)]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        if (user.Status != Common.Entities.User.ActiveStatus)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Account is not active." });
        }

        var token = _jwtTokenService.GenerateToken(user);
        var expirationMinutes = _configuration.GetValue<int?>("Jwt:ExpirationMinutes") ?? DefaultJwtExpirationMinutes;

        return Ok(new
        {
            message = "Login successful.",
            accessToken = token,
            tokenType = "Bearer",
            expiresIn = expirationMinutes * SecondsPerMinute
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || user.Status != Common.Entities.User.ActiveStatus)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            role = user.Role
        });
    }

    [EnableRateLimiting(RateLimitPolicy)]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        const string invalidCodeMessage = "Invalid or expired verification code.";

        var email = NormalizeEmail(request.Email);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var user = await FindUserForUpdateAsync(email);
        if (user == null)
        {
            return BadRequest(new { message = invalidCodeMessage });
        }

        var tokens = await _dbContext.EmailTokens
            .FromSqlInterpolated($"""
                SELECT *
                FROM "EmailTokens"
                WHERE "UserId" = {user.Id}
                  AND "Purpose" = {EmailToken.EmailVerificationPurpose}
                  AND "UsedAt" IS NULL
                ORDER BY "ExpiresAt" DESC
                LIMIT 1
                FOR UPDATE
                """)
            .ToListAsync();

        var token = tokens.FirstOrDefault();
        if (token == null || token.ExpiresAt <= DateTime.UtcNow)
        {
            return BadRequest(new { message = invalidCodeMessage });
        }

        if (token.FailedAttempts >= MaxFailedAttempts)
        {
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                new { message = "Too many verification attempts. Request a new code." });
        }

        var enteredHash = _emailCodeService.HashCode(user.Id, EmailToken.EmailVerificationPurpose, request.Code);
        var enteredHashBytes = Convert.FromHexString(enteredHash);
        var storedHashBytes = Convert.FromHexString(token.TokenHash);

        if (!CryptographicOperations.FixedTimeEquals(enteredHashBytes, storedHashBytes))
        {
            token.FailedAttempts++;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return BadRequest(new { message = invalidCodeMessage });
        }

        user.Status = Common.Entities.User.ActiveStatus;
        token.UsedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new { message = "Email verified successfully." });
    }

    [EnableRateLimiting(RateLimitPolicy)]
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        const string genericResponse = "If verification is needed, a code will be sent.";

        var email = NormalizeEmail(request.Email);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var user = await FindUserForUpdateAsync(email);
        if (user == null || user.Status != Common.Entities.User.PendingStatus)
        {
            return Ok(new { message = genericResponse });
        }

        var latestToken = await _dbContext.EmailTokens
            .Where(t => t.UserId == user.Id && t.Purpose == EmailToken.EmailVerificationPurpose)
            .OrderByDescending(t => t.ExpiresAt)
            .FirstOrDefaultAsync();

        if (latestToken != null)
        {
            var createdAt = latestToken.ExpiresAt.AddMinutes(-EmailCodeService.CodeExpirationMinutes);
            var nextAllowedAt = createdAt.AddSeconds(ResendCooldownSeconds);

            if (DateTime.UtcNow < nextAllowedAt)
            {
                return Ok(new { message = genericResponse });
            }
        }

        var activeTokens = await _dbContext.EmailTokens
            .Where(t => t.UserId == user.Id && t.Purpose == EmailToken.EmailVerificationPurpose && t.UsedAt == null)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.UsedAt = now;
        }

        var code = _emailCodeService.GenerateCode();
        var newToken = _emailCodeService.CreateEmailToken(user.Id, EmailToken.EmailVerificationPurpose, code);

        _dbContext.EmailTokens.Add(newToken);

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        try
        {
            await _emailSender.SendVerificationCodeAsync(user.Email, code);
        }
        catch (Exception ex) when (ex is SmtpException || ex is InvalidOperationException)
        {
            newToken.UsedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "Email service is temporarily unavailable." });
        }

        return Ok(new { message = genericResponse });
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private async Task<User?> FindUserForUpdateAsync(string email)
    {
        var users = await _dbContext.Users
            .FromSqlInterpolated($"""
                SELECT *
                FROM "Users"
                WHERE "Email" = {email}
                FOR UPDATE
                """)
            .ToListAsync();

        return users.FirstOrDefault();
    }
}
