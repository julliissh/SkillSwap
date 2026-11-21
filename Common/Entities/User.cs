namespace Common.Entities;

public class User
{
    public const string DefaultRole = "User";
    public const string PendingStatus = "Pending";
    public const string ActiveStatus = "Active";

    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = DefaultRole;
    public string Status { get; set; } = PendingStatus;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Profile? Profile { get; set; }
}
