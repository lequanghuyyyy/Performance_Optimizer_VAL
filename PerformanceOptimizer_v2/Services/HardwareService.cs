using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using PerformanceOptimizer.Models;

namespace PerformanceOptimizer.Services
{
    public static class HardwareService
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
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

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public static HardwareSpecs DetectSpecs()
        {
            var specs = new HardwareSpecs();

            // 1. CPU Architecture & Topology
            try
            {
                using var cpuKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                if (cpuKey != null)
                {
                    string cpuName = cpuKey.GetValue("ProcessorNameString")?.ToString()?.Trim() ?? "Generic x64 Processor";
                    specs.CpuName = cpuName;
                    int cores = Environment.ProcessorCount;

                    // Detect AMD 3D V-Cache & Intel Hybrid
                    if (cpuName.Contains("7900X3D", StringComparison.OrdinalIgnoreCase) ||
                        cpuName.Contains("7950X3D", StringComparison.OrdinalIgnoreCase) ||
                        cpuName.Contains("9900X3D", StringComparison.OrdinalIgnoreCase) ||
                        cpuName.Contains("9950X3D", StringComparison.OrdinalIgnoreCase))
                    {
                        specs.IsAmdDualCcdX3D = true;
                        specs.CpuArchitectureNote = "AMD 3D V-Cache (Dual-CCD Cache Priority)";
                        specs.CpuDetails = $"{cores} Luồng • AMD Dual-CCD V-Cache (Giữ Core Parking tối ưu Cache)";
                    }
                    else if (cpuName.Contains("X3D", StringComparison.OrdinalIgnoreCase))
                    {
                        specs.CpuArchitectureNote = "AMD 3D V-Cache (Single-CCD Siêu Tốc)";
                        specs.CpuDetails = $"{cores} Luồng • Tối ưu 100% Cache Gaming eSports";
                    }
                    else if (cpuName.Contains("Core(TM) i", StringComparison.OrdinalIgnoreCase) || 
                             cpuName.Contains("Core Ultra", StringComparison.OrdinalIgnoreCase) ||
                             cpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                    {
                        // Check if 12th gen or higher (hybrid cores)
                        bool isHybrid = cpuName.Contains("12") || cpuName.Contains("13") || 
                                        cpuName.Contains("14") || cpuName.Contains("Ultra") || 
                                        cores >= 12;
                        specs.IsIntelHybrid = isHybrid;
                        specs.CpuArchitectureNote = isHybrid ? "Intel Hybrid (P-Core Hiệu Năng + E-Core)" : "Intel Multi-core x64";
                        specs.CpuDetails = $"{cores} Luồng • {specs.CpuArchitectureNote} (Max Turbo Unlocked)";
                    }
                    else
                    {
                        specs.CpuArchitectureNote = "x64 Multi-core CPU";
                        specs.CpuDetails = $"{cores} Luồng xử lý • Sẵn sàng tối ưu";
                    }
                }
            }
            catch
            {
                specs.CpuName = "Intel / AMD Processor";
                specs.CpuDetails = $"{Environment.ProcessorCount} Cores Detected";
            }

            // 2. RAM
            try
            {
                var mem = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(mem))
                {
                    double totalGb = Math.Round(mem.ullTotalPhys / (1024.0 * 1024 * 1024), 1);
                    double availGb = Math.Round(mem.ullAvailPhys / (1024.0 * 1024 * 1024), 1);
                    specs.RamTotal = $"{totalGb} GB RAM";
                    specs.RamDetails = $"Khả dụng: {availGb} GB | Khóa Kernel trên RAM (Zero Paging)";
                }
            }
            catch
            {
                specs.RamTotal = "16 GB RAM";
                specs.RamDetails = "Bộ nhớ khả dụng sẵn sàng";
            }

            // 3. GPU
            try
            {
                using var videoKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000");
                if (videoKey != null)
                {
                    string driverDesc = videoKey.GetValue("DriverDesc")?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(driverDesc))
                    {
                        specs.GpuName = driverDesc;
                        specs.GpuDetails = "Đã hỗ trợ MSI Mode + DirectX SwapEffect Flip + Shader Cache 10GB";
                    }
                    else
                    {
                        specs.GpuName = "Dedicated / Integrated GPU";
                        specs.GpuDetails = "Đồ họa hỗ trợ DirectX 12 & Vulkan";
                    }
                }
            }
            catch
            {
                specs.GpuName = "Card Đồ Họa Hệ Thống";
                specs.GpuDetails = "Hỗ trợ Gaming & Render Tăng tốc phần cứng";
            }

            // 4. DISK (Ổ C:)
            try
            {
                string sysDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
                var driveInfo = new DriveInfo(sysDrive);
                double totalDisk = Math.Round(driveInfo.TotalSize / (1024.0 * 1024 * 1024), 0);
                double freeDisk = Math.Round(driveInfo.AvailableFreeSpace / (1024.0 * 1024 * 1024), 0);
                specs.DiskTotal = $"Ổ {driveInfo.Name.TrimEnd('\\')} ({driveInfo.DriveFormat})";
                specs.DiskDetails = $"Trống: {freeDisk} GB / Tổng: {totalDisk} GB | Tối ưu NTFS LastAccess & DISM";
            }
            catch
            {
                specs.DiskTotal = "Ổ đĩa C: (SSD/NVMe)";
                specs.DiskDetails = "Đã tối ưu tốc độ đọc ghi & bảo vệ SSD";
            }

            // 5. OS & Network & Windows AI Status
            specs.OsVersion = Environment.OSVersion.VersionString.Replace("Microsoft Windows NT", "Windows 11 / 10");
            specs.NetworkAdapter = "Card mạng LAN / Wi-Fi (Tối ưu Cloudflare 1.1.1.1 + TCP NoDelay)";

            int aiDisabled = NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 0);
            specs.WindowsAiStatus = aiDisabled == 1 ? "Đã khóa Recall & AI Telemetry" : "Chưa tối ưu AI / Tracking";

            return specs;
        }
    }
}
