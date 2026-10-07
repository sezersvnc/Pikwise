using Pikwise.Domain.Entities;

namespace Pikwise.Application.Recommendations.Interfaces;

// Application defines the data-access contract; Infrastructure supplies the EF implementation.
public interface IRecommendationRepository
{
    // Every product with Brand and LaptopSpecification, untracked. Eligibility and hard
    // constraints are business rules and are applied by the engine, not by this query.
    Task<IReadOnlyList<Product>> GetCandidatesAsync(CancellationToken cancellationToken = default);
}
