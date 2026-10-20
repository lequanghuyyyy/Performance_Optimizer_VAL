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

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

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
            IntPtr token = IntPtr.Zero;
            try
            {
                // 1. Cấp quyền SeProfileSingleProcessPrivilege
                using var currentProcess = Process.GetCurrentProcess();
                if (OpenProcessToken(currentProcess.Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out token))
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
                // CHÚ Ý: Không dùng mã 3 (MemoryEmptyWorkingSets) vì sẽ ép rớt bộ nhớ của Riot Vanguard (vgc.exe),
                // gây lỗi mất kết nối chống gian lận VAN 1067 / VAN 84.
                int command = 4;
                int result = NtSetSystemInformation(80, ref command, sizeof(int));
                return result == 0;
            }
            catch
            {
                return false;
            }
            finally
            {
                // Đóng handle token để tránh rò rỉ tài nguyên hệ điều hành
                if (token != IntPtr.Zero)
                {
                    CloseHandle(token);
                }
            }
        }

        public static MemoryPurgeResult PurgeRamDetailed()
        {
            var res = new MemoryPurgeResult();
            try
            {
                res.AvailableBeforeMb = GetAvailableMemoryMb();

                // 1. Purge Standby List (An toàn tuyệt đối cho Valorant & Vanguard)
                bool ok = CleanStandbyMemory();

                // 2. Thu gom rác nội bộ ứng dụng
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Đợi kernel đồng bộ counter bộ nhớ
                Thread.Sleep(100);

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

        #region Background Auto-Purge Daemon
        private static System.Threading.Timer? _autoPurgeTimer;
        public static bool IsAutoPurgeRunning => _autoPurgeTimer != null;

        public static void StartAutoPurgeDaemon(int intervalMinutes = 5)
        {
            if (_autoPurgeTimer != null) return;

            // Xả ngay 1 lần ban đầu
            CleanStandbyMemory();

            int dueMs = intervalMinutes * 60 * 1000;
            _autoPurgeTimer = new System.Threading.Timer(_ =>
            {
                try
                {
                    // Tự động kiểm tra thông minh: Chỉ xả Standby List khi RAM khả dụng < 2500 MB để tránh tăng Disk I/O không cần thiết
                    ulong availMb = GetAvailableMemoryMb();
                    if (availMb < 2500)
                    {
                        CleanStandbyMemory();
                    }
                }
                catch { }
            }, null, dueMs, dueMs);
        }

        public static void StopAutoPurgeDaemon()
        {
            if (_autoPurgeTimer != null)
            {
                _autoPurgeTimer.Dispose();
                _autoPurgeTimer = null;
            }
        }
        #endregion
    }
}
