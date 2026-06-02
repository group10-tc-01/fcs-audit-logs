namespace Fcg.Audit.Logs.Application.Features.AuditLogRequested.Exceptions;

public sealed class InvalidAuditLogEventException(string message) : Exception(message);
