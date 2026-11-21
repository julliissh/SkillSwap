namespace Common.Entities;

public class Profile
{
    public const int MinNameLength = 2;
    public const int MaxNameLength = 50;

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}
