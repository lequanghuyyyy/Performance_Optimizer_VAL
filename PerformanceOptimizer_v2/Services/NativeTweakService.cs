using System.Diagnostics;
using Microsoft.Win32;

namespace PerformanceOptimizer.Services
{
    public static class NativeTweakService
    {
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
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
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
            RunCli("sc.exe", $"config {serviceName} start= {startupType}");
        }

        public static void StopService(string serviceName)
        {
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
