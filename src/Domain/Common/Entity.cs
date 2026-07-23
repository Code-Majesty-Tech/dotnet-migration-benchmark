namespace SaasPlatform.Domain.Common;

/// <summary>
/// Base type for aggregate roots and entities. Provides identity and audit stamps.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; protected set; }

    protected void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
