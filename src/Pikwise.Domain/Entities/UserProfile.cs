namespace Pikwise.Domain.Entities;

public sealed class UserProfile
{
    public int Id { get; set; }
    public required string AuthProviderUserId { get; set; }
    public required string Email { get; set; }
    public string Role { get; set; } = "User";
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}
