using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaasPlatform.Domain.Entities;

namespace SaasPlatform.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Slug)
            .IsRequired()
            .HasMaxLength(220);

        builder.HasIndex(o => o.Slug).IsUnique();

        builder.Property(o => o.CreatedAt).IsRequired();

        // One-to-one: an organization owns exactly one subscription.
        builder.HasOne(o => o.Subscription)
            .WithOne()
            .HasForeignKey<Subscription>(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many: an organization has many users. Backed by the private _users field.
        builder.HasMany(o => o.Users)
            .WithOne()
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Users)
            .HasField("_users")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(o => o.Subscription).AutoInclude();
    }
}
