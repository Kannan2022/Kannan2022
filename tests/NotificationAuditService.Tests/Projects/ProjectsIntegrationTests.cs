using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NotificationAuditService.Projects;
using NotificationAuditService.Projects.Contracts;
using NotificationAuditService.Tests.TestInfrastructure;
using Xunit;

namespace NotificationAuditService.Tests.Projects;

/// <summary>
/// End-to-end tests for the Projects API. The emphasis is multi-tenant isolation: a caller from one
/// organisation must never be able to read, modify, or delete another organisation's projects.
/// </summary>
public class ProjectsIntegrationTests : IClassFixture<ProjectApiFactory>
{
    private const string OrgA = "org-alpha";
    private const string OrgB = "org-beta";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ProjectApiFactory _factory;

    public ProjectsIntegrationTests(ProjectApiFactory factory) => _factory = factory;

    // ----- Isolation -----

    [Fact]
    public async Task GetProjectsByTeam_WhenProjectBelongsToAnotherOrg_DoesNotReturnIt()
    {
        // Arrange
        var team = NewTeam();
        var clientA = ClientFor(OrgA);
        var clientB = ClientFor(OrgB);
        await CreateProjectAsync(clientA, team, "Alpha's project");

        // Act — Org B lists the same team id
        var response = await clientB.GetAsync($"/api/projects/team/{team}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var projects = await response.Content.ReadFromJsonAsync<List<ProjectResponse>>(JsonOptions);
        Assert.NotNull(projects);
        Assert.Empty(projects);
    }

    [Fact]
    public async Task UpdateProjectStatus_WhenProjectBelongsToAnotherOrg_Returns404_AndLeavesItUnchanged()
    {
        // Arrange
        var team = NewTeam();
        var clientA = ClientFor(OrgA);
        var clientB = ClientFor(OrgB);
        var id = await CreateProjectAsync(clientA, team, "Alpha's project");

        // Act — Org B tries to change Org A's project
        var crossTenant = await clientB.PutAsJsonAsync(
            $"/api/projects/{id}/status", new { status = "Completed" });

        // Assert — hidden from Org B (404), and untouched for Org A
        Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);

        var stillOwned = await GetProjectAsync(clientA, team, id);
        Assert.NotNull(stillOwned);
        Assert.Equal(ProjectStatus.NotStarted, stillOwned!.Status);
    }

    [Fact]
    public async Task DeleteProject_WhenProjectBelongsToAnotherOrg_Returns404_AndDoesNotDelete()
    {
        // Arrange
        var team = NewTeam();
        var clientA = ClientFor(OrgA);
        var clientB = ClientFor(OrgB);
        var id = await CreateProjectAsync(clientA, team, "Alpha's project");

        // Act — Org B tries to delete Org A's project
        var crossTenant = await clientB.DeleteAsync($"/api/projects/{id}");

        // Assert — refused, and the project still exists for Org A
        Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);
        Assert.NotNull(await GetProjectAsync(clientA, team, id));
    }

    // ----- Fail-closed (no tenant) -----

    [Fact]
    public async Task Endpoints_WithoutOrganizationHeader_Return401()
    {
        var anonymous = _factory.CreateClient();

        var get = await anonymous.GetAsync("/api/projects/team/any");
        var post = await anonymous.PostAsJsonAsync("/api/projects", new { name = "X", teamId = "t" });
        var delete = await anonymous.DeleteAsync("/api/projects/1");

        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, post.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
    }

    // ----- Happy paths -----

    [Fact]
    public async Task CreateProject_WithValidRequest_ReturnsCreatedProjectInNotStartedStatus()
    {
        var client = ClientFor(OrgA);
        var team = NewTeam();

        var response = await client.PostAsJsonAsync("/api/projects",
            new { name = "Launch", teamId = team, description = "Q3 launch" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(OrgA, created!.OrganizationId);
        Assert.Equal("Launch", created.Name);
        Assert.Equal(team, created.TeamId);
        Assert.Equal(ProjectStatus.NotStarted, created.Status);
    }

    [Fact]
    public async Task UpdateProjectStatus_ForOwnProject_Returns200_AndPersistsNewStatus()
    {
        var client = ClientFor(OrgA);
        var team = NewTeam();
        var id = await CreateProjectAsync(client, team, "Own project");

        var response = await client.PutAsJsonAsync(
            $"/api/projects/{id}/status", new { status = "InProgress" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);
        Assert.Equal(ProjectStatus.InProgress, updated!.Status);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task DeleteProject_ForOwnProject_Returns204_AndRemovesIt()
    {
        var client = ClientFor(OrgA);
        var team = NewTeam();
        var id = await CreateProjectAsync(client, team, "Doomed project");

        var delete = await client.DeleteAsync($"/api/projects/{id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Null(await GetProjectAsync(client, team, id));
    }

    [Fact]
    public async Task GetProjectsByTeam_WithStatusFilter_ReturnsOnlyMatchingProjects()
    {
        var client = ClientFor(OrgA);
        var team = NewTeam();
        var inProgressId = await CreateProjectAsync(client, team, "Active");
        await CreateProjectAsync(client, team, "Idle"); // stays NotStarted
        await client.PutAsJsonAsync($"/api/projects/{inProgressId}/status", new { status = "InProgress" });

        var response = await client.GetAsync($"/api/projects/team/{team}?status=InProgress");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var projects = await response.Content.ReadFromJsonAsync<List<ProjectResponse>>(JsonOptions);
        Assert.Single(projects!);
        Assert.Equal(inProgressId, projects![0].Id);
    }

    // ----- Validation -----

    [Theory]
    [InlineData("{\"teamId\":\"t1\"}")]                 // missing name
    [InlineData("{\"name\":\"\",\"teamId\":\"t1\"}")]   // empty name
    [InlineData("{\"name\":\"Valid\"}")]                // missing teamId
    public async Task CreateProject_WithInvalidRequest_Returns400(string json)
    {
        var client = ClientFor(OrgA);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/projects", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ----- Helpers -----

    private HttpClient ClientFor(string organizationId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ProjectApiFactory.OrganizationHeader, organizationId);
        return client;
    }

    private static string NewTeam() => $"team-{Guid.NewGuid():N}";

    private static async Task<int> CreateProjectAsync(HttpClient client, string teamId, string name)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new { name, teamId });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);
        return created!.Id;
    }

    private static async Task<ProjectResponse?> GetProjectAsync(HttpClient client, string teamId, int id)
    {
        var response = await client.GetAsync($"/api/projects/team/{teamId}");
        response.EnsureSuccessStatusCode();
        var projects = await response.Content.ReadFromJsonAsync<List<ProjectResponse>>(JsonOptions);
        return projects?.FirstOrDefault(p => p.Id == id);
    }
}
