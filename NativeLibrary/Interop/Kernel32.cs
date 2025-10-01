using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

// this class is primarily constructed out of copies of older versions of files from the dotnet runtime, predating LibraryImport.
internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // Interop.SetThreadErrorMode.cs
        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        [SuppressGCTransition]
        internal static extern bool SetThreadErrorMode(uint dwNewMode, out uint lpOldMode);

        internal const uint SEM_FAILCRITICALERRORS = 1;
        internal const int SEM_NOOPENFILEERRORBOX = 0x00008000;

        // Interop.LoadLibraryEx_IntPtr.cs
        internal const int LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        internal const int LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryExW", SetLastError = true, ExactSpelling = true)]
        internal static extern IntPtr LoadLibraryEx(string libFilename, IntPtr reserved, int flags);

        // Interop.FreeLibrary.cs
        [DllImport(Libraries.Kernel32, ExactSpelling = true, SetLastError = true)]
        internal static extern bool FreeLibrary(IntPtr hModule);

        // Interop.GetProcAddress.cs
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Ansi, BestFitMapping = false)]
        public static extern IntPtr GetProcAddress(SafeLibraryHandle hModule, string lpProcName);

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Ansi, BestFitMapping = false)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
    }
}