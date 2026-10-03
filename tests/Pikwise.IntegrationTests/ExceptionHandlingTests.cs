using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pikwise.Application.Products.DTOs;
using Pikwise.Application.Products.Exceptions;
using Pikwise.Application.Products.Interfaces;

namespace Pikwise.IntegrationTests;

public class ExceptionHandlingTests
{
    [Theory]
    [InlineData(false, HttpStatusCode.InternalServerError)]
    [InlineData(true, HttpStatusCode.Conflict)]
    public async Task Errors_are_problem_details_without_internal_exception_messages(bool conflict, HttpStatusCode expected)
    {
        await using var baseFactory = new PikwiseApiFactory();
        await using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IProductService>();
            services.AddScoped<IProductService>(_ => new FailingService(conflict));
        }));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/products/1");
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret database details", body);
        Assert.Contains("traceId", body);
    }

    private sealed class FailingService(bool conflict) : IProductService
    {
        public Task<ProductComparisonResponseDto> CompareAsync(ProductComparisonRequestDto request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProductResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            throw (conflict ? new PersistenceConflictException(new Exception("secret database details"))
                : new InvalidOperationException("secret database details"));
        public Task<PagedProductResponseDto> GetAllAsync(ProductQueryRequestDto query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProductResponseDto> CreateAsync(CreateProductRequestDto request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProductResponseDto?> UpdateAsync(int id, UpdateProductRequestDto request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
