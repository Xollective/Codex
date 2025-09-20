using System.ComponentModel;
using System.Runtime.InteropServices;
using Codex.Utilities.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Codex.Utilities;

public static class IOUtilities
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        ref FILE_SET_SPARSE_BUFFER lpInBuffer,
        int nInBufferSize,
        nint OutBuffer,
        int nOutBufferSize,
        ref int pBytesReturned,
        [In] ref NativeOverlapped lpOverlapped);

    // Define the FSCTL_SET_SPARSE constant
    private const uint FSCTL_SET_SPARSE = 0x900C4;

    // Define the FILE_SET_SPARSE_BUFFER structure
    [StructLayout(LayoutKind.Sequential)]
    private struct FILE_SET_SPARSE_BUFFER
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool SetSparse;
    }

    public static bool SetSparseFlag(this FileStream fileStream, bool value)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        // Initialize FILE_SET_SPARSE_BUFFER structure
        var sparseBuffer = new FILE_SET_SPARSE_BUFFER
        {
            SetSparse = value
        };

        int bytesReturned = 0;
        var lpOverlapped = default(NativeOverlapped);
        return DeviceIoControl(
                fileStream.SafeFileHandle,
                FSCTL_SET_SPARSE,
                ref sparseBuffer,
                Marshal.SizeOf(sparseBuffer),
                nint.Zero,
                0,
                ref bytesReturned,
                ref lpOverlapped);
    }
}

public static class SdkCollectionUtilities
{
    
}