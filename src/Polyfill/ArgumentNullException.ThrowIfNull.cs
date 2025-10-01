using System.Diagnostics.CodeAnalysis;

namespace Dummy.Quic.Polyfill;

public static class ArgumentNullExceptionPolyfill
{
    public static void ThrowIfNull(object? argument, string? paramName = default)
    {
        if (argument is null)
        {
            Throw(paramName);
        }
    }

    [DoesNotReturn]
    internal static void Throw(string? paramName) =>
        throw new ArgumentNullException(paramName);
}