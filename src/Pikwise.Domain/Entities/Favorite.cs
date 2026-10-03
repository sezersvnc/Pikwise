namespace Pikwise.Domain.Entities;

// An explicit join entity lets each user-product relationship store its creation time.
public sealed class Favorite
{
    // Together, these foreign keys identify a favorite; no separate Id is needed.
    public int UserProfileId { get; set; }
    public int ProductId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public UserProfile UserProfile { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
