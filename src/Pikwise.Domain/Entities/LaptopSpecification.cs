namespace Pikwise.Domain.Entities;

public sealed class LaptopSpecification
{
    public int Id { get; set; }
    public required string Processor { get; set; }
    public required string GPU { get; set; }
    public int RamGb { get; set; }
    public int StorageGb { get; set; }
    public decimal ScreenSize { get; set; }
    public required string Resolution { get; set; }
    public int RefreshRate { get; set; }
    public decimal Weight { get; set; }
    public required string OperatingSystem { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
