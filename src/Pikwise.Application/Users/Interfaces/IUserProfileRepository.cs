using Pikwise.Domain.Entities;

namespace Pikwise.Application.Users.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetBySubjectAsync(string subject, CancellationToken cancellationToken = default);
    Task<UserProfile> AddOrGetExistingAsync(UserProfile profile, CancellationToken cancellationToken = default);
}
