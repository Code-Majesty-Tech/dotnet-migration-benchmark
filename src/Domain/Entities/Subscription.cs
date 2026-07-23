using SaasPlatform.Domain.Common;
using SaasPlatform.Domain.Enums;
using SaasPlatform.Domain.Exceptions;

namespace SaasPlatform.Domain.Entities;

/// <summary>
/// The billing subscription owned by an <see cref="Organization"/> (one-to-one).
/// Governs how many seats the organization may fill.
/// </summary>
public class Subscription : Entity
{
    private Subscription() { } // EF Core

    private Subscription(Guid organizationId, SubscriptionPlan plan)
    {
        OrganizationId = organizationId;
        Plan = plan;
        Status = plan == SubscriptionPlan.Free ? SubscriptionStatus.Active : SubscriptionStatus.Trialing;
        Seats = SeatLimitFor(plan);
        CurrentPeriodStart = DateTimeOffset.UtcNow;
        CurrentPeriodEnd = CurrentPeriodStart.AddMonths(1);
    }

    public Guid OrganizationId { get; private set; }

    public SubscriptionPlan Plan { get; private set; }

    public SubscriptionStatus Status { get; private set; }

    /// <summary>Maximum number of users the organization may have on this plan.</summary>
    public int Seats { get; private set; }

    public DateTimeOffset CurrentPeriodStart { get; private set; }

    public DateTimeOffset CurrentPeriodEnd { get; private set; }

    public static Subscription Start(Guid organizationId, SubscriptionPlan plan) => new(organizationId, plan);

    public void ChangePlan(SubscriptionPlan plan)
    {
        if (Status == SubscriptionStatus.Canceled)
            throw new DomainException("Cannot change the plan of a canceled subscription.");

        Plan = plan;
        Seats = SeatLimitFor(plan);
        if (plan != SubscriptionPlan.Free && Status == SubscriptionStatus.Trialing)
            Status = SubscriptionStatus.Active;
        Touch();
    }

    public void Renew()
    {
        if (Status == SubscriptionStatus.Canceled)
            throw new DomainException("Cannot renew a canceled subscription.");

        CurrentPeriodStart = DateTimeOffset.UtcNow;
        CurrentPeriodEnd = CurrentPeriodStart.AddMonths(1);
        Status = SubscriptionStatus.Active;
        Touch();
    }

    public void Cancel()
    {
        Status = SubscriptionStatus.Canceled;
        Touch();
    }

    public static int SeatLimitFor(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Free => 3,
        SubscriptionPlan.Starter => 10,
        SubscriptionPlan.Pro => 50,
        SubscriptionPlan.Enterprise => int.MaxValue,
        _ => throw new DomainException($"Unknown plan '{plan}'.")
    };
}
