namespace Dummy.Quic.Polyfill;

// DUMMY_TODO: revisit when c# 14 & wider extension support becomes available
internal static class Char
{
    public static bool IsBetween(char c, char minInclusive, char maxInclusive) =>
        (uint)(c - minInclusive) <= (uint)(maxInclusive - minInclusive);

    public static bool IsAsciiDigit(char c) => IsBetween(c, '0', '9');
}