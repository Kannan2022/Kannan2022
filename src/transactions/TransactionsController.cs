using Microsoft.AspNetCore.Mvc;
using NotificationAuditService.Common;
using NotificationAuditService.Transactions.Contracts;

namespace NotificationAuditService.Transactions;

/// <summary>
/// REST endpoints for recording and querying financial transactions. Every endpoint operates
/// strictly within the caller's organisation: the tenant is taken from the authenticated principal
/// via <see cref="ITenantContext"/>, so a caller can never read or write another organisation's ledger.
/// The ledger is append-only — there is no update or delete endpoint.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionService transactionService,
        ITenantContext tenantContext,
        ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>Records a transaction for a user within the caller's organisation.</summary>
    /// <param name="request">The transaction to record.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The created transaction.</returns>
    /// <response code="201">The transaction was recorded.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="401">The caller has no resolvable organisation.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TransactionResponse>> CreateTransaction(
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.OrganizationId is not { } organizationId)
            return UnauthorizedNoOrganisation();

        try
        {
            var transaction = await _transactionService.CreateTransactionAsync(
                organizationId, request.UserId, request.Amount, request.Currency, request.Description, cancellationToken);

            var response = TransactionResponse.FromEntity(transaction);
            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, response);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(ex, "Invalid transaction amount for organisation {OrganizationId}", organizationId);
            return BadRequest(new ProblemDetails { Title = "Invalid request", Detail = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid create-transaction request for organisation {OrganizationId}", organizationId);
            return BadRequest(new ProblemDetails { Title = "Invalid request", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error recording transaction for organisation {OrganizationId}", organizationId);
            return Problem("An internal error occurred.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>Gets a single transaction by id, within the caller's organisation.</summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The transaction.</returns>
    /// <response code="200">The transaction was found.</response>
    /// <response code="401">The caller has no resolvable organisation.</response>
    /// <response code="404">No such transaction exists within the caller's organisation.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetTransaction(int id, CancellationToken cancellationToken)
    {
        if (_tenantContext.OrganizationId is not { } organizationId)
            return UnauthorizedNoOrganisation();

        var transaction = await _transactionService.GetTransactionByIdAsync(organizationId, id, cancellationToken);
        if (transaction is null)
            return NotFound(new ProblemDetails { Title = "Not found", Detail = $"Transaction {id} was not found." });

        return Ok(TransactionResponse.FromEntity(transaction));
    }

    /// <summary>Lists transactions for a user within the caller's organisation, newest first.</summary>
    /// <param name="userId">The user whose transactions to list.</param>
    /// <param name="skip">Number of records to skip (paging).</param>
    /// <param name="take">Page size (1–100).</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The matching transactions.</returns>
    /// <response code="200">The transactions were retrieved.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="401">The caller has no resolvable organisation.</response>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(IEnumerable<TransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetTransactionsByUser(
        string userId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (_tenantContext.OrganizationId is not { } organizationId)
            return UnauthorizedNoOrganisation();

        try
        {
            var transactions = await _transactionService.GetTransactionsByUserAsync(
                organizationId, userId, skip, take, cancellationToken);

            return Ok(transactions.Select(TransactionResponse.FromEntity));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid list request for user {UserId}, organisation {OrganizationId}", userId, organizationId);
            return BadRequest(new ProblemDetails { Title = "Invalid request", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error listing transactions for organisation {OrganizationId}", organizationId);
            return Problem("An internal error occurred.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>Returns a 401 when the request carries no resolvable organisation.</summary>
    private ObjectResult UnauthorizedNoOrganisation()
    {
        _logger.LogWarning("Rejected transaction request: no organisation associated with the caller.");
        return Problem(
            "The request is not associated with an organisation.",
            statusCode: StatusCodes.Status401Unauthorized);
    }
}
