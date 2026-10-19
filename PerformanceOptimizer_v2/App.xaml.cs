using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace PerformanceOptimizer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!IsAdministrator())
            {
                try
                {
                    var exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = exePath,
                            UseShellExecute = true,
                            Verb = "runas"
                        };
                        Process.Start(startInfo);
                        Shutdown();
                        return;
                    }
                }
                catch
                {
                    // User canceled UAC prompt
                }
            }
        }

        private static bool IsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}
