using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pikwise.Application.Users.Interfaces;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.Persistence;

namespace Pikwise.Infrastructure.Users;

public sealed class UserProfileRepository(ApplicationDbContext context) : IUserProfileRepository
{
    public Task<UserProfile?> GetBySubjectAsync(string subject, CancellationToken cancellationToken = default) =>
        context.UserProfiles.AsNoTracking().SingleOrDefaultAsync(u => u.AuthProviderUserId == subject, cancellationToken);

    public async Task<UserProfile> AddOrGetExistingAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        context.UserProfiles.Add(profile);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return profile;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // Two first requests can race; the unique subject index decides which insert wins.
            context.Entry(profile).State = EntityState.Detached;
            var existing = await GetBySubjectAsync(profile.AuthProviderUserId, cancellationToken);
            if (existing is null) throw;
            return existing;
        }
    }
}
