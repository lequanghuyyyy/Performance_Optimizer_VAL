using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PerformanceOptimizer.Services
{
    public static class NativeTweakService
    {
        #region Mouse & Keyboard System Parameters
        public static void ApplyMouseKeyboardTweaks(bool eSportsRaw1to1)
        {
            try
            {
                if (eSportsRaw1to1)
                {
                    // Ghi Registry tinh chỉnh Chuột & Bàn phím 1:1 eSports (An toàn tuyệt đối 100%, không can thiệp heap bộ nhớ)
                    SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0");
                    SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "0");
                    SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "0");
                    SetString(RegistryHive.CurrentUser, @"Control Panel\Keyboard", "KeyboardDelay", "0");
                    SetString(RegistryHive.CurrentUser, @"Control Panel\Keyboard", "KeyboardSpeed", "31");
                    SetString(RegistryHive.CurrentUser, @"Control Panel\Accessibility\StickyKeys", "Flags", "506");
                }
                else
                {
                    // Hoàn tác về chuẩn mặc định của Windows
                    SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "1");
                    SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "6");
                    SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "10");
                    SetString(RegistryHive.CurrentUser, @"Control Panel\Keyboard", "KeyboardDelay", "1");
                    SetString(RegistryHive.CurrentUser, @"Control Panel\Keyboard", "KeyboardSpeed", "31");
                    SetString(RegistryHive.CurrentUser, @"Control Panel\Accessibility\StickyKeys", "Flags", "510");
                }
            }
            catch { }
        }
        #endregion

        #region Native High-Speed MSI Mode Engine
        public static bool ApplyMsiModeNative(bool enable)
        {
            try
            {
                using var pciBase = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
                using var pciKey = pciBase.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI", true);
                if (pciKey == null) return false;

                foreach (var devName in pciKey.GetSubKeyNames())
                {
                    try
                    {
                        using var devKey = pciKey.OpenSubKey(devName, true);
                        if (devKey == null) continue;

                        foreach (var instName in devKey.GetSubKeyNames())
                        {
                            try
                            {
                                using var instKey = devKey.OpenSubKey(instName, true);
                                if (instKey == null) continue;

                                string devClass = instKey.GetValue("Class")?.ToString() ?? "";
                                bool isDisplay = devClass.Equals("Display", StringComparison.OrdinalIgnoreCase);
                                bool isNet = devClass.Equals("Net", StringComparison.OrdinalIgnoreCase);

                                if (isDisplay || isNet)
                                {
                                    using var devParams = instKey.CreateSubKey("Device Parameters", true);
                                    if (devParams == null) continue;

                                    using var intMgmt = devParams.CreateSubKey("Interrupt Management", true);
                                    if (intMgmt == null) continue;

                                    if (enable)
                                    {
                                        using var msiKey = intMgmt.CreateSubKey("MessageSignaledInterruptProperties", true);
                                        msiKey?.SetValue("MSISupported", 1, RegistryValueKind.DWord);

                                        if (isDisplay)
                                        {
                                            using var affKey = intMgmt.CreateSubKey("Affinity Policy", true);
                                            affKey?.SetValue("DevicePriority", 3, RegistryValueKind.DWord);
                                        }
                                    }
                                    else
                                    {
                                        // An toàn tuyệt đối: Với GPU Display, giữ nguyên MSISupported = 1 để tránh crash driver card màn hình
                                        // (NVIDIA/AMD mặc định luôn cần MSI), chỉ hoàn tác DevicePriority về mặc định.
                                        if (isDisplay)
                                        {
                                            using var affKey = intMgmt.OpenSubKey("Affinity Policy", true);
                                            affKey?.DeleteValue("DevicePriority", false);
                                        }
                                        else if (isNet)
                                        {
                                            using var msiKey = intMgmt.OpenSubKey("MessageSignaledInterruptProperties", true);
                                            msiKey?.DeleteValue("MSISupported", false);
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Shell & Power Helpers
        public static bool EnsureHighPerformancePowerPlan()
        {
            try
            {
                bool ok = RunCli("powercfg.exe", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                if (!ok)
                {
                    // Fallback duplicate scheme cho laptop hoặc máy Modern Standby (S0)
                    RunCli("powercfg.exe", "-duplicatescheme 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                    ok = RunCli("powercfg.exe", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                }
                if (!ok)
                {
                    // Fallback mở khóa Ultimate Performance
                    RunCli("powercfg.exe", "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
                    RunCli("powercfg.exe", "/setactive e9a42b02-d5df-448d-aa00-03f14749eb61");
                }

                RunCli("powercfg.exe", "-setacvalueindex scheme_current sub_processor PROCTHROTTLEMAX 100");
                RunCli("powercfg.exe", "-setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN 5");
                RunCli("powercfg.exe", "-setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 be337238-0d82-4146-a960-4f3749d470c7 2");
                RunCli("powercfg.exe", "-setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 45bcc044-d885-43e2-8605-ee0ec6e96b59 100");
                RunCli("powercfg.exe", "-setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 36687f9e-e3a5-4dbf-b1dc-15eb381c6863 0");
                RunCli("powercfg.exe", "-setactive scheme_current");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool SetCloudflareDns(bool enableDoH = false)
        {
            try
            {
                string psDns = "Get-NetAdapter -Physical -ErrorAction SilentlyContinue | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ServerAddresses @('1.1.1.1', '1.0.0.1') -ErrorAction SilentlyContinue }";
                RunCli("powershell.exe", $"-NoProfile -NonInteractive -Command \"{psDns}\"", 10000);

                // DoH (DNS over HTTPS) chỉ được hỗ trợ từ Windows 11 (Build 22000+)
                if (enableDoH && Environment.OSVersion.Version.Build >= 22000)
                {
                    RunCli("netsh.exe", "dns add encryption server=1.1.1.1 dohtemplate=https://cloudflare-dns.com/dns-query autoupgrade=yes udpfallback=no", 5000);
                    RunCli("netsh.exe", "dns add encryption server=1.0.0.1 dohtemplate=https://cloudflare-dns.com/dns-query autoupgrade=yes udpfallback=no", 5000);
                    string psDoh = "Set-DnsClientDohServerAddress -ServerAddress '1.1.1.1' -DohTemplate 'https://cloudflare-dns.com/dns-query' -AllowFallbackToUdp $false -AutoUpgrade $true -ErrorAction SilentlyContinue; Set-DnsClientDohServerAddress -ServerAddress '1.0.0.1' -DohTemplate 'https://cloudflare-dns.com/dns-query' -AllowFallbackToUdp $false -AutoUpgrade $true -ErrorAction SilentlyContinue";
                    RunCli("powershell.exe", $"-NoProfile -NonInteractive -Command \"{psDoh}\"", 8000);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool ResetDnsToDefault()
        {
            try
            {
                string psReset = "Get-NetAdapter -Physical -ErrorAction SilentlyContinue | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ResetServerAddresses -ErrorAction SilentlyContinue }";
                RunCli("powershell.exe", $"-NoProfile -NonInteractive -Command \"{psReset}\"", 10000);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Registry Helpers
        public static bool SetDword(RegistryHive hive, string subKey, string valueName, int value)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                using var key = baseKey.CreateSubKey(subKey, true);
                key?.SetValue(valueName, value, RegistryValueKind.DWord);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool SetString(RegistryHive hive, string subKey, string valueName, string value)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                using var key = baseKey.CreateSubKey(subKey, true);
                key?.SetValue(valueName, value, RegistryValueKind.String);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool DeleteValue(RegistryHive hive, string subKey, string valueName)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                using var key = baseKey.OpenSubKey(subKey, true);
                if (key != null && key.GetValue(valueName) != null)
                {
                    key.DeleteValue(valueName, false);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool DeleteKey(RegistryHive hive, string subKey)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                baseKey.DeleteSubKeyTree(subKey, false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static int GetDword(RegistryHive hive, string subKey, string valueName, int defaultValue = -1)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                using var key = baseKey.OpenSubKey(subKey, false);
                if (key == null) return defaultValue;
                var val = key.GetValue(valueName);
                if (val is int intVal) return intVal;
                if (val is long longVal) return (int)longVal;
                if (int.TryParse(val?.ToString(), out int parsed)) return parsed;
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public static string GetString(RegistryHive hive, string subKey, string valueName, string defaultValue = "")
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                using var key = baseKey.OpenSubKey(subKey, false);
                return key?.GetValue(valueName)?.ToString() ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public static bool KeyExists(RegistryHive hive, string subKey)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                using var key = baseKey.OpenSubKey(subKey, false);
                return key != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool ValueExists(RegistryHive hive, string subKey, string valueName)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                using var key = baseKey.OpenSubKey(subKey, false);
                return key?.GetValue(valueName) != null;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Service & Command Helpers
        public static bool RunCli(string fileName, string arguments, int timeoutMs = 25000)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    // Không redirect stream để triệt tiêu hoàn toàn nguy cơ deadlock bộ đệm pipe 4KB của Windows
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                using var proc = Process.Start(psi);
                if (proc == null) return false;
                bool finished = proc.WaitForExit(timeoutMs);
                if (!finished)
                {
                    try { proc.Kill(true); } catch { }
                    return false;
                }
                return proc.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public static void SetServiceStartup(string serviceName, string startupType)
        {
            // Bảo vệ tuyệt đối Riot Vanguard: Không bao giờ đụng đến vgc hoặc vgk
            if (serviceName.Equals("vgc", StringComparison.OrdinalIgnoreCase) ||
                serviceName.Equals("vgk", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Xử lý riêng Per-User Service (tránh lỗi 1060 The specified service does not exist)
            if (serviceName.StartsWith("BcastDVRUserService", StringComparison.OrdinalIgnoreCase))
            {
                int startVal = startupType.Equals("disabled", StringComparison.OrdinalIgnoreCase) ? 4 : 3;
                SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\BcastDVRUserService", "Start", startVal);
                return;
            }

            RunCli("sc.exe", $"config {serviceName} start= {startupType}");
        }

        public static void StopService(string serviceName)
        {
            if (serviceName.Equals("vgc", StringComparison.OrdinalIgnoreCase) ||
                serviceName.Equals("vgk", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            RunCli("sc.exe", $"stop {serviceName}");
        }

        public static void StartService(string serviceName)
        {
            RunCli("sc.exe", $"start {serviceName}");
        }

        public static void SetScheduledTaskState(string taskPath, bool enable)
        {
            string flag = enable ? "/Enable" : "/Disable";
            RunCli("schtasks.exe", $"/Change /TN \"{taskPath}\" {flag}");
        }
        #endregion
    }
}
