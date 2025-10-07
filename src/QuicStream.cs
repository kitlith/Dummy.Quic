using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Quic;

namespace Dummy.Quic;

public abstract class QuicStream {
    protected MsQuicContextSafeHandle _handle;
    public MsQuicSafeHandle Handle { get => _handle; }
    protected unsafe nint HandleInt { get => (nint)_handle.QuicHandle; }

    protected unsafe QuicStream(QuicConnection connection, QUIC_STREAM_OPEN_FLAGS flags, GCHandleType handleType = GCHandleType.Weak) : this(connection, flags, &NativeCallback, handleType) { }
    protected unsafe QuicStream(QuicConnection connection, QUIC_STREAM_OPEN_FLAGS flags, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_STREAM_EVENT*, int> callback, GCHandleType handleType = GCHandleType.Weak)
    {
        GCHandle context = GCHandle.Alloc(this, handleType);
        try
        {
            QUIC_HANDLE* handle;
            ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.StreamOpen(
                connection.Handle,
                flags,
                callback,
                (void*)GCHandle.ToIntPtr(context),
                &handle),
                "StreamOpen failed");
            _handle = new MsQuicContextSafeHandle(handle, context, SafeHandleType.Stream, connection.Handle);
        }
        catch
        {
            context.Free();
            throw;
        }
        

        // _defaultErrorCode = defaultErrorCode;

        // _canRead = type == QuicStreamType.Bidirectional;
        // _canWrite = true;
        // if (!_canRead)
        // {
        //     _receiveTcs.TrySetResult(final: true);
        // }
        // _type = type;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int NativeCallback(QUIC_HANDLE* stream, void* context, QUIC_STREAM_EVENT* streamEvent)
    {
        GCHandle stateHandle = GCHandle.FromIntPtr((IntPtr)context);

        // Check if the instance hasn't been collected.
        if (!stateHandle.IsAllocated || stateHandle.Target is not QuicStream instance)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Error(null, $"Received event {streamEvent->Type} for [strm][{(nint)stream:X11}] while stream is already disposed");
            }
            QuicLog.Error?.Invoke($"Received event {streamEvent->Type} for [strm][{(nint)stream:X11}] while stream is already disposed");
            return MsQuic.QUIC_STATUS_INVALID_STATE;
        }

        try
        {
            // Process the event.
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(instance, $"{instance} Received event {streamEvent->Type} {streamEvent->ToString()}");
            }
            QuicLog.Info?.Invoke($"{instance} Received event {streamEvent->Type} {streamEvent->ToString()}");
            return instance.HandleStreamEvent(ref *streamEvent);
        }
        catch (Exception ex)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Error(instance, $"{instance} Exception while processing event {streamEvent->Type}: {ex}");
            }
            QuicLog.Error?.Invoke($"{instance} Exception while processing event {streamEvent->Type}: {ex}");
            return MsQuic.QUIC_STATUS_INTERNAL_ERROR;
        }
    }
    protected abstract int HandleStreamEvent(ref QUIC_STREAM_EVENT streamEvent);
}