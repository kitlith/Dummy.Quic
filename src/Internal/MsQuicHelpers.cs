// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Quic;
using static Microsoft.Quic.MsQuic;

namespace Dummy.Quic;

internal static class MsQuicHelpers
{
    internal static bool TryParse(this EndPoint endPoint, out string? host, out IPAddress? address, out int port)
    {
        if (endPoint is DnsEndPoint dnsEndPoint)
        {
            host = IPAddress.TryParse(dnsEndPoint.Host, out address) ? null : dnsEndPoint.Host;
            port = dnsEndPoint.Port;
            return true;
        }

        if (endPoint is IPEndPoint ipEndPoint)
        {
            host = null;
            address = ipEndPoint.Address;
            port = ipEndPoint.Port;
            return true;
        }

        host = default;
        address = default;
        port = default;
        return false;
    }

    internal static AddressFamily QuicAddressFamilyToAddressFamily(int quicAddressFamily)
    {
        if (quicAddressFamily == QUIC_ADDRESS_FAMILY_UNSPEC)
        {
            return AddressFamily.Unspecified;
        }
        else if (quicAddressFamily == QUIC_ADDRESS_FAMILY_INET)
        {
            return AddressFamily.InterNetwork;
        }
        else if (quicAddressFamily == QUIC_ADDRESS_FAMILY_INET6)
        {
            return AddressFamily.InterNetworkV6;
        }
        else
        {
            throw new ArgumentException("Unexpected quic address family", "quicAddressFamily");
        }
    }

    internal static int AddressFamilyToQuicAddressFamily(AddressFamily family) => family switch
    {
        AddressFamily.Unspecified => QUIC_ADDRESS_FAMILY_UNSPEC,
        AddressFamily.InterNetwork => QUIC_ADDRESS_FAMILY_INET,
        AddressFamily.InterNetworkV6 => QUIC_ADDRESS_FAMILY_INET6,
        _ => throw new ArgumentException("Unexpected address family", "family"),
    };

    internal static unsafe IPEndPoint QuicAddrToIPEndPoint(QuicAddr* quicAddress, AddressFamily? addressFamilyOverride = null)
    {
        // DUMMY_PERF: We have a SocketAddress at Home.
        AddressFamily family = addressFamilyOverride ?? QuicAddressFamilyToAddressFamily(quicAddress->Family);

        if (family == AddressFamily.InterNetwork)
        {
            return new IPEndPoint(new IPAddress(new ReadOnlySpan<byte>(quicAddress->Ipv4.sin_addr, 4)), (ushort)IPAddress.NetworkToHostOrder((short)quicAddress->Ipv4.sin_port));
        }
        else if (family == AddressFamily.InterNetworkV6)
        {
            return new IPEndPoint(new IPAddress(new ReadOnlySpan<byte>(quicAddress->Ipv6.sin6_addr, 16)), (ushort)IPAddress.NetworkToHostOrder((short)quicAddress->Ipv6.sin6_port));
        } else
        {
            throw new ArgumentException("address family was neither ipv4 or ipv6!");
        }
    }

    internal static QuicAddr ToQuicAddr(this IPEndPoint ipEndPoint)
    {
        QuicAddr result = default;
        Span<byte> rawAddress = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref result, 1));

        var addr = ipEndPoint.Serialize();
        
        // DUMMY_PERF: extra copy that upstream doesn't need to do.
        int count = addr.Size;
        for (int iii = 0; iii < count; ++iii)
        {
            rawAddress[iii] = addr[iii];
        }
        return result;
    }

    internal static unsafe T GetMsQuicParameter<T>(MsQuicSafeHandle handle, uint parameter)
        where T : unmanaged
    {
        T value;
        GetMsQuicParameter(handle, parameter, (uint)sizeof(T), (byte*)&value);
        return value;
    }
    internal static unsafe void GetMsQuicParameter(MsQuicSafeHandle handle, uint parameter, uint length, byte* value)
    {
        int status = MsQuicApi.Api.GetParam(
            handle,
            parameter,
            &length,
            value);

        if (StatusFailed(status))
        {
            ThrowHelper.ThrowMsQuicException(status, $"GetParam({handle}, {parameter}) failed");
        }
    }

    internal static unsafe void SetMsQuicParameter<T>(MsQuicSafeHandle handle, uint parameter, T value)
        where T : unmanaged
    {
        SetMsQuicParameter(handle, parameter, (uint)sizeof(T), (byte*)&value);
    }
    internal static unsafe void SetMsQuicParameter(MsQuicSafeHandle handle, uint parameter, uint length, byte* value)
    {
        int status = MsQuicApi.Api.SetParam(
            handle,
            parameter,
            length,
            value);

        if (StatusFailed(status))
        {
            ThrowHelper.ThrowMsQuicException(status, $"SetParam({handle}, {parameter}) failed");
        }
    }
}
