using FastEndpoints;
using FluentValidation;
using SaasPlatform.Application.Organizations;

namespace SaasPlatform.Api.Features.Organizations.Create;

public sealed class CreateOrganizationValidator : Validator<CreateOrganizationRequest>
{
    public CreateOrganizationValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OwnerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.OwnerName).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateOrganizationEndpoint(OrganizationService service)
    : Endpoint<CreateOrganizationRequest, OrganizationDto>
{
    public override void Configure()
    {
        Post("/organizations");
        AllowAnonymous();
        Description(b => b.Produces<OrganizationDto>(201).ProducesProblem(409));
    }

    public override async Task HandleAsync(CreateOrganizationRequest req, CancellationToken ct)
    {
        var dto = await service.CreateAsync(req, ct);
        await Send.ResponseAsync(dto, StatusCodes.Status201Created, ct);
    }
}
