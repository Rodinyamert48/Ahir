using System.Runtime.CompilerServices;
using Ahir.Core.Constants;

namespace Ahir.Core.Utilities;

public static class Guard
{
    public static void NotNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
    }

    public static void NotNullOrEmpty([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Value cannot be null or empty.", paramName);
    }

    public static void NotNullOrWhiteSpace([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);
    }

    public static void InRange(int value, int min, int max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(paramName, $"Value must be between {min} and {max}.");
    }

    public static void InRange(long value, long min, long max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(paramName, $"Value must be between {min} and {max}.");
    }

    public static void Positive(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(paramName, "Value must be positive.");
    }

    public static void Positive(long value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(paramName, "Value must be positive.");
    }

    public static void ValidPort(int port, [CallerArgumentExpression(nameof(port))] string? paramName = null)
    {
        if (port < AhirConstants.MinPort || port > AhirConstants.MaxPort)
            throw new ArgumentOutOfRangeException(paramName, $"Port must be between {AhirConstants.MinPort} and {AhirConstants.MaxPort}.");
    }

    public static void MaxLength(string value, int maxLength, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value.Length > maxLength)
            throw new ArgumentException($"Value exceeds maximum length of {maxLength}.", paramName);
    }

    public static void ValidDatabaseName(string name, [CallerArgumentExpression(nameof(name))] string? paramName = null)
    {
        NotNullOrEmpty(name, paramName);
        MaxLength(name, AhirConstants.MaxDatabaseNameLength, paramName);
    }

    public static void ValidCollectionName(string name, [CallerArgumentExpression(nameof(name))] string? paramName = null)
    {
        NotNullOrEmpty(name, paramName);
        MaxLength(name, AhirConstants.MaxCollectionNameLength, paramName);
    }
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class NotNullAttribute : Attribute { }