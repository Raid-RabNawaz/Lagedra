using Lagedra.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Auth.Infrastructure.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("user_profiles");

        builder.HasKey(p => p.UserId);

        builder.HasOne(p => p.User)
            .WithOne()
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.DisplayName).HasMaxLength(100);
        builder.Property(p => p.PhoneNumber).HasMaxLength(30);
        builder.Property(p => p.Bio).HasMaxLength(2000);
        builder.Property(p => p.ProfilePhotoUrl).HasMaxLength(2000);
        builder.Property(p => p.City).HasMaxLength(200);
        builder.Property(p => p.State).HasMaxLength(100);
        builder.Property(p => p.Country).HasMaxLength(100);
        builder.Property(p => p.Work).HasMaxLength(200);
        builder.Property(p => p.Languages).HasMaxLength(500);
        builder.Property(p => p.EmergencyContactName).HasMaxLength(200);
        builder.Property(p => p.EmergencyContactPhone).HasMaxLength(30);
        builder.Property(p => p.GovernmentIdVerified).IsRequired();
        builder.Property(p => p.PhoneVerified).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
    }
}
