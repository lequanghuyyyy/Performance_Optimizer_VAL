using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PerformanceOptimizer.Services
{
    public static class MemoryCleanerService
    {
        [DllImport("ntdll.dll")]
        private static extern int NtSetSystemInformation(int SystemInformationClass, ref int SystemInformation, int SystemInformationLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, out long lpLuid);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            public long Luid;
            public int Attributes;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        private const int SE_PRIVILEGE_ENABLED = 0x00000002;
        private const int TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const int TOKEN_QUERY = 0x0008;

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        public class MemoryPurgeResult
        {
            public ulong AvailableBeforeMb { get; set; }
            public ulong AvailableAfterMb { get; set; }
            public long FreedMb { get; set; }
            public bool Success { get; set; }
        }

        public static ulong GetAvailableMemoryMb()
        {
            try
            {
                var mem = new MEMORYSTATUSEX();
                mem.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (GlobalMemoryStatusEx(ref mem))
                {
                    return mem.ullAvailPhys / (1024 * 1024);
                }
            }
            catch { }
            return 0;
        }

        public static bool CleanStandbyMemory()
        {
            try
            {
                // 1. Cấp quyền SeProfileSingleProcessPrivilege
                if (OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr token))
                {
                    var tp = new TOKEN_PRIVILEGES
                    {
                        PrivilegeCount = 1,
                        Attributes = SE_PRIVILEGE_ENABLED
                    };
                    if (LookupPrivilegeValue(null, "SeProfileSingleProcessPrivilege", out tp.Luid))
                    {
                        AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
                    }
                }

                // 2. Gọi NtSetSystemInformation với mã lệnh 80 (SystemMemoryListInformation) và 4 (MemoryPurgeStandbyList)
                int command = 4;
                int result = NtSetSystemInformation(80, ref command, sizeof(int));
                return result == 0;
            }
            catch
            {
                return false;
            }
        }

        public static MemoryPurgeResult PurgeRamDetailed()
        {
            var res = new MemoryPurgeResult();
            try
            {
                res.AvailableBeforeMb = GetAvailableMemoryMb();

                // 1. Purge Standby List
                bool ok = CleanStandbyMemory();

                // 2. Empty inactive working sets
                try
                {
                    int cmdWorkingSets = 3; // MemoryEmptyWorkingSets
                    NtSetSystemInformation(80, ref cmdWorkingSets, sizeof(int));
                }
                catch { }

                // 3. Local GC cleanup
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Allow kernel counter sync
                Thread.Sleep(120);

                res.AvailableAfterMb = GetAvailableMemoryMb();
                res.FreedMb = (long)res.AvailableAfterMb - (long)res.AvailableBeforeMb;
                res.Success = ok;
            }
            catch
            {
                res.Success = false;
            }
            return res;
        }
    }
}
