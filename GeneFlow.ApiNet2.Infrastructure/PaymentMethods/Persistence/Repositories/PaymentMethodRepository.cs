using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.PaymentMethods;
using GeneFlow.ApiNet2.Infrastructure.PaymentMethods.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.PaymentMethods.Persistence.Repositories;

/// <summary>
/// Repository implementation for payment methods.
/// </summary>
public sealed class PaymentMethodRepository : IPaymentMethodRepository
{
    private readonly PaymentMethodContext _context;

    public PaymentMethodRepository(PaymentMethodContext context)
    {
        _context = context;
    }

    public async Task<PaymentMethod?> GetByIdAsync(PaymentMethodId id, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentMethod>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentMethods
            .Where(pm => pm.UserId == userId)
            .OrderByDescending(pm => pm.IsDefault)
            .ThenByDescending(pm => pm.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentMethod?> GetDefaultByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.UserId == userId && pm.IsDefault, cancellationToken);
    }

    public async Task<PaymentMethod?> GetByStripeIdAsync(string stripePaymentMethodId, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.StripePaymentMethodId == stripePaymentMethodId, cancellationToken);
    }

    public async Task AddAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default)
    {
        await _context.PaymentMethods.AddAsync(paymentMethod, cancellationToken);
    }

    public Task UpdateAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default)
    {
        _context.PaymentMethods.Update(paymentMethod);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default)
    {
        _context.PaymentMethods.Remove(paymentMethod);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
