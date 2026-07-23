using SaasPlatform.Domain.Common;
using SaasPlatform.Domain.Enums;
using SaasPlatform.Domain.Exceptions;

namespace SaasPlatform.Domain.Entities;

/// <summary>
/// Aggregate root representing a tenant. Owns its members and a single billing subscription.
/// </summary>
public class Organization : Entity
{
    private readonly List<User> _users = [];

    private Organization() { } // EF Core

    private Organization(string name, string slug, string ownerEmail, string ownerName)
    {
        Name = name.Trim();
        Slug = slug;
        Subscription = Subscription.Start(Id, SubscriptionPlan.Free);
        // Seed the owner as the first member.
        _users.Add(new User(Id, ownerEmail, ownerName, UserRole.Owner));
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>URL-friendly unique identifier, e.g. "acme-inc".</summary>
    public string Slug { get; private set; } = string.Empty;

    public Subscription Subscription { get; private set; } = null!;

    public IReadOnlyCollection<User> Users => _users.AsReadOnly();

    public int ActiveMemberCount => _users.Count(u => u.IsActive);

    public static Organization Create(string name, string ownerEmail, string ownerName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Organization name is required.");

        return new Organization(name, Slugify(name), ownerEmail, ownerName);
    }

    public User AddMember(string email, string fullName, UserRole role = UserRole.Member)
    {
        if (role == UserRole.Owner)
            throw new DomainException("An organization can only have one owner.");

        if (ActiveMemberCount >= Subscription.Seats)
            throw new DomainException(
                $"The {Subscription.Plan} plan allows at most {Subscription.Seats} seats.");

        var normalized = email.Trim().ToLowerInvariant();
        if (_users.Any(u => u.Email == normalized))
            throw new DomainException($"A user with email '{normalized}' already exists.");

        var user = new User(Id, email, fullName, role);
        _users.Add(user);
        Touch();
        return user;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Organization name is required.");

        Name = name.Trim();
        Touch();
    }

    private static string Slugify(string value)
    {
        var slug = new string(value
            .Trim()
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray());

        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");

        return slug.Trim('-');
    }
}
