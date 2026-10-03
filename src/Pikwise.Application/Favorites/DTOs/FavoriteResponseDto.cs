using Pikwise.Application.Products.DTOs;

namespace Pikwise.Application.Favorites.DTOs;

// Return the saved product and favorite time without exposing profile data.
public sealed record FavoriteResponseDto(DateTimeOffset CreatedAt, ProductResponseDto Product);
