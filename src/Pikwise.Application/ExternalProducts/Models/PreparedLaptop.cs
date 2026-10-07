namespace Pikwise.Application.ExternalProducts.Models;

// One manifest entry after fetching and normalizing. Record is null when nothing usable was obtained.
public sealed record PreparedLaptop(
    string ExternalId,
    ExternalLaptopRecord? Record,
    IReadOnlyList<string> Issues,
    string? FailureReason)
{
    public bool IsUsable => Record is not null && FailureReason is null;
}
