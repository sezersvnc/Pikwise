using Pikwise.Domain.Entities;

namespace Pikwise.Application.Products.Models;

// Repository results carry entities and a filtered count, without exposing IQueryable.
public sealed record ProductPageResult(IReadOnlyList<Product> Items, int TotalCount);
