using Pikwise.Application.Favorites.DTOs;
using Pikwise.Application.Products.Mappers;
using Pikwise.Domain.Entities;

namespace Pikwise.Application.Favorites.Mappers;

public static class FavoriteMapper
{
    public static FavoriteResponseDto ToResponseDto(this Favorite favorite) =>
        new(favorite.CreatedAt, favorite.Product.ToResponseDto());
}
