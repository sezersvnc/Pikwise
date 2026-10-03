using Pikwise.Application.Products.Interfaces;
using Pikwise.Application.Products.Services;
using Pikwise.Domain.Entities;
using Pikwise.Application.Products.DTOs;
using Pikwise.Application.Products.Models;
using Pikwise.Application.Products.Exceptions;

namespace Pikwise.UnitTests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAll_maps_page_and_preserves_filtered_count_and_cancellation()
    {
        var repository = new StubProductRepository(Product());
        var service = new ProductService(repository);
        using var cancellation = new CancellationTokenSource();
        var query = new ProductQueryRequestDto { Page = 2, PageSize = 2 };
        var response = await service.GetAllAsync(query, cancellation.Token);
        Assert.Single(response.Items);
        Assert.Equal("PikBook", response.Items[0].Name);
        Assert.Equal(5, response.TotalCount);
        Assert.Equal(3, response.TotalPages);
        Assert.Equal(2, response.Page);
        Assert.Equal(2, response.PageSize);
        Assert.True(response.HasPreviousPage);
        Assert.True(response.HasNextPage);
        Assert.Same(query, repository.Query);
        Assert.Equal(cancellation.Token, repository.Cancellation);
    }

    [Fact]
    public async Task GetAll_rejects_invalid_non_http_request_before_repository_access()
    {
        var repository = new StubProductRepository(null);
        var service = new ProductService(repository);
        await Assert.ThrowsAsync<ProductValidationException>(() => service.GetAllAsync(
            new ProductQueryRequestDto { MinPrice = 20, MaxPrice = 10 }));
        Assert.Null(repository.Query);
    }

    [Fact]
    public void Query_defaults_and_empty_page_metadata_are_explicit()
    {
        var query = new ProductQueryRequestDto();
        Assert.Equal(1, query.Page);
        Assert.Equal(20, query.PageSize);
        Assert.Equal("id", query.SortBy);
        Assert.Equal("asc", query.SortDirection);
        var empty = new PagedProductResponseDto([], 1, 20, 0);
        Assert.Equal(0, empty.TotalPages);
        Assert.False(empty.HasPreviousPage);
        Assert.False(empty.HasNextPage);
        // Compute ceiling without overflowing for a large filtered count.
        Assert.Equal(21474837, new PagedProductResponseDto([], 1, 100, int.MaxValue).TotalPages);
    }

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
        public ProductQueryRequestDto? Query { get; private set; }
        public CancellationToken Cancellation { get; private set; }
        public Task<ProductPageResult> GetAllAsync(ProductQueryRequestDto query, CancellationToken cancellationToken = default)
        {
            Query = query;
            Cancellation = cancellationToken;
            return Task.FromResult(new ProductPageResult(product is null ? [] : [product], 5));
        }
        public Task<Product?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Brand?> GetBrandAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Category?> GetCategoryAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Add(Product value) => throw new NotSupportedException();
        public void Remove(Product value) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
