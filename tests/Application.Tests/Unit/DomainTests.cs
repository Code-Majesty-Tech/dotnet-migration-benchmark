using SaasPlatform.Domain.Entities;
using SaasPlatform.Domain.Enums;
using SaasPlatform.Domain.Exceptions;
using Xunit;

namespace SaasPlatform.Tests.Unit;

public class DomainTests
{
    [Fact]
    public void Create_seeds_owner_and_free_subscription()
    {
        var org = Organization.Create("Acme Inc", "owner@acme.test", "Olivia Owner");

        Assert.Equal("acme-inc", org.Slug);
        Assert.Equal(SubscriptionPlan.Free, org.Subscription.Plan);
        Assert.Equal(SubscriptionStatus.Active, org.Subscription.Status);
        Assert.Equal(1, org.ActiveMemberCount);
        Assert.Single(org.Users);
        Assert.Equal(UserRole.Owner, org.Users.Single().Role);
    }

    [Fact]
    public void AddMember_enforces_seat_limit_of_current_plan()
    {
        var org = Organization.Create("Acme Inc", "owner@acme.test", "Olivia Owner");

        // Free plan allows 3 seats; owner already occupies one.
        org.AddMember("a@acme.test", "A");
        org.AddMember("b@acme.test", "B");

        var ex = Assert.Throws<DomainException>(() => org.AddMember("c@acme.test", "C"));
        Assert.Contains("Free plan", ex.Message);
    }

    [Fact]
    public void AddMember_rejects_duplicate_email()
    {
        var org = Organization.Create("Acme Inc", "owner@acme.test", "Olivia Owner");

        org.AddMember("dup@acme.test", "First");

        Assert.Throws<DomainException>(() => org.AddMember("DUP@acme.test", "Second"));
    }

    [Fact]
    public void ChangePlan_raises_seat_capacity_and_activates_trial()
    {
        var org = Organization.Create("Acme Inc", "owner@acme.test", "Olivia Owner");

        org.Subscription.ChangePlan(SubscriptionPlan.Pro);

        Assert.Equal(50, org.Subscription.Seats);
        Assert.Equal(SubscriptionStatus.Active, org.Subscription.Status);
    }

    [Fact]
    public void Canceled_subscription_cannot_change_plan()
    {
        var org = Organization.Create("Acme Inc", "owner@acme.test", "Olivia Owner");

        org.Subscription.Cancel();

        Assert.Throws<DomainException>(() => org.Subscription.ChangePlan(SubscriptionPlan.Starter));
    }

    [Fact]
    public void Owner_cannot_be_deactivated()
    {
        var org = Organization.Create("Acme Inc", "owner@acme.test", "Olivia Owner");

        Assert.Throws<DomainException>(() => org.Users.Single().Deactivate());
    }
}
