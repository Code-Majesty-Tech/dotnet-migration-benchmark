using FastEndpoints;
using SaasPlatform.Application.Organizations;

namespace SaasPlatform.Api.Features.Organizations.AddMember;

public sealed class AddMemberEndpoint(OrganizationService service)
    : Endpoint<AddMemberEndpoint.Request, MemberDto>
{
    /// <summary>Body of the request; the organization id comes from the route.</summary>
    public sealed record Request(Guid OrganizationId, string Email, string FullName, SaasPlatform.Domain.Enums.UserRole Role);

    public override void Configure()
    {
        Post("/organizations/{organizationId}/members");
        AllowAnonymous();
        Description(b => b.Produces<MemberDto>(201).ProducesProblem(400).ProducesProblem(404));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var dto = await service.AddMemberAsync(
            req.OrganizationId,
            new AddMemberRequest(req.Email, req.FullName, req.Role),
            ct);

        await SendAsync(dto, StatusCodes.Status201Created, ct);
    }
}
