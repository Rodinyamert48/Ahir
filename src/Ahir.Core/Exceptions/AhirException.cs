using Ahir.Core.Constants;

namespace Ahir.Core.Exceptions;

public class AhirException : Exception
{
    public string ErrorCode { get; }

    public AhirException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public AhirException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public static AhirException NotFound(string resource) =>
        new(ErrorCodes.NotFound, $"Resource '{resource}' not found.");

    public static AhirException AlreadyExists(string resource) =>
        new(ErrorCodes.AlreadyExists, $"Resource '{resource}' already exists.");

    public static AhirException InvalidInput(string detail) =>
        new(ErrorCodes.InvalidInput, $"Invalid input: {detail}");

    public static AhirException Unauthorized(string detail = "") =>
        new(ErrorCodes.Unauthorized, string.IsNullOrEmpty(detail) ? "Authentication required." : detail);

    public static AhirException Forbidden(string detail = "") =>
        new(ErrorCodes.Forbidden, string.IsNullOrEmpty(detail) ? "Insufficient permissions." : detail);

    public static AhirException DatabaseNotFound(string name) =>
        new(ErrorCodes.DatabaseNotFound, $"Database '{name}' not found.");

    public static AhirException CollectionNotFound(string name) =>
        new(ErrorCodes.CollectionNotFound, $"Collection '{name}' not found.");

    public static AhirException RecordNotFound(string id) =>
        new(ErrorCodes.RecordNotFound, $"Record '{id}' not found.");

    public static AhirException DatabaseCorrupted(string name) =>
        new(ErrorCodes.DatabaseCorrupted, $"Database '{name}' is corrupted and cannot be opened.");
}