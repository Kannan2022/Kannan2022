using Microsoft.AspNetCore.Mvc;
using NotificationAuditService.Common;
using NotificationAuditService.Projects.Contracts;

namespace NotificationAuditService.Projects;

/// <summary>
/// REST endpoints for managing projects. Every endpoint operates strictly within the caller's
/// organisation: the tenant is taken from the authenticated principal via <see cref="ITenantContext"/>,
/// so a caller can never read or mutate another organisation's projects.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        IProjectService projectService,
        ITenantContext tenantContext,
        ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>Creates a project for a team within the caller's organisation.</summary>
    /// <param name="request">The project to create.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The created project.</returns>
    /// <response code="201">The project was created.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="401">The caller has no resolvable organisation.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProjectResponse>> CreateProject(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.OrganizationId is not { } organizationId)
            return UnauthorizedNoOrganisation();

        try
        {
            var project = await _projectService.CreateProjectAsync(
                organizationId, request.Name, request.TeamId, request.Description, cancellationToken);

            var response = ProjectResponse.FromEntity(project);
            return CreatedAtAction(nameof(GetProjectsByTeam), new { teamId = project.TeamId }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid create-project request for organisation {OrganizationId}", organizationId);
            return BadRequest(new ProblemDetails { Title = "Invalid request", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating project for organisation {OrganizationId}", organizationId);
            return Problem("An internal error occurred.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>Updates the status of a project owned by the caller's organisation.</summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="request">The new status.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The updated project.</returns>
    /// <response code="200">The status was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="401">The caller has no resolvable organisation.</response>
    /// <response code="404">No such project exists within the caller's organisation.</response>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> UpdateProjectStatus(
        int id,
        [FromBody] UpdateProjectStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.OrganizationId is not { } organizationId)
            return UnauthorizedNoOrganisation();

        try
        {
            var project = await _projectService.UpdateProjectStatusAsync(
                organizationId, id, request.Status, cancellationToken);

            if (project is null)
                return NotFound(new ProblemDetails { Title = "Not found", Detail = $"Project {id} was not found." });

            return Ok(ProjectResponse.FromEntity(project));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid status update for project {ProjectId}, organisation {OrganizationId}", id, organizationId);
            return BadRequest(new ProblemDetails { Title = "Invalid request", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating project {ProjectId} for organisation {OrganizationId}", id, organizationId);
            return Problem("An internal error occurred.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>Lists projects for a team within the caller's organisation, newest first.</summary>
    /// <param name="teamId">The team whose projects to list.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="skip">Number of records to skip (paging).</param>
    /// <param name="take">Page size (1–100).</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The matching projects.</returns>
    /// <response code="200">The projects were retrieved.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="401">The caller has no resolvable organisation.</response>
    [HttpGet("team/{teamId}")]
    [ProducesResponseType(typeof(IEnumerable<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ProjectResponse>>> GetProjectsByTeam(
        string teamId,
        [FromQuery] ProjectStatus? status,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (_tenantContext.OrganizationId is not { } organizationId)
            return UnauthorizedNoOrganisation();

        try
        {
            var projects = await _projectService.GetProjectsByTeamAsync(
                organizationId, teamId, status, skip, take, cancellationToken);

            return Ok(projects.Select(ProjectResponse.FromEntity));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid list request for team {TeamId}, organisation {OrganizationId}", teamId, organizationId);
            return BadRequest(new ProblemDetails { Title = "Invalid request", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error listing projects for organisation {OrganizationId}", organizationId);
            return Problem("An internal error occurred.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>Deletes a project owned by the caller's organisation.</summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <response code="204">The project was deleted.</response>
    /// <response code="401">The caller has no resolvable organisation.</response>
    /// <response code="404">No such project exists within the caller's organisation.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject(int id, CancellationToken cancellationToken)
    {
        if (_tenantContext.OrganizationId is not { } organizationId)
            return UnauthorizedNoOrganisation();

        try
        {
            var deleted = await _projectService.DeleteProjectAsync(organizationId, id, cancellationToken);
            if (!deleted)
                return NotFound(new ProblemDetails { Title = "Not found", Detail = $"Project {id} was not found." });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting project {ProjectId} for organisation {OrganizationId}", id, organizationId);
            return Problem("An internal error occurred.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>Returns a 401 when the request carries no resolvable organisation.</summary>
    private ObjectResult UnauthorizedNoOrganisation()
    {
        _logger.LogWarning("Rejected project request: no organisation associated with the caller.");
        return Problem(
            "The request is not associated with an organisation.",
            statusCode: StatusCodes.Status401Unauthorized);
    }
}
