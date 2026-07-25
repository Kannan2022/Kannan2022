using Microsoft.AspNetCore.Mvc;
using NotificationAuditService.Common;
using NotificationAuditService.Expenses.Contracts;

namespace NotificationAuditService.Expenses;

/// <summary>
/// REST endpoints for shared expenses and group balances. Every endpoint operates strictly within
/// the caller's organisation: the tenant is taken from the authenticated principal via
/// <see cref="ITenantContext"/>, so a caller can never read or write another organisation's expenses.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly ISharedExpenseService _expenseService;
    private readonly IBalanceCalculationService _balanceService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(
        ISharedExpenseService expenseService,
        IBalanceCalculationService balanceService,
        ITenantContext tenantContext,
        ILogger<ExpensesController> logger)
    {
        _expenseService = expenseService;
        _balanceService = balanceService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>Creates a shared expense within the caller's organisation.</summary>
    /// <response code="201">The expense was created.</response>
    /// <response code="400">The request failed validation (e.g. exact shares do not sum to the total).</response>
    /// <response code="401">The caller has no resolvable organisation.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ExpenseResponse>> CreateExpense(
        [FromBody] CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.OrganizationId is not { } organizationId)
            return UnauthorizedNoOrganisation();

        try
        {
            var participants = request.Participants
                .Select(p => (p.UserId, p.ShareAmount))
                .ToList();

            var expense = await _expenseService.CreateExpenseAsync(
                organizationId, request.GroupId, request.Description, request.Amount,
                request.Currency, request.PaidByUserId, request.SplitType, participants, cancellationToken);

            var response = ExpenseResponse.FromEntity(expense);
            return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid create-expense request for organisation {OrganizationId}", organizationId);
            return BadRequest(new ProblemDetails { Title = "Invalid request", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating expense for organisation {OrganizationId}", organizationId);
            return Problem("An internal error occurred.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>Gets a single expense by id, within the caller's organisation.</summary>
    /// <response code="200">The expense was found.</response>
    /// <response code="401">The caller has no resolvable organisation.</response>
    /// <response code="404">No such expense exists within the caller's organisation.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpenseResponse>> GetExpense(int id, CancellationToken cancellationToken)
    {
        if (_tenantContext.OrganizationId is not { } organizationId)
            return UnauthorizedNoOrganisation();

        var expense = await _expenseService.GetExpenseByIdAsync(organizationId, id, cancellationToken);
        if (expense is null)
            return NotFound(new ProblemDetails { Title = "Not found", Detail = $"Expense {id} was not found." });

        return Ok(ExpenseResponse.FromEntity(expense));
    }

    /// <summary>Lists expenses for a group within the caller's organisation, newest first.</summary>
    /// <response code="200">The expenses were retrieved.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="401">The caller has no resolvable organisation.</response>
    [HttpGet("group/{groupId}")]
    [ProducesResponseType(typeof(IEnumerable<ExpenseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ExpenseResponse>>> GetExpensesByGroup(
        string groupId,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.OrganizationId is not { } organizationId)
            return UnauthorizedNoOrganisation();

        try
        {
            var expenses = await _expenseService.GetExpensesByGroupAsync(organizationId, groupId, cancellationToken);
            return Ok(expenses.Select(ExpenseResponse.FromEntity));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid list request for group {GroupId}, organisation {OrganizationId}", groupId, organizationId);
            return BadRequest(new ProblemDetails { Title = "Invalid request", Detail = ex.Message });
        }
    }

    /// <summary>Calculates net balances and suggested settlements for a group.</summary>
    /// <response code="200">The balances were calculated.</response>
    /// <response code="400">The group's expenses span more than one currency.</response>
    /// <response code="401">The caller has no resolvable organisation.</response>
    [HttpGet("group/{groupId}/balances")]
    [ProducesResponseType(typeof(GroupBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GroupBalanceResponse>> GetGroupBalances(
        string groupId,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.OrganizationId is not { } organizationId)
            return UnauthorizedNoOrganisation();

        try
        {
            var balances = await _balanceService.CalculateGroupBalancesAsync(organizationId, groupId, cancellationToken);

            var response = new GroupBalanceResponse
            {
                GroupId = balances.GroupId,
                Currency = balances.Currency,
                Balances = balances.Balances
                    .Select(b => new UserBalance { UserId = b.UserId, NetAmount = b.NetAmount })
                    .ToList(),
                Settlements = balances.Settlements
                    .Select(s => new Settlement { FromUserId = s.FromUserId, ToUserId = s.ToUserId, Amount = s.Amount })
                    .ToList()
            };
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid balance request for group {GroupId}, organisation {OrganizationId}", groupId, organizationId);
            return BadRequest(new ProblemDetails { Title = "Invalid request", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot calculate balances for group {GroupId}, organisation {OrganizationId}", groupId, organizationId);
            return BadRequest(new ProblemDetails { Title = "Cannot calculate balances", Detail = ex.Message });
        }
    }

    private ObjectResult UnauthorizedNoOrganisation()
    {
        _logger.LogWarning("Rejected expense request: no organisation associated with the caller.");
        return Problem(
            "The request is not associated with an organisation.",
            statusCode: StatusCodes.Status401Unauthorized);
    }
}
