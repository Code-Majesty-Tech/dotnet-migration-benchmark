using System.Net;
using System.Net.Http.Json;
using SaasPlatform.Application.Organizations;
using SaasPlatform.Application.Subscriptions;
using SaasPlatform.Domain.Enums;
using Xunit;

namespace SaasPlatform.Tests.Integration;

public class OrganizationApiTests
{
    private static CreateOrganizationRequest NewOrg(string name = "Acme Inc") =>
        new(name, "owner@acme.test", "Olivia Owner");

    [Fact]
    public async Task Create_organization_returns_201_with_free_plan()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateConfiguredClient();

        var response = await client.PostAsJsonAsync("/api/organizations", NewOrg());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<OrganizationDto>();
        Assert.NotNull(dto);
        Assert.Equal("acme-inc", dto!.Slug);
        Assert.Equal(SubscriptionPlan.Free, dto.Plan);
        Assert.Equal(1, dto.ActiveMembers);
    }

    [Fact]
    public async Task Get_organization_roundtrips_created_entity()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateConfiguredClient();

        var created = await (await client.PostAsJsonAsync("/api/organizations", NewOrg()))
            .Content.ReadFromJsonAsync<OrganizationDto>();

        var fetched = await client.GetFromJsonAsync<OrganizationDto>($"/api/organizations/{created!.Id}");

        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(created.Name, fetched.Name);
    }

    [Fact]
    public async Task Get_unknown_organization_returns_404()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateConfiguredClient();

        var response = await client.GetAsync($"/api/organizations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_invalid_email_returns_400()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateConfiguredClient();

        var response = await client.PostAsJsonAsync(
            "/api/organizations",
            new CreateOrganizationRequest("Bad Corp", "not-an-email", "Someone"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Adding_member_beyond_free_seats_returns_400()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateConfiguredClient();

        var org = await (await client.PostAsJsonAsync("/api/organizations", NewOrg()))
            .Content.ReadFromJsonAsync<OrganizationDto>();

        // Free plan = 3 seats; owner holds 1, so two more succeed.
        await AddMember(client, org!.Id, "a@acme.test");
        await AddMember(client, org.Id, "b@acme.test");

        var overflow = await AddMember(client, org.Id, "c@acme.test");
        Assert.Equal(HttpStatusCode.BadRequest, overflow.StatusCode);
    }

    [Fact]
    public async Task Upgrading_plan_raises_seats_and_allows_more_members()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateConfiguredClient();

        var org = await (await client.PostAsJsonAsync("/api/organizations", NewOrg()))
            .Content.ReadFromJsonAsync<OrganizationDto>();

        var planResponse = await client.PutAsJsonAsync(
            $"/api/organizations/{org!.Id}/subscription/plan",
            new { OrganizationId = org.Id, Plan = SubscriptionPlan.Pro });

        Assert.Equal(HttpStatusCode.OK, planResponse.StatusCode);
        var sub = await planResponse.Content.ReadFromJsonAsync<SubscriptionDto>();
        Assert.Equal(SubscriptionPlan.Pro, sub!.Plan);
        Assert.Equal(50, sub.Seats);

        // Now well under the Pro seat limit.
        var added = await AddMember(client, org.Id, "d@acme.test");
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
    }

    private static Task<HttpResponseMessage> AddMember(HttpClient client, Guid orgId, string email) =>
        client.PostAsJsonAsync(
            $"/api/organizations/{orgId}/members",
            new { OrganizationId = orgId, Email = email, FullName = "Test Member", Role = UserRole.Member });
}
