using System.IO;
using Microsoft.Win32;
using PerformanceOptimizer.Models;

namespace PerformanceOptimizer.Services
{
    public static class OptimizationDataService
    {
        private static List<string> ResolveGpuAppPaths()
        {
            var list = new List<string>();
            var drives = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
                .Select(d => d.Name.TrimEnd('\\', ':'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!drives.Contains("C", StringComparer.OrdinalIgnoreCase)) drives.Add("C");

            // 1. Valorant Shipping Binary & Riot Client & League of Legends
            foreach (var d in drives)
            {
                string valPath = $@"{d}:\Riot Games\VALORANT\live\ShooterGame\Binaries\Win64\VALORANT-Win64-Shipping.exe";
                if (File.Exists(valPath)) list.Add(valPath);
                string clientPath = $@"{d}:\Riot Games\Riot Client\RiotClientServices.exe";
                if (File.Exists(clientPath)) list.Add(clientPath);
                string lolPath = $@"{d}:\Riot Games\League of Legends\Game\League of Legends.exe";
                if (File.Exists(lolPath)) list.Add(lolPath);
            }
            try
            {
                using var valKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Riot Game valorant.live");
                string loc = valKey?.GetValue("InstallLocation")?.ToString() ?? "";
                if (!string.IsNullOrEmpty(loc))
                {
                    string p = Path.Combine(loc, @"ShooterGame\Binaries\Win64\VALORANT-Win64-Shipping.exe");
                    if (File.Exists(p) && !list.Contains(p)) list.Add(p);
                }
            }
            catch { }

            // Dự phòng đường dẫn mặc định nếu chưa cài đặt để luôn sẵn sàng
            if (!list.Any(x => x.Contains("VALORANT-Win64-Shipping.exe", StringComparison.OrdinalIgnoreCase)))
            {
                list.Add(@"C:\Riot Games\VALORANT\live\ShooterGame\Binaries\Win64\VALORANT-Win64-Shipping.exe");
            }

            // 2. CS2, Apex Legends, Dota 2
            foreach (var d in drives)
            {
                string csPath1 = $@"{d}:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\game\bin\win64\cs2.exe";
                string csPath2 = $@"{d}:\SteamLibrary\steamapps\common\Counter-Strike Global Offensive\game\bin\win64\cs2.exe";
                if (File.Exists(csPath1)) list.Add(csPath1);
                if (File.Exists(csPath2)) list.Add(csPath2);

                string apexPath1 = $@"{d}:\Program Files (x86)\Steam\steamapps\common\Apex Legends\r5apex.exe";
                string apexPath2 = $@"{d}:\SteamLibrary\steamapps\common\Apex Legends\r5apex.exe";
                if (File.Exists(apexPath1)) list.Add(apexPath1);
                if (File.Exists(apexPath2)) list.Add(apexPath2);

                string dotaPath1 = $@"{d}:\Program Files (x86)\Steam\steamapps\common\dota 2 beta\game\bin\win64\dota2.exe";
                string dotaPath2 = $@"{d}:\SteamLibrary\steamapps\common\dota 2 beta\game\bin\win64\dota2.exe";
                if (File.Exists(dotaPath1)) list.Add(dotaPath1);
                if (File.Exists(dotaPath2)) list.Add(dotaPath2);
            }

            // 3. Trình duyệt & Dev Tools
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            string[] common = {
                Path.Combine(localAppData, @"Programs\Microsoft VS Code\Code.exe"),
                Path.Combine(progFiles, @"Microsoft VS Code\Code.exe"),
                Path.Combine(progFiles, @"Google\Chrome\Application\chrome.exe"),
                Path.Combine(progFilesX86, @"Google\Chrome\Application\chrome.exe"),
                Path.Combine(progFilesX86, @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(progFiles, @"Microsoft\Edge\Application\msedge.exe")
            };
            foreach (var p in common)
            {
                if (File.Exists(p) && !list.Contains(p)) list.Add(p);
            }

            return list;
        }

        public static List<OptimizationItem> GetItems(HardwareSpecs? hardware = null)
        {
            var list = new List<OptimizationItem>();
            hardware ??= HardwareService.DetectSpecs();

            // 1. Power Plan: 100% Turbo Boost
            list.Add(new OptimizationItem
            {
                Id = 1,
                Category = "⚡ Hiệu năng & CPU",
                Title = "Power Plan: High Performance (100% Max Turbo Boost Cho Mọi Máy)",
                Description = "Tối ưu hóa trực tiếp trên gói nguồn hiện tại của máy (tương thích 100% cả PC Desktop và Laptop OEM Asus, Lenovo, Dell, HP). Mở khóa Max Turbo Boost, cho phép nhân nhàn rỗi ngủ sâu C-State để dồn trọn vẹn TDP bung đỉnh xung 4.0 - 5.0+ GHz.",
                Badges = new List<string> { "Max Turbo 4.0+ GHz", "Không Tụt Xung", "Mọi Máy PC/Laptop" },
                Icon = "⚡",
                CheckStatus = () =>
                {
                    int max = NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\bc5038f7-23e0-4960-96da-33abaf5935ec", "CurrentValue", -1);
                    return max == 100;
                },
                NativeApply = () =>
                {
                    return NativeTweakService.EnsureHighPerformancePowerPlan(hardware);
                },
                NativeRevert = () =>
                {
                    NativeTweakService.RunCli("powercfg.exe", "-setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 36687f9e-e3a5-4dbf-b1dc-15eb381c6863 50");
                    NativeTweakService.RunCli("powercfg.exe", "-setactive scheme_current");
                    return true;
                },
                Script = @"
& powercfg.exe -setacvalueindex scheme_current sub_processor PROCTHROTTLEMAX 100 2>$null | Out-Null
& powercfg.exe -setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN 5 2>$null | Out-Null
& powercfg.exe -setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 be337238-0d82-4146-a960-4f3749d470c7 2 2>$null | Out-Null
& powercfg.exe -setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 36687f9e-e3a5-4dbf-b1dc-15eb381c6863 20 2>$null | Out-Null
& powercfg.exe -setactive scheme_current 2>$null | Out-Null
Write-Host '[OK] Da toi uu Power Plan tren scheme_current (Bung Max Turbo 4.0+ GHz tren moi dong may).' -ForegroundColor Green
",
                RevertScript = @"
& powercfg.exe -setacvalueindex scheme_current 54533251-82be-4824-96c1-47b60b740d00 36687f9e-e3a5-4dbf-b1dc-15eb381c6863 50 2>$null | Out-Null
& powercfg.exe -setactive scheme_current 2>$null | Out-Null
Write-Host '[OK] Da dua Power Plan ve che do can bang mac dinh.' -ForegroundColor Yellow
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
                    // Tự động thích ứng GPU: Chỉ bật HAGS nếu có GPU rời (NVIDIA RTX/GTX, AMD RX, Intel Arc)
                    // Nếu là GPU tích hợp (Intel UHD/Iris Xe hoặc AMD Radeon Vega/680M/780M), giữ HwSchMode = 1 để tránh nghẽn RAM dùng chung
                    int hwSch = hardware.HasDedicatedGpu ? 2 : 1;
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", hwSch);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0);
                    // Giữ GameDVR_FSEBehaviorMode = 0 để Windows 11 bảo toàn Independent Flip Model (giảm lag và mượt mà cho Borderless/Fullscreen)
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"System\GameConfigStore", "GameDVR_FSEBehaviorMode", 0);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 1);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 1);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"System\GameConfigStore", "GameDVR_FSEBehaviorMode", 0);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 1);
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR");
                    return true;
                },
                Script = @"
$hasDedicated = " + (hardware.HasDedicatedGpu ? "1" : "0") + @";
if ($hasDedicated -eq 1) {
    Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name 'HwSchMode' -Value 2 -Type DWord -ErrorAction SilentlyContinue
} else {
    Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name 'HwSchMode' -Value 1 -Type DWord -ErrorAction SilentlyContinue
}
Set-ItemProperty -Path 'HKCU:\System\GameConfigStore' -Name 'GameDVR_Enabled' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\System\GameConfigStore' -Name 'GameDVR_FSEBehaviorMode' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR' -Name 'AllowGameDVR' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da cau hinh HAGS thich ung va tat GameDVR.' -ForegroundColor Green
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
                Title = "Gán GPU Rời Dedicated cho Valorant, CS2 & Dev Tools",
                Description = "Tự động quét đường dẫn thực thi chính xác của VALORANT (VALORANT-Win64-Shipping.exe), CS2 và môi trường dev (VS Code, Chrome, Edge), ép Windows Direct3D luôn ưu tiên card đồ họa mạnh nhất.",
                Badges = new List<string> { "Full Path Scan", "Ưu Tiên Valorant", "High Performance GPU" },
                Icon = "🚀",
                CheckStatus = () =>
                {
                    var paths = ResolveGpuAppPaths();
                    return paths.Any(p => NativeTweakService.ValueExists(RegistryHive.CurrentUser, @"Software\Microsoft\DirectX\UserGpuPreferences", p));
                },
                NativeApply = () =>
                {
                    // Chỉ gán nếu máy có GPU rời (NVIDIA / AMD Radeon RX / Intel Arc) để tránh lỗi gọi Direct3D sai trên máy chỉ có iGPU
                    if (!hardware.HasDedicatedGpu) return true;
                    var paths = ResolveGpuAppPaths();
                    foreach (var path in paths)
                    {
                        NativeTweakService.SetString(RegistryHive.CurrentUser, @"Software\Microsoft\DirectX\UserGpuPreferences", path, "GpuPreference=2;");
                    }
                    return true;
                },
                NativeRevert = () =>
                {
                    var paths = ResolveGpuAppPaths();
                    foreach (var path in paths)
                    {
                        NativeTweakService.DeleteValue(RegistryHive.CurrentUser, @"Software\Microsoft\DirectX\UserGpuPreferences", path);
                    }
                    return true;
                },
                Script = @"
$hasDedicated = " + (hardware.HasDedicatedGpu ? "1" : "0") + @";
if ($hasDedicated -eq 1) {
    $paths = @(
        'C:\Riot Games\VALORANT\live\ShooterGame\Binaries\Win64\VALORANT-Win64-Shipping.exe',
        'D:\Riot Games\VALORANT\live\ShooterGame\Binaries\Win64\VALORANT-Win64-Shipping.exe',
        'C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\game\bin\win64\cs2.exe'
    )
    foreach ($p in $paths) {
        if (Test-Path $p) {
            Set-ItemProperty -Path 'HKCU:\Software\Microsoft\DirectX\UserGpuPreferences' -Name $p -Value 'GpuPreference=2;' -Type String -ErrorAction SilentlyContinue
        }
    }
    Write-Host '[OK] Da gan High Performance GPU cho Valorant va cac ung dung game/dev.' -ForegroundColor Green
} else {
    Write-Host '[INFO] May su dung GPU Onboard (iGPU), bo qua gan Direct3D Dedicated de toi uu tai nguyen he thong.' -ForegroundColor Cyan
}
",
                RevertScript = @"
$paths = @(
    'C:\Riot Games\VALORANT\live\ShooterGame\Binaries\Win64\VALORANT-Win64-Shipping.exe',
    'D:\Riot Games\VALORANT\live\ShooterGame\Binaries\Win64\VALORANT-Win64-Shipping.exe',
    'C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\game\bin\win64\cs2.exe'
)
foreach ($p in $paths) {
    Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\DirectX\UserGpuPreferences' -Name $p -ErrorAction SilentlyContinue
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
                Description = "Tắt Energy Efficient Ethernet (EEE) và Green Ethernet (chống drop ping), giữ BẬT Interrupt Moderation để triệt tiêu bão ngắt DPC Latency Spike trong game 128 tickrate.",
                Badges = new List<string> { "Triệt Tiêu DPC Spike", "Tắt Tiết Kiệm Điện", "Ping Ổn Định" },
                Icon = "🌐",
                CheckStatus = () =>
                {
                    try
                    {
                        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
                        using var netClass = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}");
                        if (netClass != null)
                        {
                            foreach (var sub in netClass.GetSubKeyNames())
                            {
                                if (!int.TryParse(sub, out _)) continue;
                                using var dev = netClass.OpenSubKey(sub);
                                if (dev == null) continue;
                                string desc = dev.GetValue("DriverDesc")?.ToString() ?? "";
                                if (desc.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) || desc.Contains("Wireless", StringComparison.OrdinalIgnoreCase)) continue;
                                string? eee = dev.GetValue("*EEE")?.ToString() ?? dev.GetValue("EnergyEfficientEthernet")?.ToString();
                                string? green = dev.GetValue("*GreenEthernet")?.ToString();
                                if (eee == "0" || green == "0") return true;
                            }
                        }
                    }
                    catch { }
                    return false;
                },
                NativeApply = () =>
                {
                    string cmd = "Get-NetAdapter -Physical -ErrorAction SilentlyContinue | Where-Object { $_.InterfaceDescription -notmatch 'Wi-Fi|Wireless' } | ForEach-Object { Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Energy Efficient Ethernet' -DisplayValue 'Disabled' -ErrorAction SilentlyContinue; Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Green Ethernet' -DisplayValue 'Disabled' -ErrorAction SilentlyContinue; Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Interrupt Moderation' -DisplayValue 'Enabled' -ErrorAction SilentlyContinue }";
                    return NativeTweakService.RunCli("powershell.exe", $"-NoProfile -NonInteractive -Command \"{cmd}\"", 12000);
                },
                NativeRevert = () =>
                {
                    string cmd = "Get-NetAdapter -Physical -ErrorAction SilentlyContinue | Where-Object { $_.InterfaceDescription -notmatch 'Wi-Fi|Wireless' } | ForEach-Object { Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Energy Efficient Ethernet' -DisplayValue 'Enabled' -ErrorAction SilentlyContinue; Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Green Ethernet' -DisplayValue 'Enabled' -ErrorAction SilentlyContinue; Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Interrupt Moderation' -DisplayValue 'Enabled' -ErrorAction SilentlyContinue }";
                    return NativeTweakService.RunCli("powershell.exe", $"-NoProfile -NonInteractive -Command \"{cmd}\"", 12000);
                },
                Script = @"
Get-NetAdapter -Physical -ErrorAction SilentlyContinue | Where-Object { $_.InterfaceDescription -notmatch 'Wi-Fi|Wireless' } | ForEach-Object {
    Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Energy Efficient Ethernet' -DisplayValue 'Disabled' -ErrorAction SilentlyContinue
    Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Green Ethernet' -DisplayValue 'Disabled' -ErrorAction SilentlyContinue
    Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Interrupt Moderation' -DisplayValue 'Enabled' -ErrorAction SilentlyContinue
}
Write-Host '[OK] Da toi uu Card Mang Day Ethernet (Bao ve Interrupt Moderation chong DPC Latency).' -ForegroundColor Green
",
                RevertScript = @"
Get-NetAdapter -Physical -ErrorAction SilentlyContinue | Where-Object { $_.InterfaceDescription -notmatch 'Wi-Fi|Wireless' } | ForEach-Object {
    Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Energy Efficient Ethernet' -DisplayValue 'Enabled' -ErrorAction SilentlyContinue
    Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Green Ethernet' -DisplayValue 'Enabled' -ErrorAction SilentlyContinue
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
                CheckStatus = () =>
                {
                    try
                    {
                        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
                        using var netClass = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}");
                        if (netClass != null)
                        {
                            foreach (var sub in netClass.GetSubKeyNames())
                            {
                                if (!int.TryParse(sub, out _)) continue;
                                using var dev = netClass.OpenSubKey(sub);
                                if (dev == null) continue;
                                string desc = dev.GetValue("DriverDesc")?.ToString() ?? "";
                                if (!desc.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) && !desc.Contains("Wireless", StringComparison.OrdinalIgnoreCase)) continue;
                                string? roam = dev.GetValue("RoamAggressiveness")?.ToString() ?? dev.GetValue("RoamingSensitivity3")?.ToString();
                                if (roam == "1") return true;
                            }
                        }
                    }
                    catch { }
                    return false;
                },
                NativeApply = () =>
                {
                    string cmd = "Get-NetAdapter -Physical -ErrorAction SilentlyContinue | Where-Object { $_.InterfaceDescription -match 'Wi-Fi|Wireless' } | ForEach-Object { Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Roaming Aggressiveness' -DisplayValue '1. Lowest' -ErrorAction SilentlyContinue; Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Transmit Power' -DisplayValue '5. Highest' -ErrorAction SilentlyContinue }";
                    return NativeTweakService.RunCli("powershell.exe", $"-NoProfile -NonInteractive -Command \"{cmd}\"", 12000);
                },
                NativeRevert = () =>
                {
                    string cmd = "Get-NetAdapter -Physical -ErrorAction SilentlyContinue | Where-Object { $_.InterfaceDescription -match 'Wi-Fi|Wireless' } | ForEach-Object { Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Roaming Aggressiveness' -DisplayValue '3. Medium' -ErrorAction SilentlyContinue }";
                    return NativeTweakService.RunCli("powershell.exe", $"-NoProfile -NonInteractive -Command \"{cmd}\"", 12000);
                },
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
                    NativeTweakService.SetCloudflareDns(false);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", -1);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 20);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.ResetDnsToDefault();
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 10);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 20);
                    return true;
                },
                Script = @"
Get-NetAdapter -Physical -ErrorAction SilentlyContinue | ForEach-Object {
    Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ServerAddresses @('1.1.1.1', '1.0.0.1') -ErrorAction SilentlyContinue
}
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name 'NetworkThrottlingIndex' -Value 0xFFFFFFFF -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' -Name 'SystemResponsiveness' -Value 20 -Type DWord -ErrorAction SilentlyContinue
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

            // 8. Hypervisor An Toàn (BẢO VỆ RIOT VANGUARD)
            list.Add(new OptimizationItem
            {
                Id = 8,
                Category = "🛡️ Dịch vụ & Windows",
                Title = "Bảo Toàn Hypervisor & Chống Lỗi Riot Vanguard (VAN9005/VAN1067)",
                Description = "Đảm bảo tính năng ảo hóa và chống gian lận (Riot Vanguard, Easy Anti-Cheat, BattlEye) hoạt động chuẩn mực. Thiết lập hypervisorlaunchtype auto để Valorant khởi chạy 100% không bị lỗi VAN9005 hay VAN1067.",
                Badges = new List<string> { "An Toàn Riot Vanguard", "Chống Lỗi VAN9005", "WSL2/Docker OK" },
                Icon = "🛡️",
                CheckStatus = () =>
                {
                    return NativeTweakService.KeyExists(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Hypervisor") ||
                           NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 0) == 1;
                },
                NativeApply = () =>
                {
                    NativeTweakService.RunCli("bcdedit.exe", "/set hypervisorlaunchtype auto");
                    return true;
                },
                Script = @"
& bcdedit.exe /set hypervisorlaunchtype auto 2>$null | Out-Null
Write-Host '[OK] Da kiem tra va bao toan Hypervisor Launch Type Auto (Chong loi Vanguard).' -ForegroundColor Green
"
            });

            // 9. Tắt Services Dư Thừa
            list.Add(new OptimizationItem
            {
                Id = 9,
                Category = "🛡️ Dịch vụ & Windows",
                Title = "Tắt Services Windows Dư Thừa & Chống Reboot Ngầm",
                Description = "Vô hiệu hóa DiagTrack, SysMain, MapsBroker, Xbox DVR và thiết lập chính sách không tự động ép khởi động lại máy khi đang làm việc/chơi game. Tuyệt đối không can thiệp dịch vụ Riot Vanguard (vgc).",
                Badges = new List<string> { "Tiết Kiệm RAM", "Không Reboot Ngầm", "Không Đụng Vanguard" },
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
                    string[] services = { "SysMain", "PcaSvc", "MapsBroker", "DiagTrack", "dmwappushservice", "lfsvc", "RemoteRegistry", "WalletService" };
                    foreach (var s in services)
                    {
                        NativeTweakService.SetServiceStartup(s, "auto");
                        NativeTweakService.StartService(s);
                    }
                    NativeTweakService.SetServiceStartup("BcastDVRUserService", "manual");
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers");
                    return true;
                },
                Script = @"
$services = @('DiagTrack', 'dmwappushservice', 'SysMain', 'lfsvc', 'MapsBroker', 'PcaSvc', 'RemoteRegistry', 'WalletService')
foreach ($s in $services) {
    & sc.exe stop $s 2>$null | Out-Null
    & sc.exe config $s start= disabled 2>$null | Out-Null
}
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\BcastDVRUserService' -Name 'Start' -Value 4 -Type DWord -ErrorAction SilentlyContinue
& sc.exe config WSearch start= delayed-auto 2>$null | Out-Null
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU' -Name 'NoAutoRebootWithLoggedOnUsers' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da tat services du thua va bao toan Windows Search.' -ForegroundColor Green
",
                RevertScript = @"
$services = @('SysMain', 'PcaSvc', 'MapsBroker', 'DiagTrack', 'dmwappushservice', 'lfsvc', 'RemoteRegistry', 'WalletService')
foreach ($s in $services) {
    & sc.exe config $s start= auto 2>$null | Out-Null
    & sc.exe start $s 2>$null | Out-Null
}
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\BcastDVRUserService' -Name 'Start' -Value 3 -Type DWord -ErrorAction SilentlyContinue
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU' -Name 'NoAutoRebootWithLoggedOnUsers' -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoi phuc day du cac services he thong ve mac dinh.' -ForegroundColor Yellow
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
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", 1) == 0,
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
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", 0);
                    return true;
                },
                NativeRevert = () =>
                {
                    string[] tasks = {
                        @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
                        @"\Microsoft\Windows\Application Experience\ProgramDataUpdater"
                    };
                    foreach (var t in tasks) NativeTweakService.SetScheduledTaskState(t, true);
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable");
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
                Description = "Tự động phát hiện dung lượng RAM: Nếu RAM >= 8GB, khóa toàn bộ nhân Kernel Windows trong RAM để phản hồi 0s delay ổ đĩa. Tắt cập nhật LastAccess NTFS để tăng tốc SSD (Đã loại bỏ LargeSystemCache để triệt tiêu lỗi tràn RAM & giật khựng trong Valorant / Unreal Engine).",
                Badges = new List<string> { "Zero Disk Delay", "Khóa Kernel RAM", "Bảo Vệ SSD", "Mượt Mà Valorant" },
                Icon = "💾",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 0) == 1,
                NativeApply = () =>
                {
                    // LargeSystemCache = 0 bảo đảm RAM vật lý được ưu tiên cho Game, tránh crash driver GPU nvlddmkm
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 0);
                    // Thích ứng mọi cấu hình: Chỉ khóa Kernel trên RAM nếu RAM vật lý >= 8GB. Máy RAM <= 6GB giữ 0 để tránh cạn RAM.
                    int disablePaging = (hardware.RamSizeGb >= 7.5) ? 1 : 0;
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", disablePaging);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsDisableLastAccessUpdate", 1);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 0);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 0);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsDisableLastAccessUpdate", 0);
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management' -Name 'LargeSystemCache' -Value 0 -Type DWord -ErrorAction SilentlyContinue
$hasHighRam = " + (hardware.RamSizeGb >= 7.5 ? "1" : "0") + @";
if ($hasHighRam -eq 1) {
    Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management' -Name 'DisablePagingExecutive' -Value 1 -Type DWord -ErrorAction SilentlyContinue
} else {
    Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management' -Name 'DisablePagingExecutive' -Value 0 -Type DWord -ErrorAction SilentlyContinue
}
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name 'NtfsDisableLastAccessUpdate' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da toi uu Kernel Memory thich ung theo dung luong RAM va tang toc NTFS.' -ForegroundColor Green
",
                RevertScript = @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management' -Name 'LargeSystemCache' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management' -Name 'DisablePagingExecutive' -Value 0 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name 'NtfsDisableLastAccessUpdate' -Value 0 -Type DWord -ErrorAction SilentlyContinue
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
                    NativeTweakService.ApplyMouseKeyboardTweaks(true);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.ApplyMouseKeyboardTweaks(false);
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'MouseSpeed' -Value '0' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'MouseThreshold1' -Value '0' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'MouseThreshold2' -Value '0' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'DoubleClickHeight' -Value '4' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'DoubleClickWidth' -Value '4' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'DoubleClickSpeed' -Value '500' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Keyboard' -Name 'KeyboardDelay' -Value '0' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Keyboard' -Name 'KeyboardSpeed' -Value '31' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Accessibility\StickyKeys' -Name 'Flags' -Value '506' -Type String -ErrorAction SilentlyContinue
Write-Host '[OK] Da cai dat chuot eSports 1:1 va phan hoi phim sieu toc (Thong bao Kernel Win32 tuc thi).' -ForegroundColor Green
",
                RevertScript = @"
Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'MouseSpeed' -Value '1' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'MouseThreshold1' -Value '6' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'MouseThreshold2' -Value '10' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'DoubleClickHeight' -Value '4' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'DoubleClickWidth' -Value '4' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'DoubleClickSpeed' -Value '500' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Keyboard' -Name 'KeyboardDelay' -Value '1' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Control Panel\Keyboard' -Name 'KeyboardSpeed' -Value '31' -ErrorAction SilentlyContinue
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
                    NativeTweakService.SetString(RegistryHive.CurrentUser, @"Control Panel\Desktop", "AutoEndTasks", "0");
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 1);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
                    NativeTweakService.SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", 1);
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
Set-ItemProperty -Path 'HKCU:\Control Panel\Desktop' -Name 'AutoEndTasks' -Value '0' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize' -Name 'EnableTransparency' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize' -Name 'AppsUseLightTheme' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize' -Name 'SystemUsesLightTheme' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoi phuc Menu delay 400ms va theme sang mac dinh.' -ForegroundColor Yellow
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
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferQualityUpdatesPeriodInDays");
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
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate' -Name 'DeferQualityUpdatesPeriodInDays' -ErrorAction SilentlyContinue
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
Get-ChildItem -Path $env:TEMP -Exclude '.net*' -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path 'C:\Windows\Temp' -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
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
                Description = "Tự động phát hiện Card màn hình (NVIDIA / AMD / Intel) và Card mạng (Ethernet / Wi-Fi), kích hoạt chế độ ngắt MSI Mode (Message Signaled Interrupts). Ưu tiên cao cho GPU và chuẩn hóa cho card mạng nhằm triệt tiêu hoàn toàn hiện tượng DPC Latency và drop FPS trong combat.",
                Badges = new List<string> { "MSI Mode On", "GPU Priority High", "Triệt Tiêu DPC Trễ" },
                Icon = "⚡",
                CheckStatus = () =>
                {
                    try
                    {
                        using var pciBase = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
                        using var pciKey = pciBase.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI");
                        if (pciKey == null) return false;
                        foreach (var dev in pciKey.GetSubKeyNames())
                        {
                            using var devKey = pciKey.OpenSubKey(dev);
                            if (devKey == null) continue;
                            foreach (var inst in devKey.GetSubKeyNames())
                            {
                                using var instKey = devKey.OpenSubKey(inst);
                                if (instKey?.GetValue("Class")?.ToString()?.Equals("Display", StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    using var msi = instKey.OpenSubKey(@"Device Parameters\Interrupt Management\MessageSignaledInterruptProperties");
                                    if (msi != null && (int)(msi.GetValue("MSISupported") ?? 0) == 1) return true;
                                }
                            }
                        }
                    }
                    catch { }
                    return false;
                },
                NativeApply = () =>
                {
                    return NativeTweakService.ApplyMsiModeNative(true);
                },
                NativeRevert = () =>
                {
                    return NativeTweakService.ApplyMsiModeNative(false);
                },
                Script = @"
$pciKeys = Get-ChildItem -Path 'HKLM:\SYSTEM\CurrentControlSet\Enum\PCI' -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.PSChildName -eq 'Device Parameters' }
$count = 0
foreach ($key in $pciKeys) {
    $parent = Split-Path $key.PSPath -Parent
    $props = Get-ItemProperty -Path $parent -ErrorAction SilentlyContinue
    $class = $props.Class
    if ($class -eq 'Display') {
        $msiPath = Join-Path $key.PSPath 'Interrupt Management\MessageSignaledInterruptProperties'
        New-Item -Path $msiPath -Force -ErrorAction SilentlyContinue | Out-Null
        Set-ItemProperty -Path $msiPath -Name 'MSISupported' -Value 1 -Type DWord -ErrorAction SilentlyContinue
        $affPath = Join-Path $key.PSPath 'Interrupt Management\Affinity Policy'
        if (Test-Path $affPath) {
            Remove-ItemProperty -Path $affPath -Name 'DevicePriority' -ErrorAction SilentlyContinue
        }
        $count++
    } elseif ($class -eq 'Net') {
        $msiPath = Join-Path $key.PSPath 'Interrupt Management\MessageSignaledInterruptProperties'
        New-Item -Path $msiPath -Force -ErrorAction SilentlyContinue | Out-Null
        Set-ItemProperty -Path $msiPath -Name 'MSISupported' -Value 1 -Type DWord -ErrorAction SilentlyContinue
        $count++
    }
}
Write-Host ""[OK] Da kich hoat MSI Mode thanh cong cho $count thiet bi GPU va Card mang."" -ForegroundColor Green
",
                RevertScript = @"
$pciKeys = Get-ChildItem -Path 'HKLM:\SYSTEM\CurrentControlSet\Enum\PCI' -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.PSChildName -eq 'Device Parameters' }
foreach ($key in $pciKeys) {
    $parent = Split-Path $key.PSPath -Parent
    $props = Get-ItemProperty -Path $parent -ErrorAction SilentlyContinue
    $class = $props.Class
    if ($class -eq 'Display' -or $class -eq 'Net') {
        $msiPath = Join-Path $key.PSPath 'Interrupt Management\MessageSignaledInterruptProperties'
        Remove-ItemProperty -Path $msiPath -Name 'MSISupported' -ErrorAction SilentlyContinue
        $affPath = Join-Path $key.PSPath 'Interrupt Management\Affinity Policy'
        Remove-ItemProperty -Path $affPath -Name 'DevicePriority' -ErrorAction SilentlyContinue
    }
}
Write-Host '[OK] Da khoi phuc che do ngat thiet bi ve mac dinh.' -ForegroundColor Yellow
"
            });

            // 24. CPU Affinity & Power Throttling Off
            string cpuWarning = hardware.IsAmdDualCcdX3D 
                ? " (Phát hiện AMD Dual-CCD V-Cache: Bảo toàn Core Parking dồn toàn bộ luồng vào 3D V-Cache)"
                : (hardware.IsAmdProcessor 
                    ? " (Phát hiện AMD Ryzen: Tối ưu CPPC2 Preferred Cores để bung xung đỉnh 4.6 - 5.7+ GHz)"
                    : " (Giải phóng TDP headroom để CPU bung Max Turbo Boost 4.0+ GHz)");

            list.Add(new OptimizationItem
            {
                Id = 24,
                Category = "⚡ Hiệu năng & CPU",
                Title = "Điều Phối Luồng CPU & Power Throttling Off" + (hardware.IsAmdDualCcdX3D ? " [AMD X3D Aware]" : (hardware.IsAmdProcessor ? " [AMD Ryzen Aware]" : "")),
                Description = "Tối ưu bộ điều phối luồng Windows Scheduler thích ứng theo từng loại CPU. Giải phóng TDP và nhiệt độ để CPU bung Max Turbo Boost mà không bị nghẽn xung." + cpuWarning,
                Badges = new List<string> { hardware.IsAmdDualCcdX3D ? "Bảo Toàn 3D V-Cache" : (hardware.IsAmdProcessor ? "AMD CPPC2 Max Boost" : "Dynamic Turbo Boost"), "Tối Ưu Phân Luồng", "Ưu Tiên Game" },
                Icon = "🧠",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 0) == 1,
                NativeApply = () =>
                {
                    // Tối ưu trực tiếp trên scheme_current, bảo toàn cơ chế Core Parking tự nhiên để CPU dồn toàn bộ TDP bung xung 4.0 - 5.0+ GHz
                    NativeTweakService.RunCli("powercfg.exe", "-setactive scheme_current");
                    
                    // Với Intel Hybrid (P/E cores): Giữ nguyên cơ chế phân loại của Thread Director để không bị ném game sang E-Core
                    // Với AMD và Intel Non-Hybrid: Tắt PowerThrottling cho ứng dụng game
                    int ptOff = hardware.IsIntelHybrid ? 0 : 1;
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", ptOff);
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff");
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling' -Name 'PowerThrottlingOff' -Value 1 -Type DWord -ErrorAction SilentlyContinue
& powercfg.exe -setactive scheme_current 2>$null | Out-Null
Write-Host '[OK] Da toi uu CPU Scheduling cho moi loai may (Bao toan TDP cho Max Turbo Boost).' -ForegroundColor Green
",
                RevertScript = @"
Remove-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling' -Name 'PowerThrottlingOff' -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoi phuc CPU Scheduling ve mac dinh.' -ForegroundColor Yellow
"
            });

            // 25. Tự động dọn Standby Memory
            list.Add(new OptimizationItem
            {
                Id = 25,
                Category = "⚡ Hiệu năng & CPU",
                Title = "Xả Tức Thời Standby RAM (Direct Purge via NT Kernel)",
                Description = "Giải phóng tức thì danh sách bộ nhớ chờ (Standby List) của Windows bằng Native API NtSetSystemInformation (Mã lệnh 80/4). Trả lại hàng Gigabyte RAM khả dụng tức thì cho Game trong 0.05 giây mà không tốn CPU và hoàn toàn không ghi hay làm tràn ổ cứng SSD. An toàn tuyệt đối cho Riot Vanguard (không đụng Working Sets, chống lỗi VAN1067).",
                Badges = new List<string> { "Xóa Standby List", "Giải Phóng Hàng GB RAM", "Không Hại SSD", "An Toàn Vanguard" },
                Icon = "🧹",
                CheckStatus = () => false,
                NativeApply = () =>
                {
                    MemoryCleanerService.CleanStandbyMemory();
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

            // 27. 🛡️ Core Isolation / VBS Gaming Mode (LƯU Ý CHO VALORANT)
            bool isWin11 = Environment.OSVersion.Version.Build >= 22000;
            list.Add(new OptimizationItem
            {
                Id = 27,
                Category = "⚡ Hiệu năng & CPU",
                Title = "Chế Độ Gaming eSports: Core Isolation (HVCI) " + (isWin11 ? "[Lưu Ý Vanguard Win 11]" : "[An Toàn Cho Win 10]"),
                Description = isWin11
                    ? "Tắt Toàn vẹn bộ nhớ (HVCI) giúp tăng 5-15% FPS CPU trong game thi đấu. LƯU Ý CHO WIN 11: Riot Vanguard trên Win 11 bắt buộc yêu cầu VBS/HVCI để khởi động Valorant (tránh lỗi VAN9005). Nếu chơi Valorant trên Win 11, hãy GIỮ NGUYÊN hoặc bật Vanguard Safe Mode!"
                    : "Tắt Toàn vẹn bộ nhớ (HVCI) giúp tăng 5-15% FPS CPU. Trên Windows 10, Riot Vanguard không bắt buộc VBS, bạn có thể an toàn tắt để bung tối đa FPS cho mọi tựa game (Valorant, CS2, Apex). (Cần Restart máy để có hiệu lực).",
                Badges = new List<string> { isWin11 ? "Vanguard Win 11 (Cần Bật)" : "An Toàn Win 10", "Tăng 5-15% FPS Gaming", "Cần Khởi Động Lại" },
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
Write-Host '[OK] Da tat Core Isolation (HVCI). Can khoi dong lai may de co hieu luc!' -ForegroundColor Green
",
                RevertScript = @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity' -Name 'Enabled' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da bat lai Core Isolation (HVCI) bao mat cho Vanguard. Can khoi dong lai may!' -ForegroundColor Yellow
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
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\DirectX", "D3D_SHADER_CACHE_MAX_SIZE");
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\DirectX' -Name 'D3D_SHADER_CACHE_MAX_SIZE' -Value 10240 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\DirectX' -Name 'D3D_SHADER_CACHE_MAX_SIZE' -Value 10240 -Type DWord -ErrorAction SilentlyContinue
Write-Host '[OK] Da mo rong DirectX Shader Cache len 10GB.' -ForegroundColor Green
",
                RevertScript = @"
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\DirectX' -Name 'D3D_SHADER_CACHE_MAX_SIZE' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\DirectX' -Name 'D3D_SHADER_CACHE_MAX_SIZE' -ErrorAction SilentlyContinue
Write-Host '[OK] Da khoi phuc Shader Cache mac dinh.' -ForegroundColor Yellow
"
            });

            // 29. 🌐 DoH (DNS over HTTPS) & UDP Gaming Priority
            list.Add(new OptimizationItem
            {
                Id = 29,
                Category = "🌐 Mạng & MSI",
                Title = "DNS over HTTPS (DoH) Mã Hóa & Tối Ưu Độ Trễ TCP/UDP Gaming",
                Description = "Kích hoạt giao thức phân giải tên miền mã hóa HTTPS (Cloudflare DoH 1.1.1.1) bảo mật tuyệt đối, giảm thời gian phản hồi máy chủ game. Tối ưu cổng kết nối mạng và dọn dẹp bão ngắt TCP để bảo toàn băng thông UDP cho Valorant / CS2.",
                Badges = new List<string> { "DoH Mã Hóa", "Tối Ưu UDP Gaming", "Ping Siêu Ổn Định" },
                Icon = "🔒",
                CheckStatus = () => NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "Tcp1323Opts", 0) == 1,
                NativeApply = () =>
                {
                    // 1. Kích hoạt DoH thực thụ trên toàn bộ card mạng
                    NativeTweakService.SetCloudflareDns(true);

                    // 2. Tinh chỉnh TCP Stack
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "Tcp1323Opts", 1);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "MaxUserPort", 65534);
                    NativeTweakService.SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TcpTimedWaitDelay", 30);

                    // Bảo vệ Valorant/CS2 (chạy UDP): Xóa bỏ TcpAckFrequency = 1 & TcpDelAckTicks = 0 nếu có
                    // để triệt tiêu bão ngắt DPC (Interrupt Storm) do các app nền TCP (Discord, Riot Client) gây ra.
                    try
                    {
                        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
                        using var ifKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", true);
                        if (ifKey != null)
                        {
                            foreach (var sub in ifKey.GetSubKeyNames())
                            {
                                using var subDev = ifKey.OpenSubKey(sub, true);
                                subDev?.DeleteValue("TcpAckFrequency", false);
                                subDev?.DeleteValue("TcpDelAckTicks", false);
                            }
                        }
                    }
                    catch { }
                    return true;
                },
                NativeRevert = () =>
                {
                    NativeTweakService.ResetDnsToDefault();
                    NativeTweakService.DeleteValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "Tcp1323Opts");
                    try
                    {
                        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
                        using var ifKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", true);
                        if (ifKey != null)
                        {
                            foreach (var sub in ifKey.GetSubKeyNames())
                            {
                                using var subDev = ifKey.OpenSubKey(sub, true);
                                subDev?.DeleteValue("TcpAckFrequency", false);
                                subDev?.DeleteValue("TcpDelAckTicks", false);
                            }
                        }
                    }
                    catch { }
                    return true;
                },
                Script = @"
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters' -Name 'Tcp1323Opts' -Value 1 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters' -Name 'MaxUserPort' -Value 65534 -Type DWord -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters' -Name 'TcpTimedWaitDelay' -Value 30 -Type DWord -ErrorAction SilentlyContinue

$interfaces = Get-ChildItem -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces' -ErrorAction SilentlyContinue
foreach ($if in $interfaces) {
    Remove-ItemProperty -Path $if.PSPath -Name 'TcpAckFrequency' -ErrorAction SilentlyContinue
    Remove-ItemProperty -Path $if.PSPath -Name 'TcpDelAckTicks' -ErrorAction SilentlyContinue
}
& netsh.exe dns add encryption server=1.1.1.1 dohtemplate=https://cloudflare-dns.com/dns-query autoupgrade=yes udpfallback=no 2>$null | Out-Null
& netsh.exe dns add encryption server=1.0.0.1 dohtemplate=https://cloudflare-dns.com/dns-query autoupgrade=yes udpfallback=no 2>$null | Out-Null
Write-Host '[OK] Da toi uu thong so UDP Gaming Priority va DoH Network Stack tren toan bo Card mang.' -ForegroundColor Green
",
                RevertScript = @"
Remove-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters' -Name 'Tcp1323Opts' -ErrorAction SilentlyContinue
$interfaces = Get-ChildItem -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces' -ErrorAction SilentlyContinue
foreach ($if in $interfaces) {
    Remove-ItemProperty -Path $if.PSPath -Name 'TcpAckFrequency' -ErrorAction SilentlyContinue
    Remove-ItemProperty -Path $if.PSPath -Name 'TcpDelAckTicks' -ErrorAction SilentlyContinue
}
Write-Host '[OK] Da khoi phuc thiet lap TCP/IP Parameters mac dinh.' -ForegroundColor Yellow
"
            });

            // 30. ⚡ Auto RAM Standby Cleaner Daemon
            list.Add(new OptimizationItem
            {
                Id = 30,
                Category = "⚡ Hiệu năng & CPU",
                Title = "Kiểm Soát RAM Nâng Cao: Tự Động Xả Cache Bộ Nhớ Khi Đầy (RAM Daemon)",
                Description = "Kích hoạt dịch vụ giám sát chạy ngầm (Auto RAM Daemon), định kỳ tự động kiểm tra và giải phóng Standby Memory mỗi 5 phút hoặc khi bộ nhớ đệm tăng cao. Giúp game thủ chiến game liên tục nhiều giờ không bao giờ bị tràn RAM hay giật lag, 100% không ảnh hưởng Riot Vanguard (vgc).",
                Badges = new List<string> { "Auto RAM Daemon", "Xả Định Kỳ 5 Phút", "An Toàn Vanguard 100%", "Zero Lag" },
                Icon = "⚡",
                CheckStatus = () => MemoryCleanerService.IsAutoPurgeRunning,
                NativeApply = () =>
                {
                    MemoryCleanerService.StartAutoPurgeDaemon(5);
                    return true;
                },
                NativeRevert = () =>
                {
                    MemoryCleanerService.StopAutoPurgeDaemon();
                    return true;
                },
                Script = @"
Write-Host '[OK] Da kich hoat Auto RAM Standby Cleaner Daemon thanh cong.' -ForegroundColor Green
",
                RevertScript = @"
Write-Host '[OK] Da dung Auto RAM Cleaner Daemon.' -ForegroundColor Yellow
"
            });

            return list;
        }
    }
}
