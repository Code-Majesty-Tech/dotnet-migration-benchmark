using FastEndpoints;
using SaasPlatform.Application.Organizations;

namespace SaasPlatform.Api.Features.Organizations.GetById;

public sealed record GetOrganizationRequest(Guid Id);

public sealed class GetOrganizationEndpoint(OrganizationService service)
    : Endpoint<GetOrganizationRequest, OrganizationDto>
{
    public override void Configure()
    {
        Get("/organizations/{id}");
        AllowAnonymous();
        Description(b => b.Produces<OrganizationDto>(200).ProducesProblem(404));
    }

    public override async Task HandleAsync(GetOrganizationRequest req, CancellationToken ct)
    {
        var dto = await service.GetByIdAsync(req.Id, ct);
        await Send.OkAsync(dto, ct);
    }
}
