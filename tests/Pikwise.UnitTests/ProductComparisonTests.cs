using Pikwise.Application.Products.DTOs;
using Pikwise.Application.Products.Exceptions;
using Pikwise.Application.Products.Interfaces;
using Pikwise.Application.Products.Models;
using Pikwise.Application.Products.Services;
using Pikwise.Domain.Entities;

namespace Pikwise.UnitTests;

public class ProductComparisonTests
{
    [Fact]
    public async Task Compare_preserves_selection_order_maps_facts_and_forwards_cancellation()
    {
        var first = Product(10);
        var second = Product(20);
        second.LaptopSpecification = null;
        var repository = new ComparisonRepository([first, second]);
        using var cancellation = new CancellationTokenSource();
        var request = new ProductComparisonRequestDto { Ids = [20, 10] };
        var response = await new ProductService(repository).CompareAsync(request, cancellation.Token);

        Assert.Equal(new[] { 20, 10 }, response.Products.Select(p => p.Id));
        Assert.Null(response.Products[0].Specification);
        var actual = response.Products[1];
        Assert.Equal(first.Name, actual.Name);
        Assert.Equal(first.Price, actual.Price);
        Assert.Equal(first.Brand.Name, actual.Brand.Name);
        Assert.Equal("CPU Test", actual.Specification!.Processor);
        Assert.Equal("GPU Test", actual.Specification.GPU);
        Assert.Equal(16, actual.Specification.RamGb);
        Assert.Equal(512, actual.Specification.StorageGb);
        Assert.Equal(15.6m, actual.Specification.ScreenSize);
        Assert.Equal("1920x1080", actual.Specification.Resolution);
        Assert.Equal(144, actual.Specification.RefreshRate);
        Assert.Equal(1.75m, actual.Specification.Weight);
        Assert.Equal("Test OS", actual.Specification.OperatingSystem);
        Assert.Equal(1, repository.CallCount);
        Assert.Equal(request.Ids, repository.Ids);
        Assert.Equal(cancellation.Token, repository.Cancellation);
    }

    [Theory]
    [InlineData(new int[] { })]
    [InlineData(new[] { 1 })]
    [InlineData(new[] { 1, 2, 3, 4 })]
    [InlineData(new[] { 1, 1 })]
    [InlineData(new[] { 1, 2, 1 })]
    [InlineData(new[] { 0, 2 })]
    [InlineData(new[] { -1, 2 })]
    public async Task Compare_rejects_invalid_selections_before_data_access(int[] ids)
    {
        var repository = new ComparisonRepository([]);
        await Assert.ThrowsAsync<ProductValidationException>(() => new ProductService(repository)
            .CompareAsync(new ProductComparisonRequestDto { Ids = ids }));
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task Compare_reports_all_missing_ids_in_request_order()
    {
        var repository = new ComparisonRepository([Product(10)]);
        var error = await Assert.ThrowsAsync<ProductsNotFoundException>(() => new ProductService(repository)
            .CompareAsync(new ProductComparisonRequestDto { Ids = [30, 10, 20] }));
        Assert.Equal(new[] { 30, 20 }, error.MissingProductIds);
        Assert.Equal(1, repository.CallCount);
    }

    private static Product Product(int id) => new()
    {
        Id = id, Name = "Laptop " + id, Price = 40000.50m,
        Brand = new Brand { Id = 1, Name = "Test brand" },
        LaptopSpecification = new LaptopSpecification
        {
            Processor = "CPU Test", GPU = "GPU Test", RamGb = 16, StorageGb = 512,
            ScreenSize = 15.6m, Resolution = "1920x1080", RefreshRate = 144, Weight = 1.75m, OperatingSystem = "Test OS"
        }
    };

    private sealed class ComparisonRepository(IReadOnlyList<Product> products) : IProductRepository
    {
        public int CallCount { get; private set; }
        public IReadOnlyList<int>? Ids { get; private set; }
        public CancellationToken Cancellation { get; private set; }
        public Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
        {
            CallCount++;
            Ids = ids;
            Cancellation = cancellationToken;
            return Task.FromResult(products);
        }
        public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProductPageResult> GetAllAsync(ProductQueryRequestDto query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Product?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Brand?> GetBrandAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Category?> GetCategoryAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Add(Product product) => throw new NotSupportedException();
        public void Remove(Product product) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
