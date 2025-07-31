using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Ide.Common
{
    public static class JobObjectUtilities
    {
        public static Action<string> WriteLineHandler = m => Console.Error.WriteLine(m);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool QueryInformationJobObject(IntPtr hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, int cbJobObjectInfoLength, IntPtr lpReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetInformationJobObject(IntPtr hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool IsProcessInJob(IntPtr hProcess, IntPtr hJob, [MarshalAs(UnmanagedType.Bool)] out bool result);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        public record ProcessRef(Process Process)
        {
            public static implicit operator ProcessRef(int processId) => new(Process.GetProcessById(processId));

            public static implicit operator ProcessRef(Process process) => new(process);

            public static implicit operator int(ProcessRef p) => p.Process.Id;

            public static implicit operator uint(ProcessRef p) => (uint)p.Process.Id;
        }

        public static void WriteLine(string message)
        {
            WriteLineHandler?.Invoke(message);
        }

        public static bool IsProcessInJobObject(ProcessRef processRef)
        {
            try
            {
                // Open the process with specific access rights
                IntPtr hProcess = processRef.Process.Handle;
                if (hProcess == IntPtr.Zero)
                {
                    throw new Exception("Failed to open process.");
                }

                var success = IsProcessInJob(hProcess, IntPtr.Zero, out var result);

                if (!success)
                {
                    throw new Exception("QueryInformationJobObject failed: " + GetLastWin32Exception());
                }

                return result;
            }
            catch (Exception ex)
            {
                WriteLine("Error checking process job object association: " + ex.Message);
                return false;
            }
        }

        private static Exception GetLastWin32Exception()
        {
            return Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        public record struct JobResult(IntPtr JobHandle, bool Created, bool Assigned);

        public static JobResult CreateOrGetJobObject(ProcessRef process = null, bool forceNew = false, string jobObjectName = null)
        {
            JobResult result = default;
            result.JobHandle = IntPtr.Zero;

            try
            {
                process ??= Process.GetCurrentProcess();

                // Check if current process is already in a job object
                if (forceNew || !IsProcessInJobObject(process))
                {
                    // Create a new job object if not already associated
                    var hJob = result.JobHandle = CreateJobObject(IntPtr.Zero, jobObjectName);
                    if (result.JobHandle == IntPtr.Zero)
                    {
                        throw new Exception("Failed to create Job Object. Error: " + GetLastWin32Exception());
                    }

                    result.Created = true;

                    // Assign the current process to the job object
                    if (!AssignProcessToJobObject(result.JobHandle, process.Process.Handle))
                    {
                        throw new Exception("Failed to assign current process to Job Object. Error: " + GetLastWin32Exception());
                    }

                    result.Assigned = true;

                    // Set job object to terminate processes when the job object is closed
                    var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
                    {
                        BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                        {
                            LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                        }
                    };

                    int length = Marshal.SizeOf(info);
                    IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
                    Marshal.StructureToPtr(info, extendedInfoPtr, false);

                    if (!SetInformationJobObject(hJob, JobObjectInfoType.ExtendedLimitInformation, extendedInfoPtr, (uint)length))
                    {
                        throw new Exception("Failed to set information on Job Object. Error: " + GetLastWin32Exception());
                    }

                    Marshal.FreeHGlobal(extendedInfoPtr);
                }
                else
                {
                    WriteLine("Current process is already part of a job object.");
                }
            }
            catch (Exception ex)
            {
                WriteLine("Error creating or getting Job Object: " + ex);

                // Cleanup if an error occurred
                if (result.JobHandle != IntPtr.Zero)
                {
                    CloseHandle(result.JobHandle);
                }

                result.JobHandle = IntPtr.Zero;
            }

            return result;
        }

        // Constants and structures for Job Object information
        private const int JOB_OBJECT_QUERY = 0x0004;

        private enum JobObjectInfoType
        {
            JobObjectBasicProcessIdList = 3,
            ExtendedLimitInformation = 9
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_PROCESS_ID_LIST
        {
            public uint NumberOfAssignedProcesses;

            public uint NumberOfProcessIdsInList;

            public UIntPtr ProcessIdList;
        }


        // Job object limit flag to terminate processes when job object is closed
        private const int JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

        // Structures required for job object configuration
        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public Int64 PerProcessUserTimeLimit;
            public Int64 PerJobUserTimeLimit;
            public int LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public int ActiveProcessLimit;
            public Int64 Affinity;
            public int PriorityClass;
            public int SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public UInt64 ReadOperationCount;
            public UInt64 WriteOperationCount;
            public UInt64 OtherOperationCount;
            public UInt64 ReadTransferCount;
            public UInt64 WriteTransferCount;
            public UInt64 OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public Int64 ProcessMemoryLimit;
            public Int64 JobMemoryLimit;
            public Int64 PeakProcessMemoryUsed;
            public Int64 PeakJobMemoryUsed;
        }
    }
}
