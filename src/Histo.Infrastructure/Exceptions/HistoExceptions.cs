namespace Histo.Infrastructure.Exceptions;

/// <summary>
/// Base type for all Histo domain-layer exceptions. Lets callers write a single
/// <c>catch (HistoException)</c> boundary to distinguish known/handled domain
/// failures from unexpected framework or runtime exceptions.
/// </summary>
public abstract class HistoException : Exception
{
    protected HistoException(string message) : base(message) { }
    protected HistoException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>Raised when a database or stored-procedure operation fails unexpectedly.</summary>
public class HistoDataAccessException : HistoException
{
    public string Operation { get; }

    public HistoDataAccessException(string message, string operation, Exception innerException)
        : base(message, innerException) => Operation = operation;
}

/// <summary>Raised when user-supplied input fails a domain validation or business rule.</summary>
public class HistoValidationException : HistoException
{
    public string? FieldName { get; }

    public HistoValidationException(string message, string? fieldName = null) : base(message) => FieldName = fieldName;
}

/// <summary>Raised when an external service or integration call fails.</summary>
public class HistoIntegrationException : HistoException
{
    public string ServiceName { get; }

    public HistoIntegrationException(string message, string serviceName, Exception innerException)
        : base(message, innerException) => ServiceName = serviceName;
}

/// <summary>Raised when a requested resource does not exist.</summary>
public class HistoNotFoundException : HistoException
{
    public HistoNotFoundException(string resourceType, string resourceId)
        : base($"{resourceType} '{resourceId}' was not found.") { }
}

/// <summary>
/// Base for optimistic-concurrency conflicts (rowstamp mismatch) — the recurring
/// failure mode across Batch/Animal/QCNote/BlockTest updates in this domain.
/// </summary>
public abstract class HistoConcurrencyException : HistoException
{
    protected HistoConcurrencyException(string message) : base(message) { }
}
