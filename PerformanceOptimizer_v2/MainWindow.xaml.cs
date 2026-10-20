using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using PerformanceOptimizer.ViewModels;

namespace PerformanceOptimizer
{
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            try
            {
                SetCurrentProcessExplicitAppUserModelID("Windows11.PerformanceStudio.Pro");
            }
            catch { }

            InitializeComponent();
            ViewModel = new MainViewModel();
            DataContext = ViewModel;

            Loaded += (s, e) => ViewModel.UpdateLayoutForWidth(ActualWidth);
            SizeChanged += (s, e) => ViewModel.UpdateLayoutForWidth(e.NewSize.Width);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                var handle = new WindowInteropHelper(this).Handle;
                if (handle != IntPtr.Zero)
                {
                    int darkMode = 1;
                    if (DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int)) != 0)
                    {
                        DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkMode, sizeof(int));
                    }

                    // Windows 11 Build 22000+: Exact dark caption background matching #0B0F19 (COLORREF 0x00BBGGRR -> 0x00190F0B)
                    int captionColor = 0x00190F0B; 
                    DwmSetWindowAttribute(handle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

                    // Title bar text color: White
                    int textColor = 0x00FFFFFF;
                    DwmSetWindowAttribute(handle, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
                }
            }
            catch { }
        }

        private void BtnOpenSpecs_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenSpecsPopup();
        }

        private void BtnCloseSpecs_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CloseSpecsPopup();
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string category)
            {
                ViewModel.FilterByCategory(category);
            }
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectAll();
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DeselectAll();
        }

        private async void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ApplySelectedAsync();
        }

        private async void BtnRevertSingleItem_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is FrameworkElement fe && fe.DataContext is PerformanceOptimizer.Models.OptimizationItem item)
            {
                await ViewModel.RevertSingleItemAsync(item);
            }
        }

        private async void BtnRescan_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ScanSystemStateAsync();
        }

        private async void BtnQuickRam_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.PurgeRamInstantlyAsync();
        }

        private async void BtnToggleFpsBoost_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ToggleFpsBoostAsync();
        }

        private void BtnToggleVanguardSafe_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleVanguardSafeMode();
        }

        private void Card_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is PerformanceOptimizer.Models.OptimizationItem item)
            {
                item.IsSelected = !item.IsSelected;
                e.Handled = true;
            }
        }
    }
}
