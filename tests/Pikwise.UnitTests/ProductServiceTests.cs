using Pikwise.Application.Products.Interfaces;
using Pikwise.Application.Products.Services;
using Pikwise.Domain.Entities;

namespace Pikwise.UnitTests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetById_maps_repository_entity_to_response_contract()
    {
        var product = Product();
        var service = new ProductService(new StubProductRepository(product));

        var response = await service.GetByIdAsync(product.Id);

        Assert.NotNull(response);
        Assert.Equal(product.Id, response.Id);
        Assert.Equal("PikBook", response.Name);
        Assert.Equal("Pikwise", response.Brand.Name);
        Assert.Equal("Laptop", response.Category.Name);
        Assert.Equal("Core Test", response.Specification?.Processor);
    }

    [Fact]
    public async Task GetById_returns_null_when_repository_finds_nothing()
    {
        var service = new ProductService(new StubProductRepository(null));

        var response = await service.GetByIdAsync(404);

        Assert.Null(response);
    }

    private static Product Product() => new()
    {
        Id = 7,
        Name = "PikBook",
        Price = 34999.90m,
        Stock = 4,
        IsActive = true,
        BrandId = 2,
        CategoryId = 3,
        CreatedAt = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero),
        Brand = new Brand { Id = 2, Name = "Pikwise" },
        Category = new Category { Id = 3, Name = "Laptop" },
        LaptopSpecification = new LaptopSpecification
        {
            Processor = "Core Test", GPU = "GPU Test", RamGb = 16, StorageGb = 512,
            ScreenSize = 15.6m, Resolution = "1920x1080", RefreshRate = 144,
            Weight = 1.75m, OperatingSystem = "Test OS", ProductId = 7
        }
    };

    private sealed class StubProductRepository(Product? product) : IProductRepository
    {
        public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(product?.Id == id ? product : null);
        public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Product?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Brand?> GetBrandAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Category?> GetCategoryAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Add(Product value) => throw new NotSupportedException();
        public void Remove(Product value) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
