namespace Pikwise.Application.Users.DTOs;

// Only the authenticated user's own profile is exposed; navigation collections are excluded.
public sealed record UserProfileResponseDto(int Id, string AuthProviderUserId, string Email, string Role, DateTimeOffset CreatedAt);
