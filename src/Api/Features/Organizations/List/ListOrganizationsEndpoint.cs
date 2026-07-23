using FastEndpoints;
using SaasPlatform.Application.Organizations;

namespace SaasPlatform.Api.Features.Organizations.List;

public sealed class ListOrganizationsEndpoint(OrganizationService service)
    : EndpointWithoutRequest<IReadOnlyList<OrganizationDto>>
{
    public override void Configure()
    {
        Get("/organizations");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var organizations = await service.ListAsync(ct);
        await SendAsync(organizations, cancellation: ct);
    }
}
