using Pikwise.Application.Favorites.DTOs;

namespace Pikwise.Application.Favorites.Interfaces;

// Identity comes from the current-user use case; callers supply only a product Id.
public interface IFavoriteService
{
    Task<IReadOnlyList<FavoriteResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FavoriteResponseDto?> AddAsync(int productId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int productId, CancellationToken cancellationToken = default);
}
