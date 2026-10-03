using Pikwise.Domain.Entities;

namespace Pikwise.Application.Favorites.Interfaces;

public interface IFavoriteRepository
{
    Task<IReadOnlyList<Favorite>> GetAllAsync(int userProfileId, CancellationToken cancellationToken = default);
    Task<bool> AddAsync(Favorite favorite, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int userProfileId, int productId, CancellationToken cancellationToken = default);
}
