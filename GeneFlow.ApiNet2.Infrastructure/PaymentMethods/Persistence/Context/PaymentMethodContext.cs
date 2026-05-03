using GeneFlow.ApiNet2.Domain.PaymentMethods;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.PaymentMethods.Persistence.Context;

/// <summary>
/// DbContext for payment methods.
/// </summary>
public sealed class PaymentMethodContext : DbContext
{
    public PaymentMethodContext(DbContextOptions<PaymentMethodContext> options) : base(options)
    {
    }

    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("billing");

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("payment_methods");

            entity.HasKey(e => e.Id);

            // Configure PaymentMethodId (stored as string: M00000001)
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasMaxLength(9)
                .HasConversion(
                    id => id.ToString(),
                    value => PaymentMethodId.Parse(value));

            // Configure UserId (stored as string: U00000001)
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasMaxLength(9)
                .IsRequired()
                .HasConversion(
                    id => id.ToString(),
                    value => Domain.Identity.UserId.Parse(value));

            entity.Property(e => e.StripePaymentMethodId)
                .HasColumnName("stripe_payment_method_id")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Brand)
                .HasColumnName("brand")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Last4)
                .HasColumnName("last4")
                .HasMaxLength(4)
                .IsRequired();

            entity.Property(e => e.ExpiryMonth)
                .HasColumnName("expiry_month")
                .IsRequired();

            entity.Property(e => e.ExpiryYear)
                .HasColumnName("expiry_year")
                .IsRequired();

            entity.Property(e => e.IsDefault)
                .HasColumnName("is_default")
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(e => e.ModifiedAt)
                .HasColumnName("modified_at");

            // Indexes
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_payment_methods_user_id");

            entity.HasIndex(e => e.StripePaymentMethodId)
                .IsUnique()
                .HasDatabaseName("ix_payment_methods_stripe_id");

            entity.HasIndex(e => new { e.UserId, e.IsDefault })
                .HasDatabaseName("ix_payment_methods_user_default");
        });
    }
}
