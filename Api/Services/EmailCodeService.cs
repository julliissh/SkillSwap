using System.Security.Cryptography;
using System.Text;
using Common.Entities;

namespace Api.Services;

public class EmailCodeService
{
    public const int CodeExpirationMinutes = 10;
    private const int MinHmacKeyBytes = 32;
    private const int SixDigitCodeUpperBound = 1_000_000;

    private readonly byte[] _hmacKey;

    public EmailCodeService(IConfiguration configuration)
    {
        var key = configuration["EmailCode:HmacKey"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("EmailCode:HmacKey is not configured.");
        }

        _hmacKey = Convert.FromBase64String(key);
        if (_hmacKey.Length < MinHmacKeyBytes)
        {
            throw new InvalidOperationException($"HMAC key must be at least {MinHmacKeyBytes} bytes.");
        }
    }

    public string GenerateCode()
    {
        var number = RandomNumberGenerator.GetInt32(0, SixDigitCodeUpperBound);
        return number.ToString("D6");
    }

    public string HashCode(Guid userId, string purpose, string code)
    {
        var data = $"{userId:N}:{purpose}:{code}";
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hashBytes = HMACSHA256.HashData(_hmacKey, dataBytes);

        return Convert.ToHexString(hashBytes);
    }

    public EmailToken CreateEmailToken(Guid userId, string purpose, string code)
    {
        return new EmailToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashCode(userId, purpose, code),
            Purpose = purpose,
            FailedAttempts = 0,
            ExpiresAt = DateTime.UtcNow.AddMinutes(CodeExpirationMinutes)
        };
    }
}
