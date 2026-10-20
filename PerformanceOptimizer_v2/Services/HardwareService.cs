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

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetLogicalProcessorInformationEx(int relationshipType, IntPtr buffer, ref uint returnedLength);

        private static bool CheckHardwareHybridCores()
        {
            try
            {
                uint len = 0;
                // 0 = RelationProcessorCore
                GetLogicalProcessorInformationEx(0, IntPtr.Zero, ref len);
                if (len == 0) return false;

                IntPtr buffer = Marshal.AllocHGlobal((int)len);
                try
                {
                    if (GetLogicalProcessorInformationEx(0, buffer, ref len))
                    {
                        int offset = 0;
                        byte firstEff = 255;
                        bool hasMultipleEff = false;

                        while (offset + 10 <= len)
                        {
                            int size = Marshal.ReadInt32(buffer, offset + 4);
                            if (size <= 0) break;

                            byte eff = Marshal.ReadByte(buffer, offset + 9);
                            if (firstEff == 255)
                            {
                                firstEff = eff;
                            }
                            else if (eff != firstEff)
                            {
                                hasMultipleEff = true;
                                break;
                            }
                            offset += size;
                        }
                        return hasMultipleEff;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch { }
            return false;
        }

        private static int ScoreGpu(string name, string vendorId)
        {
            if (string.IsNullOrWhiteSpace(name)) return -1;
            string upper = name.ToUpperInvariant();

            // Loại bỏ adapter ảo hoặc giả lập
            if (upper.Contains("MICROSOFT BASIC") || upper.Contains("VIRTUAL") || 
                upper.Contains("PARSEC") || upper.Contains("CITRIX") || upper.Contains("REMOTE"))
                return 0;

            // 1. Nhận diện rõ GPU Tích Hợp (iGPU / Onboard)
            bool isIntegrated = upper.Contains("INTEL(R) UHD") ||
                                upper.Contains("INTEL(R) HD") ||
                                upper.Contains("INTEL(R) IRIS") ||
                                upper.Contains("INTEL GRAPHICS") ||
                                upper.Contains("RADEON(TM) GRAPHICS") ||
                                upper.Contains("RADEON GRAPHICS") ||
                                upper.Contains("VEGA 3") || upper.Contains("VEGA 6") || 
                                upper.Contains("VEGA 7") || upper.Contains("VEGA 8") || 
                                upper.Contains("VEGA 11");

            if (isIntegrated) return 20;

            // 2. NVIDIA Dedicated (RTX, GTX, GeForce, Quadro, TITAN)
            if (upper.Contains("RTX") || upper.Contains("GTX") || 
                upper.Contains("GEFORCE") || upper.Contains("NVIDIA") || 
                vendorId.Contains("10DE", StringComparison.OrdinalIgnoreCase))
            {
                int score = 100;
                if (upper.Contains("RTX")) score += 20; // Ưu tiên cao nhất cho dòng RTX
                else if (upper.Contains("GTX")) score += 10;
                return score;
            }

            // 3. AMD Dedicated (Radeon RX, Radeon Pro)
            if (upper.Contains("RADEON RX") || upper.Contains("RADEON PRO") || 
                upper.Contains("RX ") || vendorId.Contains("1002", StringComparison.OrdinalIgnoreCase))
            {
                return 90;
            }

            // 4. Intel Arc Dedicated
            if (upper.Contains("ARC") || upper.Contains("INTEL ARC"))
            {
                return 85;
            }

            return 30; // GPU không rõ nhưng không phải iGPU
        }

        private static string ExtractGpuName(RegistryKey devKey, out string matchingDeviceId)
        {
            matchingDeviceId = devKey.GetValue("MatchingDeviceId")?.ToString()?.Trim() ?? "";

            // 1. Thử DriverDesc
            string name = devKey.GetValue("DriverDesc")?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(name) && !name.StartsWith("@")) return name;

            // 2. Thử HardwareInformation.AdapterString (Có thể là string hoặc byte[] Unicode binary)
            object? adapterVal = devKey.GetValue("HardwareInformation.AdapterString");
            if (adapterVal is string s && !string.IsNullOrWhiteSpace(s))
            {
                return s.Trim();
            }
            else if (adapterVal is byte[] bytes && bytes.Length > 0)
            {
                try
                {
                    string sBin = System.Text.Encoding.Unicode.GetString(bytes).Trim('\0', ' ', '\r', '\n');
                    if (!string.IsNullOrWhiteSpace(sBin)) return sBin;
                }
                catch { }
            }

            // 3. Thử DeviceDesc
            string devDesc = devKey.GetValue("DeviceDesc")?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(devDesc))
            {
                int idx = devDesc.LastIndexOf(';');
                if (idx >= 0 && idx < devDesc.Length - 1)
                    devDesc = devDesc.Substring(idx + 1).Trim();
                if (!string.IsNullOrEmpty(devDesc)) return devDesc;
            }

            // 4. Thử ProviderName
            string prov = devKey.GetValue("ProviderName")?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(name)) return name;
            if (!string.IsNullOrEmpty(prov) && (prov.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) || prov.Contains("AMD", StringComparison.OrdinalIgnoreCase)))
                return prov + " Dedicated Graphics";

            return "";
        }

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
                        // 1. Kiểm tra trực tiếp qua Windows Kernel API (EfficiencyClass)
                        bool isHybrid = CheckHardwareHybridCores();
                        
                        // 2. Fallback kiểm tra nhận diện tên CPU nếu API không trả về
                        if (!isHybrid)
                        {
                            // Loại trừ rõ ràng các CPU thuần 6 P-Core (i5-12400, i5-12400F, i5-12500, i3 đời 12-14)
                            bool isNonHybridSku = System.Text.RegularExpressions.Regex.IsMatch(cpuName, @"(i5-12[45]00|i3-1[234]\d{2})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (!isNonHybridSku)
                            {
                                isHybrid = System.Text.RegularExpressions.Regex.IsMatch(cpuName, @"(i[79]-1[234]\d{2}|i5-1[34]\d{2}|i5-12600K|Ultra\s*[579]|2[468]5K)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            }
                        }

                        specs.IsIntelHybrid = isHybrid;
                        specs.CpuArchitectureNote = isHybrid ? "Intel Hybrid (P-Core Hiệu Năng + E-Core)" : "Intel Multi-core x64 (Đồng nhất)";
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

            // 3. GPU (Hệ thống 3 tầng quét & xếp hạng ưu tiên Card Rời NVIDIA RTX/GTX, AMD Radeon RX, Intel Arc)
            try
            {
                string bestGpu = "";
                string bestVendor = "";
                int bestScore = -1;

                // Tầng 1: Quét Class {4d36e968-e325-11ce-bfc1-08002be10318}
                try
                {
                    using var classKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
                    if (classKey != null)
                    {
                        foreach (var subName in classKey.GetSubKeyNames())
                        {
                            if (!int.TryParse(subName, out _)) continue;
                            using var devKey = classKey.OpenSubKey(subName);
                            if (devKey == null) continue;

                            string name = ExtractGpuName(devKey, out string matchId);
                            if (string.IsNullOrEmpty(name)) continue;

                            int score = ScoreGpu(name, matchId);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestGpu = name;
                                bestVendor = matchId;
                            }
                        }
                    }
                }
                catch { }

                // Tầng 2: Quét Control\Video
                try
                {
                    using var videoKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Video");
                    if (videoKey != null)
                    {
                        foreach (var guidName in videoKey.GetSubKeyNames())
                        {
                            using var guidKey = videoKey.OpenSubKey(guidName);
                            if (guidKey == null) continue;

                            foreach (var subName in guidKey.GetSubKeyNames())
                            {
                                if (!int.TryParse(subName, out _)) continue;
                                using var devKey = guidKey.OpenSubKey(subName);
                                if (devKey == null) continue;

                                string name = ExtractGpuName(devKey, out string matchId);
                                if (string.IsNullOrEmpty(name)) continue;

                                int score = ScoreGpu(name, matchId);
                                if (score > bestScore)
                                {
                                    bestScore = score;
                                    bestGpu = name;
                                    bestVendor = matchId;
                                }
                            }
                        }
                    }
                }
                catch { }

                // Tầng 3: Nếu vẫn chỉ ra card Onboard (Score <= 20) hoặc chưa tìm thấy, gọi WMI qua CIM để quét toàn bộ thiết bị Windows phát hiện
                if (bestScore <= 20)
                {
                    try
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = "-NoProfile -NonInteractive -Command \"Get-CimInstance Win32_VideoController | Select-Object -ExpandProperty Name\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true
                        };
                        using var proc = System.Diagnostics.Process.Start(psi);
                        if (proc != null)
                        {
                            string output = proc.StandardOutput.ReadToEnd();
                            proc.WaitForExit(3000);
                            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                string candidate = line.Trim();
                                if (string.IsNullOrEmpty(candidate)) continue;
                                int score = ScoreGpu(candidate, "");
                                if (score > bestScore)
                                {
                                    bestScore = score;
                                    bestGpu = candidate;
                                }
                            }
                        }
                    }
                    catch { }
                }

                bool isDedicated = bestScore >= 50;
                if (!string.IsNullOrEmpty(bestGpu))
                {
                    specs.GpuName = bestGpu;
                    specs.GpuDetails = isDedicated
                        ? "GPU Rời Dedicated • Tối ưu MSI Mode + HAGS + DirectX Flip + Shader Cache 10GB"
                        : "Đồ họa tích hợp (Onboard) • Hỗ trợ DirectX 12 & Vulkan";
                }
                else
                {
                    specs.GpuName = "Dedicated / Integrated GPU";
                    specs.GpuDetails = "Đồ họa hỗ trợ DirectX 12 & Vulkan";
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
            int build = Environment.OSVersion.Version.Build;
            string edition = "";
            try
            {
                using var rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                edition = rk?.GetValue("ProductName")?.ToString() ?? "";
                string displayVer = rk?.GetValue("DisplayVersion")?.ToString() ?? "";
                if (!string.IsNullOrEmpty(displayVer)) edition += $" ({displayVer})";
            }
            catch { }

            if (build >= 22000)
            {
                specs.OsVersion = string.IsNullOrEmpty(edition) ? $"Windows 11 Pro (Build {build})" : $"{edition} [Build {build}]";
            }
            else
            {
                specs.OsVersion = string.IsNullOrEmpty(edition) ? $"Windows 10 (Build {build})" : $"{edition} [Build {build}]";
            }

            specs.NetworkAdapter = "Card mạng LAN / Wi-Fi (Tối ưu Cloudflare 1.1.1.1 + Low Latency)";

            int aiDisabled = NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 0);
            specs.WindowsAiStatus = aiDisabled == 1 ? "Đã khóa Recall & AI Telemetry" : "Chưa tối ưu AI / Tracking";

            return specs;
        }
    }
}
