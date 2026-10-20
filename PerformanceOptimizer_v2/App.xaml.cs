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

            // Bắt toàn bộ lỗi ngoại lệ không mong muốn, bảo đảm ứng dụng không bao giờ bị crash đột ngột
            DispatcherUnhandledException += (s, args) =>
            {
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                // Ghi nhận lỗi và ngăn crash nếu có thể
            };

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                args.SetObserved();
            };

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
                    MessageBox.Show(
                        "Windows Performance Studio cần quyền Quản trị viên (Administrator) để tối ưu hóa hệ thống và bảo đảm an toàn cho các tiến trình game.\n\nỨng dụng sẽ tự động đóng.",
                        "Yêu Cầu Quyền Quản Trị Viên",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    Shutdown();
                    return;
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
