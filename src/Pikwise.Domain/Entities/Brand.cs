namespace Pikwise.Domain.Entities;

public sealed class Brand
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
