namespace SaasPlatform.Domain.Enums;

public enum SubscriptionPlan
{
    Free = 0,
    Starter = 1,
    Pro = 2,
    Enterprise = 3
}

public enum SubscriptionStatus
{
    Trialing = 0,
    Active = 1,
    PastDue = 2,
    Canceled = 3
}

public enum UserRole
{
    Member = 0,
    Admin = 1,
    Owner = 2
}
