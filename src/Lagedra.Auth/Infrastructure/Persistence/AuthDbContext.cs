using Lagedra.Auth.Domain;
using Lagedra.Infrastructure.Persistence;
using Lagedra.SharedKernel.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Auth.Infrastructure.Persistence;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);

        // Move all Identity tables into the "auth" schema
        builder.HasDefaultSchema("auth");

        builder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}
