using Microsoft.EntityFrameworkCore;
using SaasPlatform.Domain.Entities;

namespace SaasPlatform.Application.Common;

/// <summary>
/// Abstraction over the persistence context so the Application layer stays
/// independent of the concrete EF Core implementation in Infrastructure.
/// </summary>
public interface IAppDbContext
{
    DbSet<Organization> Organizations { get; }

    DbSet<User> Users { get; }

    DbSet<Subscription> Subscriptions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
