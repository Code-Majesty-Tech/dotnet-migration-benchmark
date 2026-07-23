namespace SaasPlatform.Application.Common;

/// <summary>Requested resource does not exist. Maps to HTTP 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>Request conflicts with current state, e.g. a duplicate slug. Maps to HTTP 409.</summary>
public sealed class ConflictException(string message) : Exception(message);
