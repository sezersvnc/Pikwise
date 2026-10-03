using Pikwise.Application.Users.DTOs;

namespace Pikwise.Application.Users.Interfaces;

public interface IUserProfileService
{
    Task<UserProfileResponseDto?> GetCurrentAsync(CancellationToken cancellationToken = default);
}
