namespace Pikwise.Domain.Entities;

public sealed class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public int BrandId { get; set; }
    public int CategoryId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Brand Brand { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public LaptopSpecification? LaptopSpecification { get; set; }
}
