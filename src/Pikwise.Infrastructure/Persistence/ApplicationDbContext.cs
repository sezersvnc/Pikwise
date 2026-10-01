using Microsoft.EntityFrameworkCore;

namespace Pikwise.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options);
