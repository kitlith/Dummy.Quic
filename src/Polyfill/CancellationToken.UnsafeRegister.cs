using System.Reflection;

namespace Dummy.Quic.Polyfill;

#if !NETCOREAPP3_0_OR_GREATER
public static class CancellationTokenExtension
{
    private delegate CancellationTokenRegistration InternalRegisterWithoutECDelegate(CancellationToken _this, Action<object> callback, Object state);

    private static readonly InternalRegisterWithoutECDelegate InternalRegisterWithoutEC;

    static CancellationTokenExtension()
    {
        var method = typeof(CancellationToken).GetMethod("InternalRegisterWithoutEC", BindingFlags.Static | BindingFlags.NonPublic);
        InternalRegisterWithoutEC = (InternalRegisterWithoutECDelegate)Delegate.CreateDelegate(typeof(InternalRegisterWithoutECDelegate), method);
    }

    public static CancellationTokenRegistration UnsafeRegister(this CancellationToken token, Action<object> callback, Object state) => InternalRegisterWithoutEC(token, callback, state);
}
#endif