using System.Runtime.CompilerServices;

namespace Dummy.Quic.Polyfill;

public static class ConditionalWeakTableExtension
{
    public static bool TryAdd<TKey, TValue>(this ConditionalWeakTable<TKey, TValue> table, TKey key, TValue value)
        where TKey : class
        where TValue : class?
    {
        try
        {
            table.Add(key, value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}