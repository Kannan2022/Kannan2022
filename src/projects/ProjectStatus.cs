namespace NotificationAuditService.Projects;

/// <summary>
/// The lifecycle states a <see cref="Project"/> can be in. Persisted as its string name so the
/// stored value is stable across enum reordering and human-readable in the database.
/// </summary>
public enum ProjectStatus
{
    /// <summary>Created but not yet started.</summary>
    NotStarted,

    /// <summary>Actively being worked on.</summary>
    InProgress,

    /// <summary>Temporarily paused.</summary>
    OnHold,

    /// <summary>Finished successfully.</summary>
    Completed,

    /// <summary>Abandoned before completion.</summary>
    Cancelled
}
