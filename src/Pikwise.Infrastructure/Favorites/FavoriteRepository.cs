using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pikwise.Application.Favorites.Interfaces;
using Pikwise.Application.Products.Exceptions;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.Persistence;

namespace Pikwise.Infrastructure.Favorites;

public sealed class FavoriteRepository(ApplicationDbContext context) : IFavoriteRepository
{
    public async Task<IReadOnlyList<Favorite>> GetAllAsync(int userProfileId, CancellationToken cancellationToken = default) =>
        await context.Favorites.AsNoTracking().Where(f => f.UserProfileId == userProfileId)
            .Include(f => f.Product).ThenInclude(p => p.Brand)
            .Include(f => f.Product).ThenInclude(p => p.Category)
            .Include(f => f.Product).ThenInclude(p => p.LaptopSpecification)
            .OrderByDescending(f => f.CreatedAt).ThenBy(f => f.ProductId)
            .ToListAsync(cancellationToken);

    public async Task<bool> AddAsync(Favorite favorite, CancellationToken cancellationToken = default)
    {
        context.Favorites.Add(favorite);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            context.Entry(favorite).State = EntityState.Detached;
            return false;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 547 })
        {
            // A product/profile removed after validation is a write conflict, not a server error.
            throw new PersistenceConflictException(exception);
        }
    }

    public async Task<bool> DeleteAsync(int userProfileId, int productId, CancellationToken cancellationToken = default) =>
        // Delete only the caller's join row in one SQL statement; parent records are preserved.
        await context.Favorites.Where(f => f.UserProfileId == userProfileId && f.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken) > 0;
}
