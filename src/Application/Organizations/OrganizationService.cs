using Microsoft.EntityFrameworkCore;
using SaasPlatform.Application.Common;
using SaasPlatform.Domain.Entities;
using SaasPlatform.Domain.Enums;

namespace SaasPlatform.Application.Organizations;

// --- Contracts -------------------------------------------------------------

public sealed record CreateOrganizationRequest(string Name, string OwnerEmail, string OwnerName);

public sealed record AddMemberRequest(string Email, string FullName, UserRole Role);

public sealed record MemberDto(Guid Id, string Email, string FullName, UserRole Role, bool IsActive);

public sealed record OrganizationDto(
    Guid Id,
    string Name,
    string Slug,
    SubscriptionPlan Plan,
    SubscriptionStatus Status,
    int Seats,
    int ActiveMembers,
    DateTimeOffset CreatedAt)
{
    public static OrganizationDto FromEntity(Organization org) => new(
        org.Id,
        org.Name,
        org.Slug,
        org.Subscription.Plan,
        org.Subscription.Status,
        org.Subscription.Seats,
        org.ActiveMemberCount,
        org.CreatedAt);
}

// --- Service ---------------------------------------------------------------

public sealed class OrganizationService(IAppDbContext db)
{
    private readonly IAppDbContext _db = db;

    public async Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request, CancellationToken ct = default)
    {
        var organization = Organization.Create(request.Name, request.OwnerEmail, request.OwnerName);

        var slugTaken = await _db.Organizations.AnyAsync(o => o.Slug == organization.Slug, ct);
        if (slugTaken)
            throw new ConflictException($"An organization with slug '{organization.Slug}' already exists.");

        _db.Organizations.Add(organization);
        await _db.SaveChangesAsync(ct);

        return OrganizationDto.FromEntity(organization);
    }

    public async Task<OrganizationDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var organization = await LoadAsync(id, ct);
        return OrganizationDto.FromEntity(organization);
    }

    public async Task<IReadOnlyList<OrganizationDto>> ListAsync(CancellationToken ct = default)
    {
        var organizations = await _db.Organizations
            .Include(o => o.Subscription)
            .Include(o => o.Users)
            .OrderBy(o => o.Name)
            .AsNoTracking()
            .ToListAsync(ct);

        return organizations.Select(OrganizationDto.FromEntity).ToList();
    }

    public async Task<MemberDto> AddMemberAsync(Guid id, AddMemberRequest request, CancellationToken ct = default)
    {
        var organization = await LoadAsync(id, ct);

        var member = organization.AddMember(request.Email, request.FullName, request.Role);
        await _db.SaveChangesAsync(ct);

        return new MemberDto(member.Id, member.Email, member.FullName, member.Role, member.IsActive);
    }

    private async Task<Organization> LoadAsync(Guid id, CancellationToken ct)
    {
        var organization = await _db.Organizations
            .Include(o => o.Subscription)
            .Include(o => o.Users)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        return organization ?? throw new NotFoundException($"Organization '{id}' was not found.");
    }
}
