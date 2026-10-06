namespace Pikwise.Domain.Entities;

// Every specification value is optional: a source may not supply it, and unknown is never guessed.
public sealed class LaptopSpecification
{
    public int Id { get; set; }
    public string? Processor { get; set; }
    public string? GPU { get; set; }
    public int? RamGb { get; set; }
    public int? StorageGb { get; set; }
    public decimal? ScreenSize { get; set; }
    public string? Resolution { get; set; }
    public int? RefreshRate { get; set; }
    public decimal? Weight { get; set; }
    public string? OperatingSystem { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
