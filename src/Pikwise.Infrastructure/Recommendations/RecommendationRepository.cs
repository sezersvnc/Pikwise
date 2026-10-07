using Microsoft.EntityFrameworkCore;
using Pikwise.Application.Recommendations.Interfaces;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.Persistence;

namespace Pikwise.Infrastructure.Recommendations;

public sealed class RecommendationRepository(ApplicationDbContext context) : IRecommendationRepository
{
    // One untracked query loads the relationships the engine and mapper read; Category is not needed.
    public async Task<IReadOnlyList<Product>> GetCandidatesAsync(CancellationToken cancellationToken = default) =>
        await context.Products.AsNoTracking()
            .Include(product => product.Brand)
            .Include(product => product.LaptopSpecification)
            .ToListAsync(cancellationToken);
}
