using Microsoft.EntityFrameworkCore;
using SaasPlatform.Application.Common;
using SaasPlatform.Domain.Entities;
using SaasPlatform.Domain.Enums;

namespace SaasPlatform.Application.Subscriptions;

// --- Contracts -------------------------------------------------------------

public sealed record ChangePlanRequest(SubscriptionPlan Plan);

public sealed record SubscriptionDto(
    Guid Id,
    Guid OrganizationId,
    SubscriptionPlan Plan,
    SubscriptionStatus Status,
    int Seats,
    DateTimeOffset CurrentPeriodStart,
    DateTimeOffset CurrentPeriodEnd)
{
    public static SubscriptionDto FromEntity(Subscription s) => new(
        s.Id,
        s.OrganizationId,
        s.Plan,
        s.Status,
        s.Seats,
        s.CurrentPeriodStart,
        s.CurrentPeriodEnd);
}

// --- Service ---------------------------------------------------------------

public sealed class SubscriptionService(IAppDbContext db)
{
    private readonly IAppDbContext _db = db;

    public async Task<SubscriptionDto> GetForOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var subscription = await LoadAsync(organizationId, ct);
        return SubscriptionDto.FromEntity(subscription);
    }

    public async Task<SubscriptionDto> ChangePlanAsync(Guid organizationId, ChangePlanRequest request, CancellationToken ct = default)
    {
        var subscription = await LoadAsync(organizationId, ct);

        subscription.ChangePlan(request.Plan);
        await _db.SaveChangesAsync(ct);

        return SubscriptionDto.FromEntity(subscription);
    }

    public async Task<SubscriptionDto> CancelAsync(Guid organizationId, CancellationToken ct = default)
    {
        var subscription = await LoadAsync(organizationId, ct);

        subscription.Cancel();
        await _db.SaveChangesAsync(ct);

        return SubscriptionDto.FromEntity(subscription);
    }

    private async Task<Subscription> LoadAsync(Guid organizationId, CancellationToken ct)
    {
        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);

        return subscription
            ?? throw new NotFoundException($"No subscription found for organization '{organizationId}'.");
    }
}
