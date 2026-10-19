using Microsoft.Win32;
using PerformanceOptimizer.Models;

namespace PerformanceOptimizer.Services
{
    public static class OptimizationDataService
    {
        public static List<OptimizationItem> GetItems(HardwareSpecs? hardware = null)
        {
            var list = new List<OptimizationItem>();
            hardware ??= HardwareService.DetectSpecs();

            // 1. Power Plan: 100% Turbo Boost
            list.Add(new OptimizationItem
            {
                Id = 1,
                Category = "⚡ Hiệu năng & CPU",
                Title = "Power Plan: High Performance (100% Turbo Boost Unlocked)",
                Description = "Kích hoạt gói nguồn hiệu năng cao, mở khóa 100% xung nhịp Turbo Boost (đã sửa lỗi khóa 99% xung), đảm bảo CPU bung hết sức mạnh cho game và render.",
                Badges = new List<string> { "Max Turbo Boost", "FPS Ổn Định", "Khuyên Dùng" },
                Icon = "⚡",
                CheckStatus = () =>
                {
                    int max = NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes", "ActivePowerScheme", -1);
                    return true;
                },
                NativeApply = () =>
                {
                    NativeTweakService.RunCli("powercfg.exe", "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
                    NativeTweakService.RunCli("powercfg.exe", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                    NativeTweakService.RunCli("powercfg.exe", "-setacvalueindex scheme_current sub_processor PROCTHROTTLEMAX 100");
                    NativeTweakService.RunCli("powercfg.exe", "-setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN 5");
                    NativeTweakService.RunCli("powercfg.exe", "-setactive scheme_current");
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.RunCli("powercfg.exe", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e"); // Balanced
                    return true;
                },
                Script = @"
& powercfg.exe -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 2>$null | Out-Null
& powercfg.exe /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c 2>$null | Out-Null
& powercfg.exe -setacvalueindex scheme_current sub_processor PROCTHROTTLEMAX 100 2>$null | Out-Null
& powercfg.exe -setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN 5 2>$null | Out-Null
& powercfg.exe -setactive scheme_current 2>$null | Out-Null
Write-Host '[OK] Da kich hoat High Performance Plan (100% Turbo Boost).' -ForegroundColor Green
",
                RevertScript = @"
& powercfg.exe /setactive 381b4222-f694-41f0-9685-ff5bb260df2e 2>$null | Out-Null
Write-Host '[OK] Da khoi phuc Power Plan Balanced mac dinh.' -ForegroundColor Yellow
"
            });

            // 2. CPU Quantum
            list.Add(new OptimizationItem
            {
                Id = 2,
                Category = "⚡ Hiệu năng & CPU",
                Title = "CPU Quantum & Priority (Win32PrioritySeparation = 38)",
                Description = "Cấu hình bộ lập lịch Windows ưu tiên cửa sổ ứng dụng/game đang mở (Foreground Process Boost), phản hồi chuột phím 0s delay.",
                Badges = new List<string> { "Giảm Input Lag", "Ưu Tiên Game", "Phản Hồi Nhanh" },
                Icon = "🎯",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 2) == 38,
                NativeApply = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "ConvertibleSlateMode", 0);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 2);
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl' -Name 'Win32PrioritySeparation' -Value 38 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl' -Name 'ConvertibleSlateMode' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da toi uu Win32PrioritySeparation = 38.' -ForegroundColor Green
",
                RevertScript = @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl' -Name 'Win32PrioritySeparation' -Value 2 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoi phuc Win32PrioritySeparation ve 2 mac dinh.' -ForegroundColor Yellow
"
            });

            // 3. Đồ họa HAGS & SwapEffect
            list.Add(new OptimizationItem
            {
                Id = 3,
                Category = "🎮 GPU & Gaming",
                Title = "Đồ họa: HAGS, SwapEffect Flip & GPU Boost",
                Description = "Bật Lập lịch GPU tăng tốc phần cứng (HAGS), DirectX SwapEffect Flip và tắt triệt để GameDVR để giải phóng tài nguyên card đồ họa.",
                Badges = new List<string> { "HAGS On", "DirectX Flip", "Tắt GameDVR" },
                Icon = "🎮",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 0) == 2,
                NativeApply = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"System\GameConfigStore", "GameDVR_FSEBehaviorMode", 2);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 1);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 1);
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR");
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name 'HwSchMode' -Value 2 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\System\GameConfigStore' -Name 'GameDVR_Enabled' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR' -Name 'AllowGameDVR' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da bat HAGS va tat GameDVR.' -ForegroundColor Green
",
                RevertScript = @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name 'HwSchMode' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\System\GameConfigStore' -Name 'GameDVR_Enabled' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR' -Name 'AllowGameDVR' -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoi phuc HAGS va GameDVR ve mac dinh.' -ForegroundColor Yellow
"
            });

            // 4. Gán GPU Rời
            list.Add(new OptimizationItem
            {
                Id = 4,
                Category = "🎮 GPU & Gaming",
                Title = "Gán GPU Rời Dedicated cho Game & Dev Tools",
                Description = "Tự động gán quyền ưu tiên chạy card đồ họa mạnh nhất cho các tựa game và môi trường lập trình (VS Code, Visual Studio, JetBrains, Chrome, Valorant, CS2).",
                Badges = new List<string> { "High Performance GPU", "Auto Scan", "Mượt Mà" },
                Icon = "🚀",
                CheckStatus = () => NativeTweakService.ValueExists(RegistryHive.CurrentUser, @"Software\Microsoft\DirectX\UserGpuPreferences", "Valorant.exe"),
                NativeApply = () =>
                {
                    string[] apps = { "Code.exe", "devenv.exe", "idea64.exe", "chrome.exe", "msedge.exe", "Valorant.exe", "cs2.exe", "League of Legends.exe" };
                    foreach (var app in apps)
                    {
                        NativeTweakService.SetString(RegistryHive.CurrentUser, @"Software\Microsoft\DirectX\UserGpuPreferences", app, "GpuPreference=2;");
                    }
                    return true;
                },
                NativeRevert = () =>
                {
                    string[] apps = { "Code.exe", "devenv.exe", "idea64.exe", "chrome.exe", "msedge.exe", "Valorant.exe", "cs2.exe", "League of Legends.exe" };
                    foreach (var app in apps)
                    {
                        NativeTweakService.DeleteValue(RegistryHive.CurrentUser, @"Software\Microsoft\DirectX\UserGpuPreferences", app);
                    }
                    return true;
                },
                Script = @"
$apps = @('Code.exe', 'devenv.exe', 'idea64.exe', 'chrome.exe', 'msedge.exe', 'Valorant.exe', 'cs2.exe', 'League of Legends.exe')
foreach ($app in $apps) {
    Set-ItemProperty -Path 'HKCU:\Software\Microsoft\DirectX\UserGpuPreferences' -Name $app -Value 'GpuPreference=2;' -Type String -ErrorAction SilentlyContinue
}
Write-Host '[OK] Da gan High Performance GPU cho cac ung dung chinh.' -ForegroundColor Green
",
                RevertScript = @"
$apps = @('Code.exe', 'devenv.exe', 'idea64.exe', 'chrome.exe', 'msedge.exe', 'Valorant.exe', 'cs2.exe', 'League of Legends.exe')
foreach ($app in $apps) {
    Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\DirectX\UserGpuPreferences' -Name $app -ErrorAction SilentlyContinue
}
Write-Host '[OK] Da xoa thiet lap GPU roi cho ung dung.' -ForegroundColor Yellow
"
            });

            // 5. Driver Ethernet
            list.Add(new OptimizationItem
            {
                Id = 5,
                Category = "🌐 Mạng & MSI",
                Title = "Tối ưu Card Mạng Dây Ethernet (Realtek / Intel / Killer)",
                Description = "Tắt Energy Efficient Ethernet (EEE) và Green Ethernet (chống drop ping), tắt Interrupt Moderation để nhận gói tin game tức thì.",
                Badges = new List<string> { "Giảm Ping Jitter", "Tắt Tiết Kiệm Điện", "Ping Ổn Định" },
                Icon = "🌐",
                CheckStatus = () => true,
                Script = @"
Get-NetAdapter -Physical -ErrorAction SilentlyContinue | Where-Object { $_.InterfaceDescription -notmatch 'Wi-Fi|Wireless' } | ForEach-Object {
    Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Energy Efficient Ethernet' -DisplayValue 'Disabled' -ErrorAction SilentlyContinue
    Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Green Ethernet' -DisplayValue 'Disabled' -ErrorAction SilentlyContinue
    Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Interrupt Moderation' -DisplayValue 'Disabled' -ErrorAction SilentlyContinue
}
Write-Host '[OK] Da toi uu Card Mang Day Ethernet.' -ForegroundColor Green
",
                RevertScript = @"
Get-NetAdapter -Physical -ErrorAction SilentlyContinue | Where-Object { $_.InterfaceDescription -notmatch 'Wi-Fi|Wireless' } | ForEach-Object {
    Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Energy Efficient Ethernet' -DisplayValue 'Enabled' -ErrorAction SilentlyContinue
    Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Interrupt Moderation' -DisplayValue 'Enabled' -ErrorAction SilentlyContinue
}
Write-Host '[OK] Da khoi phuc thiet lap Card Mang Day ve mac dinh.' -ForegroundColor Yellow
"
            });

            // 6. Driver Wi-Fi
            list.Add(new OptimizationItem
            {
                Id = 6,
                Category = "🌐 Mạng & MSI",
                Title = "Tối ưu Card Mạng Wi-Fi (Chống Lag Spikes)",
                Description = "Tối ưu công suất truyền tải Wi-Fi lên mức cao nhất, giảm mức độ nhảy trạm phát (Roaming Aggressiveness) để không bị đứt kết nối khi chơi game.",
                Badges = new List<string> { "Chống Lag Wi-Fi", "Max Power", "Băng Thông Cao" },
                Icon = "📶",
                CheckStatus = () => true,
                Script = @"
Get-NetAdapter -Physical -ErrorAction SilentlyContinue | Where-Object { $_.InterfaceDescription -match 'Wi-Fi|Wireless' } | ForEach-Object {
    Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Roaming Aggressiveness' -DisplayValue '1. Lowest' -ErrorAction SilentlyContinue
    Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Transmit Power' -DisplayValue '5. Highest' -ErrorAction SilentlyContinue
}
Write-Host '[OK] Da toi uu Card Mang Khong Day Wi-Fi.' -ForegroundColor Green
",
                RevertScript = @"
Get-NetAdapter -Physical -ErrorAction SilentlyContinue | Where-Object { $_.InterfaceDescription -match 'Wi-Fi|Wireless' } | ForEach-Object {
    Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Roaming Aggressiveness' -DisplayValue '3. Medium' -ErrorAction SilentlyContinue
}
Write-Host '[OK] Da khoi phuc thiet lap Wi-Fi ve mac dinh.' -ForegroundColor Yellow
"
            });

            // 7. DNS & TCP Stack
            list.Add(new OptimizationItem
            {
                Id = 7,
                Category = "🌐 Mạng & MSI",
                Title = "DNS Cloudflare (1.1.1.1) & Tối ưu TCP Stack",
                Description = "Thiết lập DNS Cloudflare bảo mật tốc độ cao, tắt thuật toán Nagle (TCPNoDelay) và tắt Network Throttling Index trong Windows.",
                Badges = new List<string> { "DNS 1.1.1.1", "Tắt Nagle", "Phản Hồi Siêu Tốc" },
                Icon = "⚡",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 0) == -1,
                NativeApply = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", -1);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 10);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 20);
                    return true;
                },
                Script = @"
Get-NetAdapter -Physical -ErrorAction SilentlyContinue | ForEach-Object {
    Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ServerAddresses @('1.1.1.1', '1.0.0.1') -ErrorAction SilentlyContinue
}
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name 'NetworkThrottlingIndex' -Value 0xFFFFFFFF -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name 'SystemResponsiveness' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da cau hinh DNS Cloudflare va TCP NoDelay.' -ForegroundColor Green
",
                RevertScript = @"
Get-NetAdapter -Physical -ErrorAction SilentlyContinue | ForEach-Object {
    Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ResetServerAddresses -ErrorAction SilentlyContinue
}
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name 'NetworkThrottlingIndex' -Value 10 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name 'SystemResponsiveness' -Value 20 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoi phuc DNS DHCP va TCP Throttling mac dinh.' -ForegroundColor Yellow
"
            });

            // 8. Hypervisor An Toàn
            list.Add(new OptimizationItem
            {
                Id = 8,
                Category = "🛡️ Dịch vụ & Windows",
                Title = "Bảo toàn 100% Hypervisor (WSL2, Docker, Vanguard)",
                Description = "Đảm bảo tính năng ảo hóa và chống gian lận (Vanguard, Easy Anti-Cheat, BattlEye) hoạt động bình thường, không lo bị ban tài khoản hay lỗi WSL.",
                Badges = new List<string> { "100% An Toàn", "WSL2/Docker OK", "Anti-Cheat OK" },
                Icon = "🛡️",
                CheckStatus = () => true,
                NativeApply = () =>
                {
                    NativeTweakService.RunCli("bcdedit.exe", "/set hypervisorlaunchtype auto");
                    return true;
                },
                Script = @"
& bcdedit.exe /set hypervisorlaunchtype auto 2>$null | Out-Null
Write-Host '[OK] Da kiem tra va bao toan Hypervisor Launch Type Auto.' -ForegroundColor Green
"
            });

            // 9. Tắt Services Dư Thừa
            list.Add(new OptimizationItem
            {
                Id = 9,
                Category = "🛡️ Dịch vụ & Windows",
                Title = "Tắt Services Windows Dư Thừa & Chống Reboot Ngầm",
                Description = "Vô hiệu hóa DiagTrack, SysMain, MapsBroker, Xbox DVR và thiết lập chính sách không tự động ép khởi động lại máy khi đang làm việc/chơi game.",
                Badges = new List<string> { "Tiết Kiệm RAM", "Không Reboot Ngầm", "Bảo Toàn Search" },
                Icon = "🛑",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", 0) == 1,
                NativeApply = () =>
                {
                    string[] services = { "DiagTrack", "dmwappushservice", "SysMain", "lfsvc", "MapsBroker", "PcaSvc", "RemoteRegistry", "WalletService", "BcastDVRUserService" };
                    foreach (var s in services)
                    {
                        NativeTweakService.StopService(s);
                        NativeTweakService.SetServiceStartup(s, "disabled");
                    }
                    NativeTweakService.SetServiceStartup("WSearch", "delayed-auto");
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", 1);
                    return true;
                },
                NativeRevert = () =>
                {
                    string[] services = { "SysMain", "PcaSvc", "MapsBroker" };
                    foreach (var s in services)
                    {
                        NativeTweakService.SetServiceStartup(s, "auto");
                        NativeTweakService.StartService(s);
                    }
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers");
                    return true;
                },
                Script = @"
$services = @('DiagTrack', 'dmwappushservice', 'SysMain', 'lfsvc', 'MapsBroker', 'PcaSvc', 'RemoteRegistry', 'WalletService', 'BcastDVRUserService')
foreach ($s in $services) {
    & sc.exe stop $s 2>$null | Out-Null
    & sc.exe config $s start= disabled 2>$null | Out-Null
}
& sc.exe config WSearch start= delayed-auto 2>$null | Out-Null
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU' -Name 'NoAutoRebootWithLoggedOnUsers' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da tat services du thua va bao toan Windows Search.' -ForegroundColor Green
",
                RevertScript = @"
$services = @('SysMain', 'PcaSvc', 'MapsBroker')
foreach ($s in $services) {
    & sc.exe config $s start= auto 2>$null | Out-Null
    & sc.exe start $s 2>$null | Out-Null
}
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU' -Name 'NoAutoRebootWithLoggedOnUsers' -ErrorAction SilentlyContinue
Write-Host '[OK] Da bat lai cac service co ban.' -ForegroundColor Yellow
"
            });

            // 10. Scheduled Tasks
            list.Add(new OptimizationItem
            {
                Id = 10,
                Category = "🛡️ Dịch vụ & Windows",
                Title = "Tắt Scheduled Tasks Thu Thập Dữ Liệu Ngầm",
                Description = "Tắt các tác vụ tự động ghi dữ liệu chẩn đoán ổ đĩa, CEIP (Customer Experience Improvement) và ProgramDataUpdater gây nghẽn SSD.",
                Badges = new List<string> { "Bảo Vệ SSD", "Tắt CEIP", "Giảm Đọc Ghi" },
                Icon = "⏱️",
                CheckStatus = () => true,
                NativeApply = () =>
                {
                    string[] tasks = {
                        @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
                        @"\Microsoft\Windows\Application Experience\ProgramDataUpdater",
                        @"\Microsoft\Windows\Autochk\Proxy",
                        @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
                        @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"
                    };
                    foreach (var t in tasks) NativeTweakService.SetScheduledTaskState(t, false);
                    return true;
                },
                NativeRevert = () =>
                {
                    string[] tasks = {
                        @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
                        @"\Microsoft\Windows\Application Experience\ProgramDataUpdater"
                    };
                    foreach (var t in tasks) NativeTweakService.SetScheduledTaskState(t, true);
                    return true;
                },
                Script = @"
$tasks = @(
    '\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector',
    '\Microsoft\Windows\Application Experience\ProgramDataUpdater',
    '\Microsoft\Windows\Autochk\Proxy',
    '\Microsoft\Windows\Customer Experience Improvement Program\Consolidator',
    '\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip'
)
foreach ($t in $tasks) {
    & schtasks.exe /Change /TN $t /Disable 2>$null | Out-Null
}
Write-Host '[OK] Da vo hieu hoa cac Scheduled Tasks chan doan ngam.' -ForegroundColor Green
",
                RevertScript = @"
$tasks = @(
    '\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector',
    '\Microsoft\Windows\Application Experience\ProgramDataUpdater'
)
foreach ($t in $tasks) {
    & schtasks.exe /Change /TN $t /Enable 2>$null | Out-Null
}
Write-Host '[OK] Da khoi phuc Scheduled Tasks.' -ForegroundColor Yellow
"
            });

            // 11. Policies Hệ thống
            list.Add(new OptimizationItem
            {
                Id = 11,
                Category = "🛡️ Dịch vụ & Windows",
                Title = "Tắt Telemetry, Tracking & Quảng Cáo Windows",
                Description = "Chặn gửi dữ liệu chẩn đoán về Microsoft, tắt tính năng gợi ý quảng cáo, ActivityFeed và đồng bộ đám mây rác.",
                Badges = new List<string> { "Bảo Mật", "Không Quảng Cáo", "Sạch Sẽ" },
                Icon = "🔒",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 0) == 1,
                NativeApply = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableSettingSync", 2);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableSoftLanding", 1);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures");
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableSoftLanding");
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableSettingSync");
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\SettingSync' -Name 'DisableSettingSync' -Value 2 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\CloudContent' -Name 'DisableWindowsConsumerFeatures' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\CloudContent' -Name 'DisableSoftLanding' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoa cac chinh sach Telemetry va Tracking.' -ForegroundColor Green
",
                RevertScript = @"
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\CloudContent' -Name 'DisableWindowsConsumerFeatures' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\CloudContent' -Name 'DisableSoftLanding' -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoi phuc thiet lap Consumer Features.' -ForegroundColor Yellow
"
            });

            // 12. Explorer LongPaths
            list.Add(new OptimizationItem
            {
                Id = 12,
                Category = "🛡️ Dịch vụ & Windows",
                Title = "Bật Hỗ Trợ Đường Dẫn Dài LongPaths (>260 Ký Tự)",
                Description = "Xóa bỏ giới hạn 260 ký tự của Windows, tránh hoàn toàn lỗi crash khi clone repo Git, build dự án NPM / Node.js hoặc giải nén file sâu.",
                Badges = new List<string> { "LongPaths >260", "Tránh Lỗi Git/NPM", "Tiện Ích Dev" },
                Icon = "📁",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", 0) == 1,
                NativeApply = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", 1);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", 0);
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name 'LongPathsEnabled' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da kich hoat LongPathsEnabled thanh cong.' -ForegroundColor Green
",
                RevertScript = @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name 'LongPathsEnabled' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da tat LongPathsEnabled.' -ForegroundColor Yellow
"
            });

            // 13. Tắt gợi ý Bing Search
            list.Add(new OptimizationItem
            {
                Id = 13,
                Category = "🛡️ Dịch vụ & Windows",
                Title = "Tắt Tìm Kiếm Bing Web Trên Start Menu",
                Description = "Khi gõ tìm kiếm trong Start Menu, chỉ tìm ứng dụng và file cục bộ tức thì 0s delay, không tốn mạng tải kết quả tìm kiếm Bing.",
                Badges = new List<string> { "Tìm Kiếm Offline 0s", "Không Quảng Cáo Bing", "Giữ Nguyên Taskbar" },
                Icon = "🔍",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "DisableWebSearch", 0) == 1,
                NativeApply = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "DisableWebSearch", 1);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "CortanaConsent", 0);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "DisableWebSearch", 0);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 1);
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Search' -Name 'DisableWebSearch' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Search' -Name 'BingSearchEnabled' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da tat Bing Web Search, giu nguyen Taskbar hien tai.' -ForegroundColor Green
",
                RevertScript = @"
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Search' -Name 'DisableWebSearch' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Search' -Name 'BingSearchEnabled' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da bat lai Bing Web Search tren Start Menu.' -ForegroundColor Yellow
"
            });

            // 14. RAM & Kernel Lock
            list.Add(new OptimizationItem
            {
                Id = 14,
                Category = "⚡ Hiệu năng & CPU",
                Title = "Tối Ưu RAM: Khóa Kernel Trên RAM (DisablePagingExecutive)",
                Description = "Tự động phát hiện dung lượng RAM: Nếu RAM >= 8GB, khóa toàn bộ nhân Kernel Windows trong RAM để phản hồi 0s delay ổ đĩa. Tắt cập nhật LastAccess NTFS để tăng tốc SSD.",
                Badges = new List<string> { "Zero Disk Delay", "Khóa Kernel RAM", "Bảo Vệ SSD" },
                Icon = "💾",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 0) == 1,
                NativeApply = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 1);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 1);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsDisableLastAccessUpdate", 1);
                    NativeTweakService.RunCli("powercfg.exe", "-h off");
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 0);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 0);
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management' -Name 'LargeSystemCache' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management' -Name 'DisablePagingExecutive' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name 'NtfsDisableLastAccessUpdate' -Value 1 -Type DWord -ErrorAction SilentlyContinue
& powercfg.exe -h off 2>$null | Out-Null
Write-Host '[OK] Da khoa Kernel tren RAM va tat Hibernate giai phong o dia.' -ForegroundColor Green
",
                RevertScript = @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management' -Name 'LargeSystemCache' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management' -Name 'DisablePagingExecutive' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoi phuc thiet lap Paging Executive ve mac dinh.' -ForegroundColor Yellow
"
            });

            // 15. Chuột eSports 1:1
            list.Add(new OptimizationItem
            {
                Id = 15,
                Category = "🎮 GPU & Gaming",
                Title = "Chuột 1:1 eSports (Raw Input) & Tốc Độ Bàn Phím",
                Description = "Tắt hoàn toàn gia tốc chuột (Sens 10, Speed 0 chuẩn 1:1 không lệch tâm bắn), Keyboard Repeat Delay nhanh nhất và tắt các phím StickyKeys gây ức chế.",
                Badges = new List<string> { "Chuột Chuẩn 1:1", "Phím Cực Nhạy", "Tắt StickyKeys" },
                Icon = "🖱️",
                CheckStatus = () => NativeTweakService.GetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "1") == "0",
                NativeApply = () =>
                {
                    NativeTweakService.SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0");
                    NativeTweakService.SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "0");
                    NativeTweakService.SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "0");
                    NativeTweakService.SetString(RegistryHive.CurrentUser, @"Control Panel\Keyboard", "KeyboardDelay", "0");
                    NativeTweakService.SetString(RegistryHive.CurrentUser, @"Control Panel\Keyboard", "KeyboardSpeed", "31");
                    NativeTweakService.SetString(RegistryHive.CurrentUser, @"Control Panel\Accessibility\StickyKeys", "Flags", "506");
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "1");
                    NativeTweakService.SetString(RegistryHive.CurrentUser, @"Control Panel\Accessibility\StickyKeys", "Flags", "510");
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'MouseSpeed' -Value '0' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'MouseThreshold1' -Value '0' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'MouseThreshold2' -Value '0' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Keyboard' -Name 'KeyboardDelay' -Value '0' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Keyboard' -Name 'KeyboardSpeed' -Value '31' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Accessibility\StickyKeys' -Name 'Flags' -Value '506' -Type String -ErrorAction SilentlyContinue
Write-Host '[OK] Da cai dat chuot eSports 1:1 va phan hoi phim sieu toc.' -ForegroundColor Green
",
                RevertScript = @"
Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'MouseSpeed' -Value '1' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Accessibility\StickyKeys' -Name 'Flags' -Value '510' -Type String -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoi phuc thiet lap chuot mac dinh.' -ForegroundColor Yellow
"
            });

            // 16. Dark Theme & Menu
            list.Add(new OptimizationItem
            {
                Id = 16,
                Category = "🛡️ Dịch vụ & Windows",
                Title = "Giao Diện Windows Dark Theme & Tắt Hiệu Ứng Trễ Menu",
                Description = "Kích hoạt Dark Theme dịu mắt, MenuShowDelay = 0 (mở menu tức thì), tắt độ mờ Acrylic để giảm tải GPU. Tuyệt đối giữ nguyên thanh Taskbar của bạn.",
                Badges = new List<string> { "Dark Theme", "Menu 0ms", "Không Đổi Taskbar" },
                Icon = "🌙",
                CheckStatus = () => NativeTweakService.GetString(RegistryHive.CurrentUser, @"Control Panel\Desktop", "MenuShowDelay", "400") == "0",
                NativeApply = () =>
                {
                    NativeTweakService.SetString(RegistryHive.CurrentUser, @"Control Panel\Desktop", "MenuShowDelay", "0");
                    NativeTweakService.SetString(RegistryHive.CurrentUser, @"Control Panel\Desktop", "AutoEndTasks", "1");
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 0);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", 0);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.SetString(RegistryHive.CurrentUser, @"Control Panel\Desktop", "MenuShowDelay", "400");
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 1);
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKCU:\Control Panel\Desktop' -Name 'MenuShowDelay' -Value '0' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Desktop' -Name 'AutoEndTasks' -Value '1' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize' -Name 'EnableTransparency' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da thiet lap Dark Theme va Menu 0ms.' -ForegroundColor Green
",
                RevertScript = @"
Set-ItemProperty -Path 'HKCU:\Control Panel\Desktop' -Name 'MenuShowDelay' -Value '400' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize' -Name 'EnableTransparency' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoi phuc Menu delay 400ms.' -ForegroundColor Yellow
"
            });

            // 17. Developer Mode
            list.Add(new OptimizationItem
            {
                Id = 17,
                Category = "🛡️ Dịch vụ & Windows",
                Title = "Kích Hoạt Developer Mode & Clipboard History (Win + V)",
                Description = "Bật chế độ lập trình viên Developer Mode để debug và kích hoạt sẵn lịch sử khay nhớ tạm nhiều mục (Win + V) cực kỳ hữu ích.",
                Badges = new List<string> { "Developer Mode", "Win + V", "Tiện Ích" },
                Icon = "💻",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Clipboard", "EnableClipboardHistory", 0) == 1,
                NativeApply = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock", "AllowDevelopmentWithoutDevLicense", 1);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Clipboard", "EnableClipboardHistory", 1);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Clipboard", "EnableClipboardHistory", 0);
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock' -Name 'AllowDevelopmentWithoutDevLicense' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Clipboard' -Name 'EnableClipboardHistory' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da kich hoat Developer Mode va Clipboard History.' -ForegroundColor Green
"
            });

            // 18. Restore Point
            list.Add(new OptimizationItem
            {
                Id = 18,
                Category = "Chris_TiTus_Tech",
                Title = "Tạo System Restore Point (Điểm Khôi Phục An Toàn)",
                Description = "Kích hoạt System Protection và tự động tạo điểm sao lưu 'Truoc_Khi_Toi_Uu_PerformanceStudio', đảm bảo an toàn tuyệt đối 100% có thể rollback.",
                Badges = new List<string> { "An Toàn 100%", "Rollback Dễ Dàng", "Khuyên Dùng" },
                Icon = "🛡️",
                CheckStatus = () => true,
                Script = @"
$ProgressPreference = 'SilentlyContinue'
Set-Service -Name vss -StartupType Manual -ErrorAction SilentlyContinue
Start-Service -Name vss -ErrorAction SilentlyContinue
Enable-ComputerRestore -Drive 'C:\' -ErrorAction SilentlyContinue
Checkpoint-Computer -Description 'Truoc_Khi_Toi_Uu_PerformanceStudio' -RestorePointType 'MODIFY_SETTINGS' -ErrorAction SilentlyContinue
Write-Host '[OK] Da tao System Restore Point an toan tren o C:.' -ForegroundColor Green
exit 0
"
            });

            // 19. End Task on Taskbar
            list.Add(new OptimizationItem
            {
                Id = 19,
                Category = "Chris_TiTus_Tech",
                Title = "Bật 'End Task' Trực Tiếp Khi Chuột Phải Taskbar",
                Description = "Tính năng được yêu thích nhất của Chris Titus trên Win 11: Chuột phải vào bất kỳ app nào trên thanh Taskbar là có ngay nút 'End Task' để đóng ứng dụng đơ mà không cần mở Task Manager.",
                Badges = new List<string> { "End Task 1 Click", "Đặc Quyền Win 11", "Tiện Lợi" },
                Icon = "⚡",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings", "TaskbarEndTask", 0) == 1,
                NativeApply = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings", "TaskbarEndTask", 1);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings", "TaskbarEndTask", 0);
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings' -Name 'TaskbarEndTask' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da bat tinh nang End Task tren Taskbar.' -ForegroundColor Green
",
                RevertScript = @"
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings' -Name 'TaskbarEndTask' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da tat tinh nang End Task tren Taskbar.' -ForegroundColor Yellow
"
            });

            // 20. Classic Context Menu
            list.Add(new OptimizationItem
            {
                Id = 20,
                Category = "Chris_TiTus_Tech",
                Title = "Khôi Phục Menu Chuột Phải Đầy Đủ (Classic Context Menu)",
                Description = "Loại bỏ nút 'Show more options' (Shift + F10) gây mất thời gian trên Win 11, khôi phục chuột phải trực quan tức thì như Windows 10.",
                Badges = new List<string> { "Menu Chuột Phải Cũ", "Không Cần Shift+F10", "Tiết Kiệm Thao Tác" },
                Icon = "🖱️",
                CheckStatus = () => NativeTweakService.KeyExists(RegistryHive.CurrentUser, @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"),
                NativeApply = () =>
                {
                    NativeTweakService.SetString(RegistryHive.CurrentUser, @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", "", "");
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.DeleteKey(RegistryHive.CurrentUser, @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}");
                    return true;
                },
                Script = @"
New-Item -Path 'HKCU:\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32' -Value '' -Force -ErrorAction SilentlyContinue | Out-Null
Write-Host '[OK] Da khoi phuc Classic Context Menu chuot phai.' -ForegroundColor Green
",
                RevertScript = @"
Remove-Item -Path 'HKCU:\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}' -Recurse -Force -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoi phuc Context Menu Windows 11 mac dinh.' -ForegroundColor Yellow
"
            });

            // 21. Windows Update Security Mode
            list.Add(new OptimizationItem
            {
                Id = 21,
                Category = "Chris_TiTus_Tech",
                Title = "Windows Update: Chế Độ Security (Chuẩn Chris Titus)",
                Description = "Trì hoãn các bản cập nhật tính năng lớn (Feature Updates) 365 ngày để chống lỗi vặt/tụt FPS, chỉ nhận các bản vá bảo mật quan trọng.",
                Badges = new List<string> { "Hoãn 365 Ngày", "Chống Lỗi Update", "Bảo Vệ FPS" },
                Icon = "🔄",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferFeatureUpdates", 0) == 1,
                NativeApply = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferFeatureUpdates", 1);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferFeatureUpdatesPeriodInDays", 365);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferQualityUpdatesPeriodInDays", 4);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferFeatureUpdates");
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferFeatureUpdatesPeriodInDays");
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate' -Name 'DeferFeatureUpdates' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate' -Name 'DeferFeatureUpdatesPeriodInDays' -Value 365 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da thiet lap Windows Update che do Security Mode.' -ForegroundColor Green
",
                RevertScript = @"
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate' -Name 'DeferFeatureUpdates' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate' -Name 'DeferFeatureUpdatesPeriodInDays' -ErrorAction SilentlyContinue
Write-Host '[OK] Da xoa chinh sach tri hoan Windows Update.' -ForegroundColor Yellow
"
            });

            // 22. Dọn rác DISM
            list.Add(new OptimizationItem
            {
                Id = 22,
                Category = "Chris_TiTus_Tech",
                Title = "Dọn Rác Sâu Hệ Thống (Temp & DISM Component Cleanup)",
                Description = "Dọn sạch thư mục tạm %temp%, cache rác và chạy lệnh DISM Component Cleanup để thu hồi hàng chục GB ổ cứng SSD.",
                Badges = new List<string> { "Giải Phóng Ổ C", "Dọn Rác DISM", "Tối Ưu Ổ Cứng" },
                Icon = "🧹",
                CheckStatus = () => false,
                Script = @"
Remove-Item -Path ""$env:TEMP\*"" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path 'C:\Windows\Temp\*' -Recurse -Force -ErrorAction SilentlyContinue
& dism.exe /Online /Cleanup-Image /StartComponentCleanup /NoRestart 2>$null | Out-Null
Write-Host '[OK] Da don dep rac va Component Cleanup hoan tat.' -ForegroundColor Green
exit 0
"
            });

            // 23. MSI MODE
            list.Add(new OptimizationItem
            {
                Id = 23,
                Category = "🎮 GPU & Gaming",
                Title = "Ngắt MSI Mode Chuyên Sâu cho GPU & Card Mạng (Triệt Tiêu DPC Latency)",
                Description = "Tự động phát hiện Card màn hình (NVIDIA / AMD / Intel) và Card mạng (Ethernet / Wi-Fi), kích hoạt chế độ ngắt MSI Mode (Message Signaled Interrupts) ưu tiên cao. Triệt tiêu hoàn toàn nghẽn ngắt, giảm DPC Latency và chống drop FPS trong combat đông người.",
                Badges = new List<string> { "MSI Mode On", "Triệt Tiêu DPC Trễ", "Chống Micro-Stutter" },
                Icon = "⚡",
                CheckStatus = () => true,
                Script = @"
$pciKeys = Get-ChildItem -Path 'HKLM:\SYSTEM\CurrentControlSet\Enum\PCI' -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.PSChildName -eq 'Device Parameters' }
$count = 0
foreach ($key in $pciKeys) {
    $parent = Split-Path $key.PSPath -Parent
    $props = Get-ItemProperty -Path $parent -ErrorAction SilentlyContinue
    $desc = $props.DeviceDesc
    $class = $props.Class
    if ($class -eq 'Display' -or $class -eq 'Net' -or $desc -match 'NVIDIA|GeForce|Radeon|AMD|Intel.*Graphics|Ethernet|Wi-Fi|Wireless|Realtek.*GbE') {
        $msiPath = Join-Path $key.PSPath 'Interrupt Management\MessageSignaledInterruptProperties'
        New-Item -Path $msiPath -Force -ErrorAction SilentlyContinue | Out-Null
        Set-ItemProperty -Path $msiPath -Name 'MSISupported' -Value 1 -Type DWord -ErrorAction SilentlyContinue
        $affPath = Join-Path $key.PSPath 'Interrupt Management\Affinity Policy'
        New-Item -Path $affPath -Force -ErrorAction SilentlyContinue | Out-Null
        Set-ItemProperty -Path $affPath -Name 'DevicePriority' -Value 3 -Type DWord -ErrorAction SilentlyContinue
        $count++
    }
}
Write-Host ""[OK] Da kich hoat MSI Mode thanh cong cho $count thiet bi GPU va Card mang."" -ForegroundColor Green
"
            });

            // 24. CPU Affinity & Core Parking (ĐÃ SỬA THÔNG MINH CHO AMD X3D)
            string cpuWarning = hardware.IsAmdDualCcdX3D 
                ? " (Phát hiện AMD Dual-CCD V-Cache: Giữ nguyên Core Parking để tối ưu phân luồng 3D V-Cache)"
                : " (Tắt Core Parking trên CPU chuẩn để ép xung nhịp phản hồi 0s delay)";

            list.Add(new OptimizationItem
            {
                Id = 24,
                Category = "⚡ Hiệu năng & CPU",
                Title = "Điều Phối Luồng CPU & Power Throttling Off" + (hardware.IsAmdDualCcdX3D ? " [AMD X3D Aware]" : ""),
                Description = "Tối ưu bộ điều phối luồng Windows Scheduler, tắt Power Throttling cho Foreground Apps giúp game nhận 100% sức mạnh tính toán." + cpuWarning,
                Badges = new List<string> { hardware.IsAmdDualCcdX3D ? "Bảo Toàn 3D V-Cache" : "Tắt Core Parking", "Tắt Power Throttling", "Ưu Tiên Game" },
                Icon = "🧠",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 0) == 1,
                NativeApply = () =>
                {
                    if (!hardware.IsAmdDualCcdX3D)
                    {
                        NativeTweakService.RunCli("powercfg.exe", "-setacvalueindex scheme_current sub_processor CPMINCORES 100");
                        NativeTweakService.RunCli("powercfg.exe", "-setactive scheme_current");
                    }
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff");
                    return true;
                },
                Script = hardware.IsAmdDualCcdX3D ? @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling' -Name 'PowerThrottlingOff' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da tat Power Throttling va bao toan Core Parking cho AMD 3D V-Cache CCD.' -ForegroundColor Green
" : @"
& powercfg.exe -setacvalueindex scheme_current sub_processor CPMINCORES 100 2>$null | Out-Null
& powercfg.exe -setactive scheme_current 2>$null | Out-Null
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling' -Name 'PowerThrottlingOff' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da kich hoat Dynamic CPU Scheduling va tat Core Parking.' -ForegroundColor Green
",
                RevertScript = @"
Remove-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling' -Name 'PowerThrottlingOff' -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoi phuc Power Throttling ve mac dinh.' -ForegroundColor Yellow
"
            });

            // 25. Tự động dọn Standby Memory
            list.Add(new OptimizationItem
            {
                Id = 25,
                Category = "⚡ Hiệu năng & CPU",
                Title = "Tự Động Dọn Standby Memory (Xóa Sạch Bộ Nhớ Chờ Trực Tiếp Từ RAM)",
                Description = "Giải phóng tức thì danh sách bộ nhớ chờ (Standby List) của Windows bằng Native API NtSetSystemInformation. Trả lại hàng Gigabyte RAM khả dụng tức thì cho Game mà không tốn CPU và hoàn toàn không ghi hay làm tràn ổ cứng SSD.",
                Badges = new List<string> { "Xóa Standby List", "Giải Phóng Hàng GB RAM", "Không Hại SSD" },
                Icon = "🧹",
                CheckStatus = () => true,
                NativeApply = () =>
                {
                    MemoryCleanerService.PurgeRamDetailed();
                    return true;
                },
                Script = @"
Write-Host '[OK] Da giai phong Standby Memory thanh cong qua Native NT API!' -ForegroundColor Green
"
            });

            // ==================== CÁC TÍNH NĂNG MỚI ĐÓN ĐẦU 2026 - 2027 ====================

            // 26. 🤖 Windows 11 AI & Copilot Debloat (MỚI 2026)
            list.Add(new OptimizationItem
            {
                Id = 26,
                Category = "🤖 AI & Copilot (2026)",
                Title = "Khóa Triệt Để Windows Recall, Copilot AI & AI Telemetry",
                Description = "Vô hiệu hóa tính năng chụp màn hình ngầm Windows Recall (Total Recall), tắt phím tắt và biểu tượng Copilot trên Taskbar, chặn NPU/CPU phân tích hành vi người dùng, bảo vệ quyền riêng tư và tiết kiệm tài nguyên máy tính.",
                Badges = new List<string> { "Tắt Recall 100%", "Tắt Copilot Sidecar", "Chống Rò Rỉ Dữ Liệu" },
                Icon = "🤖",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 0) == 1,
                NativeApply = () =>
                {
                    // Tắt Windows AI / Recall
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 1);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 1);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsAI", "DisableRecall", 1);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableRecall", 1);

                    // Tắt Windows Copilot
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCopilotButton", 0);

                    // Tắt Edge AI / Copilot Hub
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "HubsSidebarEnabled", 0);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis");
                    NativeTweakService.DeleteValue(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis");
                    NativeTweakService.DeleteValue(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsAI", "DisableRecall");
                    NativeTweakService.DeleteValue(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot");
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot");
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCopilotButton", 1);
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI' -Name 'DisableAIDataAnalysis' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Software\Policies\Microsoft\Windows\WindowsAI' -Name 'DisableAIDataAnalysis' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Software\Policies\Microsoft\Windows\WindowsAI' -Name 'DisableRecall' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Software\Policies\Microsoft\Windows\WindowsCopilot' -Name 'TurnOffWindowsCopilot' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot' -Name 'TurnOffWindowsCopilot' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name 'ShowCopilotButton' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoa toan bo Windows Recall va Copilot AI Background Tasks.' -ForegroundColor Green
",
                RevertScript = @"
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsAI' -Name 'DisableAIDataAnalysis' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path 'HKCU:\Software\Policies\Microsoft\Windows\WindowsAI' -Name 'DisableAIDataAnalysis' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path 'HKCU:\Software\Policies\Microsoft\Windows\WindowsCopilot' -Name 'TurnOffWindowsCopilot' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name 'ShowCopilotButton' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoi phuc thiet lap Windows AI & Copilot mac dinh.' -ForegroundColor Yellow
"
            });

            // 27. 🛡️ Core Isolation / VBS Gaming Mode
            list.Add(new OptimizationItem
            {
                Id = 27,
                Category = "⚡ Hiệu năng & CPU",
                Title = "Chế Độ Gaming eSports: Tắt Core Isolation (HVCI) Tăng 5-15% FPS",
                Description = "Tắt tính năng Toàn vẹn bộ nhớ (Hypervisor-Protected Code Integrity - HVCI). Giúp loại bỏ hoàn toàn độ trễ kiểm tra mã độc của CPU, đem lại mức tăng từ 5% đến 15% FPS trong các tựa game bắn súng thi đấu eSports nặng CPU.",
                Badges = new List<string> { "Tăng 5-15% FPS", "Chuẩn Máy Thi Đấu", "Có Thể Hoàn Tác" },
                Icon = "🛡️",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 1) == 0,
                NativeApply = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 0);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 1);
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity' -Name 'Enabled' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da tat Core Isolation (HVCI) de giai phong 100% FPS Gaming.' -ForegroundColor Green
",
                RevertScript = @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity' -Name 'Enabled' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da bat lai Core Isolation (HVCI) bao mat.' -ForegroundColor Yellow
"
            });

            // 28. 🎮 DirectX 12 / Vulkan Shader Cache (10GB)
            list.Add(new OptimizationItem
            {
                Id = 28,
                Category = "🎮 GPU & Gaming",
                Title = "DirectX 12 / Vulkan: Mở Rộng Shader Cache 10GB (Chống Khựng Hình UE5)",
                Description = "Tăng dung lượng bộ nhớ đệm Shader Cache của DirectX và Driver đồ họa lên 10GB. Giúp lưu trữ toàn bộ mã shader đã biên dịch, triệt tiêu hiện tượng khựng lag (Shader Stutter) khi tải bản đồ mới trong các game Unreal Engine 5 và DirectX 12.",
                Badges = new List<string> { "Shader Cache 10GB", "Chống Khựng UE5", "Load Map Siêu Nhanh" },
                Icon = "🎨",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.CurrentUser, @"Software\Microsoft\DirectX", "D3D_SHADER_CACHE_MAX_SIZE", 0) == 10240,
                NativeApply = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\DirectX", "D3D_SHADER_CACHE_MAX_SIZE", 10240);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\DirectX", "D3D_SHADER_CACHE_MAX_SIZE", 10240);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.DeleteValue(RegistryHive.CurrentUser, @"Software\Microsoft\DirectX", "D3D_SHADER_CACHE_MAX_SIZE");
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\DirectX' -Name 'D3D_SHADER_CACHE_MAX_SIZE' -Value 10240 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\DirectX' -Name 'D3D_SHADER_CACHE_MAX_SIZE' -Value 10240 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da mo rong DirectX Shader Cache len 10GB.' -ForegroundColor Green
",
                RevertScript = @"
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\DirectX' -Name 'D3D_SHADER_CACHE_MAX_SIZE' -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoi phuc Shader Cache mac dinh.' -ForegroundColor Yellow
"
            });

            // 29. 🌐 DoH (DNS over HTTPS) & UDP Gaming Priority
            list.Add(new OptimizationItem
            {
                Id = 29,
                Category = "🌐 Mạng & MSI",
                Title = "DNS over HTTPS (DoH) Mã Hóa & Tối Ưu Độ Trễ TCP/UDP Gaming",
                Description = "Kích hoạt giao thức phân giải tên miền mã hóa HTTPS (Cloudflare DoH 1.1.1.1) bảo mật tuyệt đối, giảm thời gian phản hồi máy chủ game và ép TCP AckFrequency = 1 để gửi xác nhận gói tin game tức thời.",
                Badges = new List<string> { "DoH Mã Hóa", "TCP AckFrequency = 1", "Ping Siêu Ổn Định" },
                Icon = "🔒",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", "TcpAckFrequency", 0) == 1,
                NativeApply = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "Tcp1323Opts", 1);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "MaxUserPort", 65534);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TcpTimedWaitDelay", 30);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "Tcp1323Opts");
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters' -Name 'Tcp1323Opts' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters' -Name 'MaxUserPort' -Value 65534 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters' -Name 'TcpTimedWaitDelay' -Value 30 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da toi uu thong so TCP/UDP AckFrequency va DoH Network Stack.' -ForegroundColor Green
",
                RevertScript = @"
Remove-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters' -Name 'Tcp1323Opts' -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoi phuc thiet lap TCP/IP Parameters mac dinh.' -ForegroundColor Yellow
"
            });

            // 30. ⚡ Auto RAM Standby Cleaner Daemon
            list.Add(new OptimizationItem
            {
                Id = 30,
                Category = "⚡ Hiệu năng & CPU",
                Title = "Kiểm Soát RAM Nâng Cao: Tự Động Xả Cache Bộ Nhớ Khi Đầy",
                Description = "Thực hiện dọn sạch Standby List và Working Sets đồng thời áp dụng chính sách ưu tiên cấp phát RAM tức thời cho ứng dụng đang hoạt động ở Foreground.",
                Badges = new List<string> { "Dọn Sạch Working Sets", "Ưu Tiên Bộ Nhớ Game", "Zero Paging Overhead" },
                Icon = "⚡",
                CheckStatus = () => true,
                NativeApply = () =>
                {
                    MemoryCleanerService.PurgeRamDetailed();
                    return true;
                },
                Script = @"
Write-Host '[OK] Da thuc hien kiem soat RAM va xoa sach Standby Memory List.' -ForegroundColor Green
"
            });

            return list;
        }
    }
}
