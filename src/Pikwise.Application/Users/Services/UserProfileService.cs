using System.ComponentModel.DataAnnotations;
using Pikwise.Application.Users.DTOs;
using Pikwise.Application.Users.Interfaces;
using Pikwise.Domain.Entities;

namespace Pikwise.Application.Users.Services;

public sealed class UserProfileService(ICurrentUser currentUser, IUserProfileRepository repository) : IUserProfileService
{
    public async Task<UserProfileResponseDto?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var subject = currentUser.Subject;
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 128)
            return null;

        var profile = await repository.GetBySubjectAsync(subject, cancellationToken);
        if (profile is null)
        {
            // Provision once from validated identity claims. Token roles never grant local privileges.
            var email = currentUser.Email;
            if (string.IsNullOrWhiteSpace(email) || email.Length > 254 || !new EmailAddressAttribute().IsValid(email))
                return null;
            profile = await repository.AddOrGetExistingAsync(new UserProfile
            {
                AuthProviderUserId = subject, Email = email, Role = "User", CreatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }

        // Subsequent requests preserve the local role and creation time.
        return new(profile.Id, profile.AuthProviderUserId, profile.Email, profile.Role, profile.CreatedAt);
    }
}
