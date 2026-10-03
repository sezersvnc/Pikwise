namespace Pikwise.Domain.Entities;

public sealed class UserProfile
{
    public int Id { get; set; }
    // Links the local profile to the authentication provider's user identity.
    public required string AuthProviderUserId { get; set; }
    public required string Email { get; set; }
    // Pikwise authorization reads this local role rather than the provider's database role.
    public string Role { get; set; } = "User";
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}
