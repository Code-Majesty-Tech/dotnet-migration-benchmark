using SaasPlatform.Domain.Common;
using SaasPlatform.Domain.Enums;
using SaasPlatform.Domain.Exceptions;

namespace SaasPlatform.Domain.Entities;

/// <summary>
/// A member of an <see cref="Organization"/>.
/// </summary>
public class User : Entity
{
    private User() { } // EF Core

    internal User(Guid organizationId, string email, string fullName, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("A user must have an email address.");

        OrganizationId = organizationId;
        Email = email.Trim().ToLowerInvariant();
        FullName = fullName.Trim();
        Role = role;
        IsActive = true;
    }

    public Guid OrganizationId { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public void ChangeRole(UserRole role)
    {
        Role = role;
        Touch();
    }

    public void Deactivate()
    {
        if (Role == UserRole.Owner)
            throw new DomainException("The organization owner cannot be deactivated.");

        IsActive = false;
        Touch();
    }
}
