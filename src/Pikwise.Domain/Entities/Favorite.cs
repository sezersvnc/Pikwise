namespace Pikwise.Domain.Entities;

public sealed class Favorite
{
    public int UserProfileId { get; set; }
    public int ProductId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public UserProfile UserProfile { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
