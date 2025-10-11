
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Quic;

using CONNECTED_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._CONNECTED_e__Struct;
using LOCAL_ADDRESS_CHANGED_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._LOCAL_ADDRESS_CHANGED_e__Struct;
using PEER_ADDRESS_CHANGED_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._PEER_ADDRESS_CHANGED_e__Struct;
using PEER_CERTIFICATE_RECEIVED_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._PEER_CERTIFICATE_RECEIVED_e__Struct;
using PEER_STREAM_STARTED_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._PEER_STREAM_STARTED_e__Struct;
using STREAMS_AVAILABLE_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._STREAMS_AVAILABLE_e__Struct;
using SHUTDOWN_COMPLETE_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._SHUTDOWN_COMPLETE_e__Struct;
using SHUTDOWN_INITIATED_BY_PEER_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._SHUTDOWN_INITIATED_BY_PEER_e__Struct;
using SHUTDOWN_INITIATED_BY_TRANSPORT_DATA = Microsoft.Quic.QUIC_CONNECTION_EVENT._Anonymous_e__Union._SHUTDOWN_INITIATED_BY_TRANSPORT_e__Struct;

namespace Dummy.Quic;

public abstract class QuicConnection
{
    protected MsQuicContextSafeHandle _handle;

    internal MsQuicSafeHandle Handle { get => _handle; }
    protected unsafe nint HandleInt { get => (nint)_handle.QuicHandle; }
    protected bool _disposed;
    private readonly MsQuicTlsSecret? _tlsSecret;

    protected unsafe QuicConnection(GCHandleType handleType = GCHandleType.Weak) : this(&NativeCallback, handleType) { }

    protected unsafe QuicConnection(delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_CONNECTION_EVENT*, int> callback, GCHandleType handleType = GCHandleType.Weak)
    {
        GCHandle context = GCHandle.Alloc(this, handleType);
        try
        {
            QUIC_HANDLE* handle;
            ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ConnectionOpen(
                MsQuicApi.Api.Registration,
                callback,
                (void*)GCHandle.ToIntPtr(context),
                &handle),
                "ConnectionOpen failed");
            _handle = new MsQuicContextSafeHandle(handle, context, SafeHandleType.Connection);
        }
        catch
        {
            context.Free();
            throw;
        }

        if (NetEventSource.Log.IsEnabled())
        {
            NetEventSource.Info(this, $"{this} New outbound connection.");
        }

        // _decrementStreamCapacity = DecrementStreamCapacity;
        _tlsSecret = MsQuicTlsSecret.Create(_handle);
    }

    // works with unity if you have a public copy of the attribte defined
    // per https://discussions.unity.com/t/coreclr-and-net-modernization-unite-2024/1519272?page=16
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int NativeCallback(QUIC_HANDLE* connection, void* contextPtr, QUIC_CONNECTION_EVENT* connectionEvent)
    {
        GCHandle contextHandle = GCHandle.FromIntPtr((IntPtr)contextPtr);

        // Check if the instance hasn't been collected.
        if (!contextHandle.IsAllocated || contextHandle.Target is not QuicConnection instance)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Error(null, $"Received event {connectionEvent->Type} for [conn][{(nint)connection:X11}] while connection is already disposed");
            }
            return MsQuic.QUIC_STATUS_INVALID_STATE;
        }

        try
        {
            // Process the event.
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(instance, $"{instance} Received event {connectionEvent->Type} {connectionEvent->ToString()}");
            }
            return instance.HandleConnectionEvent(ref *connectionEvent);
        }
        catch (Exception ex)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Error(instance, $"{instance} Exception while processing event {connectionEvent->Type}: {ex}");
            }
            return MsQuic.QUIC_STATUS_INTERNAL_ERROR;
        }
    }

    protected abstract int HandleConnectionEvent(ref QUIC_CONNECTION_EVENT connectionEvent);

    // base event handlers, handles writing 

    protected int HandleEventConnected(ref CONNECTED_DATA data)
    {
        // Final (1-RTT) secrets have been derived, log them if desired to allow decrypting application traffic.
        _tlsSecret?.WriteSecret();

        // if (NetEventSource.Log.IsEnabled())
        // {
        //     NetEventSource.Info(this, $"{this} Connection connected {LocalEndPoint} -> {RemoteEndPoint} for {_negotiatedApplicationProtocol} protocol");
        // }
        return MsQuic.QUIC_STATUS_SUCCESS;
    }

    protected int HandleEventShutdownComplete(ref SHUTDOWN_COMPLETE_DATA data)
    {
        // make sure we log at least some secrets in case of shutdown before handshake completes.
        _tlsSecret?.WriteSecret();

        return MsQuic.QUIC_STATUS_SUCCESS;
    }

    protected int HandleEventPeerCertificateReceived(ref PEER_CERTIFICATE_RECEIVED_DATA data)
    {
        // Handshake keys should be available by now, log them now if desired.
        _tlsSecret?.WriteSecret();

        // TODO: do our own certificate validation (on android specifically?)

        return MsQuic.QUIC_STATUS_SUCCESS;
    }

    // DUMMY_TODO: should these be public?
    public unsafe void Start(MsQuicConfigurationSafeHandle config, int family, ReadOnlySpan<byte> sni, ushort serverPort)
    {
        fixed (byte* target = sni)
        {
            ThrowHelper.ThrowIfMsQuicError(
                MsQuicApi.Api.ConnectionStart(_handle, config, (ushort)family, (sbyte*)target, serverPort),
                "ConnectionStart failed"
            );
        }
    }

    public void Shutdown(QUIC_CONNECTION_SHUTDOWN_FLAGS flags, ulong code)
    {
        MsQuicApi.Api.ConnectionShutdown(_handle, flags, code);
    }

    public void SetConfiguration(MsQuicConfigurationSafeHandle config)
    {
        ThrowHelper.ThrowIfMsQuicError(
            MsQuicApi.Api.ConnectionSetConfiguration(_handle, config),
            "SetConfiguration failed"
        );
    }

    public void SetParam<T>(uint parameter, T value) where T: unmanaged {
        MsQuicHelpers.SetMsQuicParameter(_handle, parameter, value);
    }

    public T GetParam<T>(uint parameter) where T: unmanaged {
        return MsQuicHelpers.GetMsQuicParameter<T>(_handle, parameter);
    }
}