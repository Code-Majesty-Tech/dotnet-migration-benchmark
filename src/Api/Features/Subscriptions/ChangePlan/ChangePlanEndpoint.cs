using FastEndpoints;
using SaasPlatform.Application.Subscriptions;
using SaasPlatform.Domain.Enums;

namespace SaasPlatform.Api.Features.Subscriptions.ChangePlan;

public sealed class ChangePlanEndpoint(SubscriptionService service)
    : Endpoint<ChangePlanEndpoint.Request, SubscriptionDto>
{
    public sealed record Request(Guid OrganizationId, SubscriptionPlan Plan);

    public override void Configure()
    {
        Put("/organizations/{organizationId}/subscription/plan");
        AllowAnonymous();
        Description(b => b.Produces<SubscriptionDto>(200).ProducesProblem(400).ProducesProblem(404));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var dto = await service.ChangePlanAsync(req.OrganizationId, new ChangePlanRequest(req.Plan), ct);
        await Send.OkAsync(dto, ct);
    }
}
