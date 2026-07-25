using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NotificationAuditService.Transactions.Contracts;
using NotificationAuditService.Tests.TestInfrastructure;
using Xunit;

namespace NotificationAuditService.Tests.Transactions;

/// <summary>
/// End-to-end tests for the Transactions API. The emphasis is multi-tenant isolation and
/// authorisation: a caller from one organisation must never read another organisation's ledger,
/// and requests without a resolvable organisation must be rejected.
/// </summary>
public class TransactionsIntegrationTests : IClassFixture<ProjectApiFactory>
{
    private const string OrgA = "org-alpha";
    private const string OrgB = "org-beta";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ProjectApiFactory _factory;

    public TransactionsIntegrationTests(ProjectApiFactory factory) => _factory = factory;

    // ----- Authorisation / isolation -----

    [Fact]
    public async Task Endpoints_WithoutOrganizationHeader_Return401()
    {
        var anonymous = _factory.CreateClient();

        var post = await anonymous.PostAsJsonAsync("/api/transactions", new { userId = "u1", amount = 10m, currency = "USD" });
        var getById = await anonymous.GetAsync("/api/transactions/1");
        var getByUser = await anonymous.GetAsync("/api/transactions/user/u1");

        Assert.Equal(HttpStatusCode.Unauthorized, post.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, getById.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, getByUser.StatusCode);
    }

    [Fact]
    public async Task GetTransaction_WhenItBelongsToAnotherOrg_Returns404()
    {
        var user = NewUser();
        var id = await CreateTransactionAsync(ClientFor(OrgA), user, 100m, "USD");

        var crossTenant = await ClientFor(OrgB).GetAsync($"/api/transactions/{id}");

        Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);
    }

    [Fact]
    public async Task GetTransactionsByUser_DoesNotReturnAnotherOrgsTransactions()
    {
        var user = NewUser();
        await CreateTransactionAsync(ClientFor(OrgA), user, 100m, "USD");

        var response = await ClientFor(OrgB).GetAsync($"/api/transactions/user/{user}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>(JsonOptions);
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    // ----- Happy paths -----

    [Fact]
    public async Task CreateTransaction_WithValidRequest_ReturnsCreatedTransaction()
    {
        var user = NewUser();

        var response = await ClientFor(OrgA).PostAsJsonAsync("/api/transactions",
            new { userId = user, amount = -42.50m, currency = "usd", description = "Refund" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(OrgA, created!.OrganizationId);
        Assert.Equal(user, created.UserId);
        Assert.Equal(-42.50m, created.Amount);
        Assert.Equal("USD", created.Currency); // normalised to upper-case
    }

    [Fact]
    public async Task GetTransactionsByUser_ReturnsOnlyThatUsersTransactionsInOrg()
    {
        var client = ClientFor(OrgA);
        var userX = NewUser();
        var userY = NewUser();
        await CreateTransactionAsync(client, userX, 10m, "USD");
        await CreateTransactionAsync(client, userX, 20m, "USD");
        await CreateTransactionAsync(client, userY, 99m, "USD");

        var response = await client.GetAsync($"/api/transactions/user/{userX}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>(JsonOptions);
        Assert.Equal(2, list!.Count);
        Assert.All(list, t => Assert.Equal(userX, t.UserId));
    }

    // ----- Validation -----

    [Fact]
    public async Task CreateTransaction_WithZeroAmount_Returns400()
    {
        var response = await ClientFor(OrgA).PostAsJsonAsync("/api/transactions",
            new { userId = NewUser(), amount = 0m, currency = "USD" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("US")]     // too short
    [InlineData("DOLLAR")] // too long
    [InlineData("12")]     // not letters
    public async Task CreateTransaction_WithInvalidCurrency_Returns400(string currency)
    {
        var response = await ClientFor(OrgA).PostAsJsonAsync("/api/transactions",
            new { userId = NewUser(), amount = 10m, currency });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ----- Helpers -----

    private HttpClient ClientFor(string organizationId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ProjectApiFactory.OrganizationHeader, organizationId);
        return client;
    }

    private static string NewUser() => $"user-{Guid.NewGuid():N}";

    private static async Task<int> CreateTransactionAsync(HttpClient client, string userId, decimal amount, string currency)
    {
        var response = await client.PostAsJsonAsync("/api/transactions", new { userId, amount, currency });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);
        return created!.Id;
    }
}
