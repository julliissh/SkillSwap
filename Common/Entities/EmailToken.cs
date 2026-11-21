namespace Common.Entities;

public class EmailToken
{
    public const string EmailVerificationPurpose = "EmailVerification";

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public string Purpose { get; set; } = "";
    public int FailedAttempts { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
