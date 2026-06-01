namespace Fcg.Audit.Logs.Application.Features.AuditLogRequested;

public sealed class InvalidAuditLogEventException(string message) : Exception(message);
