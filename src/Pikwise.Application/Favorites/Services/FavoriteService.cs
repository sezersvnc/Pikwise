using Pikwise.Application.Favorites.DTOs;
using Pikwise.Application.Favorites.Exceptions;
using Pikwise.Application.Favorites.Interfaces;
using Pikwise.Application.Favorites.Mappers;
using Pikwise.Application.Products.Interfaces;
using Pikwise.Application.Users.Exceptions;
using Pikwise.Application.Users.Interfaces;
using Pikwise.Domain.Entities;

namespace Pikwise.Application.Favorites.Services;

public sealed class FavoriteService(IUserProfileService profiles, IFavoriteRepository favorites, IProductRepository products)
    : IFavoriteService
{
    public async Task<IReadOnlyList<FavoriteResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var userId = await GetCurrentProfileIdAsync(cancellationToken);
        return (await favorites.GetAllAsync(userId, cancellationToken)).Select(f => f.ToResponseDto()).ToList();
    }

    public async Task<FavoriteResponseDto?> AddAsync(int productId, CancellationToken cancellationToken = default)
    {
        var userId = await GetCurrentProfileIdAsync(cancellationToken);
        var product = await products.GetByIdAsync(productId, cancellationToken);
        if (product is null) return null;

        var favorite = new Favorite
        {
            UserProfileId = userId, ProductId = product.Id, CreatedAt = DateTimeOffset.UtcNow
        };
        // The SQL primary key is authoritative when two requests add the same favorite.
        if (!await favorites.AddAsync(favorite, cancellationToken))
            throw new FavoriteAlreadyExistsException();
        favorite.Product = product;
        return favorite.ToResponseDto();
    }

    public async Task<bool> DeleteAsync(int productId, CancellationToken cancellationToken = default)
    {
        var userId = await GetCurrentProfileIdAsync(cancellationToken);
        return await favorites.DeleteAsync(userId, productId, cancellationToken);
    }

    private async Task<int> GetCurrentProfileIdAsync(CancellationToken cancellationToken)
    {
        // Reuse Session 7's validated subject and first-access profile provisioning.
        var profile = await profiles.GetCurrentAsync(cancellationToken);
        return profile?.Id ?? throw new UserProfileUnavailableException();
    }
}
