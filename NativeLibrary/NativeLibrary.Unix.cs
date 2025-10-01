// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETCOREAPP3_0_OR_GREATER

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class NativeLibrary
    {
        private const int LoadWithAlteredSearchPathFlag = 0;

#if TARGET_OSX || TARGET_MACCATALYST || TARGET_IOS || TARGET_TVOS
        private const string LibName = "libSystem.B.dylib";
#else
        private const string LibName = "libdl.so.2";
#endif

        [DllImport(LibName)]
        private static extern IntPtr dlopen(string filename, int flags);

        [DllImport(LibName)]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport(LibName)]
        private static extern int dlclose(IntPtr handle);

        [DllImport(LibName)]
        private static extern IntPtr dlerror();

        // for dlopen's flags
        const int RTLD_LAZY = 1;
        const int RTLD_NOW = 2;


        private static IntPtr LoadLibraryHelper(string libraryName, int _ /*flags*/, ref LoadLibErrorTracker errorTracker)
        {
            IntPtr ret = dlopen(libraryName, RTLD_LAZY);
            if (ret == IntPtr.Zero)
            {
                string? message = Marshal.PtrToStringUTF8(dlerror());
                errorTracker.TrackErrorMessage(message ?? string.Empty);
            }

            return ret;
        }

        private static void FreeLib(IntPtr handle)
        {
            Debug.Assert(handle != IntPtr.Zero);

            dlclose(handle);
        }

        private static unsafe IntPtr GetSymbolOrNull(IntPtr handle, string symbolName)
        {
            return dlsym(handle, symbolName);
        }

        internal struct LoadLibErrorTracker
        {
            private string? _errorMessage;

            public void Throw(string libraryName)
            {
                // DUMMY_TODO: strings
#if TARGET_OSX || TARGET_MACCATALYST || TARGET_IOS || TARGET_TVOS
                throw new DllNotFoundException(string.Format("SR.DllNotFound_Mac, {}, {}", libraryName, _errorMessage));
#else
                throw new DllNotFoundException(string.Format("SR.DllNotFound_Linux, {}, {}", libraryName, _errorMessage));
#endif
            }

            public void TrackErrorMessage(string message)
            {
                _errorMessage ??= Environment.NewLine;
                if (!_errorMessage.Contains(message))
                {
                    _errorMessage += message + Environment.NewLine;
                }
            }
        }
    }
}

#endif