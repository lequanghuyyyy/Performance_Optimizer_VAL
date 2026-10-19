using System.Diagnostics;
using System.Text;

namespace PerformanceOptimizer.Services
{
    public static class PowerShellRunner
    {
        public static async Task<bool> ExecuteScriptAsync(string script, Action<string>? onStatusUpdate = null, int timeoutMs = 60000)
        {
            if (string.IsNullOrWhiteSpace(script)) return true;

            return await Task.Run(() =>
            {
                try
                {
                    // Mã hóa base64 để tránh lỗi ký tự đặc biệt hoặc escape
                    byte[] bytes = Encoding.Unicode.GetBytes(script);
                    string base64 = Convert.ToBase64String(bytes);

                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {base64}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = new Process { StartInfo = psi };
                    process.OutputDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            onStatusUpdate?.Invoke(e.Data.Trim());
                        }
                    };
                    process.ErrorDataReceived += (s, e) => { };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    bool finished = process.WaitForExit(timeoutMs);
                    if (!finished)
                    {
                        try { process.Kill(true); } catch { }
                        return false;
                    }
                    return process.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
