using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NotificationAuditService.Expenses.Contracts;
using NotificationAuditService.Tests.TestInfrastructure;
using Xunit;

namespace NotificationAuditService.Tests.Expenses;

/// <summary>
/// End-to-end tests for the Expense-splitting feature: split correctness, balance calculation,
/// validation, and — as everywhere in this service — multi-tenant isolation and authorisation.
/// </summary>
public class ExpensesIntegrationTests : IClassFixture<ProjectApiFactory>
{
    private const string OrgA = "org-alpha";
    private const string OrgB = "org-beta";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ProjectApiFactory _factory;

    public ExpensesIntegrationTests(ProjectApiFactory factory) => _factory = factory;

    // ----- Authorisation / isolation -----

    [Fact]
    public async Task Endpoints_WithoutOrganizationHeader_Return401()
    {
        var anonymous = _factory.CreateClient();

        var post = await anonymous.PostAsJsonAsync("/api/expenses", new
        {
            groupId = "g1",
            amount = 30m,
            currency = "USD",
            paidByUserId = "alice",
            splitType = "Equal",
            participants = new[] { new { userId = "alice" } }
        });
        var getBalances = await anonymous.GetAsync("/api/expenses/group/g1/balances");

        Assert.Equal(HttpStatusCode.Unauthorized, post.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, getBalances.StatusCode);
    }

    [Fact]
    public async Task GetExpense_WhenItBelongsToAnotherOrg_Returns404()
    {
        var group = NewGroup();
        var id = await CreateEqualExpenseAsync(ClientFor(OrgA), group, 30m, "USD", "alice", "alice", "bob");

        var crossTenant = await ClientFor(OrgB).GetAsync($"/api/expenses/{id}");

        Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);
    }

    [Fact]
    public async Task GetExpensesByGroup_DoesNotReturnAnotherOrgsExpenses()
    {
        var group = NewGroup();
        await CreateEqualExpenseAsync(ClientFor(OrgA), group, 30m, "USD", "alice", "alice", "bob");

        var response = await ClientFor(OrgB).GetAsync($"/api/expenses/group/{group}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<ExpenseResponse>>(JsonOptions);
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    // ----- Split correctness -----

    [Fact]
    public async Task CreateExpense_EqualSplit_DistributesRemainderAndSumsToTotal()
    {
        var client = ClientFor(OrgA);
        var response = await client.PostAsJsonAsync("/api/expenses", new
        {
            groupId = NewGroup(),
            amount = 100m,
            currency = "USD",
            paidByUserId = "alice",
            splitType = "Equal",
            participants = new[] { new { userId = "alice" }, new { userId = "bob" }, new { userId = "carol" } }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ExpenseResponse>(JsonOptions);
        var shares = created!.Participants.Select(p => p.ShareAmount).OrderByDescending(x => x).ToList();

        Assert.Equal(3, shares.Count);
        Assert.Equal(100m, shares.Sum());                 // reconciles exactly to the total
        Assert.Equal(new[] { 33.34m, 33.33m, 33.33m }, shares); // remainder cent goes to the first
    }

    [Fact]
    public async Task CreateExpense_ExactSplit_NotSummingToTotal_Returns400()
    {
        var client = ClientFor(OrgA);
        var response = await client.PostAsJsonAsync("/api/expenses", new
        {
            groupId = NewGroup(),
            amount = 100m,
            currency = "USD",
            paidByUserId = "alice",
            splitType = "Exact",
            participants = new[]
            {
                new { userId = "alice", shareAmount = 50m },
                new { userId = "bob", shareAmount = 40m }   // sums to 90, not 100
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateExpense_WithNoParticipants_Returns400()
    {
        var client = ClientFor(OrgA);
        var response = await client.PostAsJsonAsync("/api/expenses", new
        {
            groupId = NewGroup(),
            amount = 10m,
            currency = "USD",
            paidByUserId = "alice",
            splitType = "Equal",
            participants = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ----- Balance calculation -----

    [Fact]
    public async Task GetGroupBalances_ComputesNetBalancesAndSettlements()
    {
        // Alice pays 90, split equally among alice, bob, carol (30 each).
        // Net: alice +60, bob -30, carol -30  =>  bob->alice 30, carol->alice 30.
        var client = ClientFor(OrgA);
        var group = NewGroup();
        await CreateEqualExpenseAsync(client, group, 90m, "USD", "alice", "alice", "bob", "carol");

        var response = await client.GetAsync($"/api/expenses/group/{group}/balances");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var balances = await response.Content.ReadFromJsonAsync<GroupBalanceResponse>(JsonOptions);

        Assert.Equal("USD", balances!.Currency);
        Assert.Equal(60m, NetOf(balances, "alice"));
        Assert.Equal(-30m, NetOf(balances, "bob"));
        Assert.Equal(-30m, NetOf(balances, "carol"));

        Assert.Equal(2, balances.Settlements.Count);
        Assert.Equal(30m, SettlementAmount(balances, "bob", "alice"));
        Assert.Equal(30m, SettlementAmount(balances, "carol", "alice"));
    }

    [Fact]
    public async Task GetGroupBalances_WithMixedCurrencies_Returns400()
    {
        var client = ClientFor(OrgA);
        var group = NewGroup();
        await CreateEqualExpenseAsync(client, group, 30m, "USD", "alice", "alice", "bob");
        await CreateEqualExpenseAsync(client, group, 30m, "EUR", "alice", "alice", "bob");

        var response = await client.GetAsync($"/api/expenses/group/{group}/balances");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetGroupBalances_AcrossMultipleExpenses_NetsAndDropsSettledUsers()
    {
        // Two expenses in one group, different payers, all split equally among alice, bob, carol:
        //   e1: alice pays 60  -> each owes 20  => alice +40, bob -20, carol -20
        //   e2: bob   pays 30  -> each owes 10  => bob  +20, alice -10, carol -10
        // Combined net: alice +30, bob 0, carol -30  =>  single settlement carol->alice 30.
        var client = ClientFor(OrgA);
        var group = NewGroup();
        await CreateEqualExpenseAsync(client, group, 60m, "USD", "alice", "alice", "bob", "carol");
        await CreateEqualExpenseAsync(client, group, 30m, "USD", "bob", "alice", "bob", "carol");

        var response = await client.GetAsync($"/api/expenses/group/{group}/balances");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var balances = await response.Content.ReadFromJsonAsync<GroupBalanceResponse>(JsonOptions);

        // Net balances
        Assert.Equal("USD", balances!.Currency);
        Assert.Equal(30m, NetOf(balances, "alice"));
        Assert.Equal(0m, NetOf(balances, "bob"));
        Assert.Equal(-30m, NetOf(balances, "carol"));

        // A zero-net user must not appear in any settlement; exactly one transfer clears the group.
        Assert.Single(balances.Settlements);
        Assert.Equal(30m, SettlementAmount(balances, "carol", "alice"));
        Assert.DoesNotContain(balances.Settlements, s => s.FromUserId == "bob" || s.ToUserId == "bob");
    }

    // ----- Helpers -----

    private HttpClient ClientFor(string organizationId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ProjectApiFactory.OrganizationHeader, organizationId);
        return client;
    }

    private static string NewGroup() => $"group-{Guid.NewGuid():N}";

    private static async Task<int> CreateEqualExpenseAsync(
        HttpClient client, string groupId, decimal amount, string currency, string paidBy, params string[] participantUserIds)
    {
        var response = await client.PostAsJsonAsync("/api/expenses", new
        {
            groupId,
            amount,
            currency,
            paidByUserId = paidBy,
            splitType = "Equal",
            participants = participantUserIds.Select(u => new { userId = u }).ToArray()
        });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<ExpenseResponse>(JsonOptions);
        return created!.Id;
    }

    private static decimal NetOf(GroupBalanceResponse balances, string userId) =>
        balances.Balances.Single(b => b.UserId == userId).NetAmount;

    private static decimal SettlementAmount(GroupBalanceResponse balances, string from, string to) =>
        balances.Settlements.Single(s => s.FromUserId == from && s.ToUserId == to).Amount;
}
