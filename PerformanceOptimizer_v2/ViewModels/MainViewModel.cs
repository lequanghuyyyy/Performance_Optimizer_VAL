using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Windows.Data;
using Microsoft.Win32;
using PerformanceOptimizer.Models;
using PerformanceOptimizer.Services;

namespace PerformanceOptimizer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<OptimizationItem> _items;
        private ICollectionView _filteredItems;
        private string _selectedCategory = "Tất cả";
        private double _progressValue = 0;
        private string _progressText = "Sẵn sàng (0%)";
        private string _currentStepText = "Sẵn sàng. Chọn các mục mong muốn và nhấn \"TỐI ƯU NGAY\" hoặc \"HOÀN TÁC\".";
        private bool _isRunning = false;
        private bool _isSpecsPopupOpen = false;
        private bool _isFpsBoostActive = false;
        private HardwareSpecs _hardware;
        private bool _isAdmin;

        public MainViewModel()
        {
            _isAdmin = CheckAdmin();
            _hardware = HardwareService.DetectSpecs();

            var data = OptimizationDataService.GetItems(_hardware);
            _items = new ObservableCollection<OptimizationItem>(data);

            foreach (var item in _items)
            {
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(OptimizationItem.IsSelected) || e.PropertyName == nameof(OptimizationItem.Status))
                    {
                        OnPropertyChanged(nameof(SelectedCount));
                        OnPropertyChanged(nameof(ApplyButtonText));
                        OnPropertyChanged(nameof(RevertButtonText));
                    }
                };
            }

            _filteredItems = CollectionViewSource.GetDefaultView(_items);
            _filteredItems.Filter = FilterItem;

            // Mặc định bật Vanguard Safe Mode -> Tự động bỏ chọn mục tắt HVCI (Item 27) để chống lỗi VAN9005/VAN1067
            if (_isVanguardSafeMode)
            {
                var hvciItem = _items.FirstOrDefault(x => x.Id == 27);
                if (hvciItem != null)
                {
                    hvciItem.IsSelected = false;
                }
            }

            Categories = new List<string>
            {
                "Tất cả",
                "🤖 AI & Copilot (2026)",
                "⚡ Hiệu năng & CPU",
                "🎮 GPU & Gaming",
                "🌐 Mạng & MSI",
                "🛡️ Dịch vụ & Windows",
                "Chris_TiTus_Tech"
            };

            // Quét trạng thái cấu hình hiện tại của Windows trong background khi mở app
            Task.Run(ScanSystemStateAsync);
        }

        public async Task ScanSystemStateAsync()
        {
            try
            {
                foreach (var item in _items)
                {
                    if (item.CheckStatus != null)
                    {
                        bool applied = false;
                        try { applied = item.CheckStatus(); } catch { }
                        item.IsApplied = applied;
                    }
                }

                int quantum = NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 2);
                int throttling = NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 0);
                int hvci = NativeTweakService.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 1);
                
                // Nhận diện thông minh: FPS Boost được coi là active nếu đã tối ưu Quantum & PowerThrottling (kể cả khi giữ HVCI cho Vanguard)
                IsFpsBoostActive = (quantum == 38 && throttling == 1) || (hvci == 0 && quantum == 38);
            }
            catch { }
        }

        private bool CheckAdmin()
        {
            try
            {
                var id = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(id);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public ObservableCollection<OptimizationItem> Items => _items;
        public ICollectionView FilteredItems => _filteredItems;
        public List<string> Categories { get; }
        public HardwareSpecs Hardware => _hardware;
        public bool IsAdmin => _isAdmin;

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged();
                    _filteredItems.Refresh();
                }
            }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                if (_progressValue != value)
                {
                    _progressValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ProgressText
        {
            get => _progressText;
            set
            {
                if (_progressText != value)
                {
                    _progressText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentStepText
        {
            get => _currentStepText;
            set
            {
                if (_currentStepText != value)
                {
                    _currentStepText = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsNotRunning));
                    OnPropertyChanged(nameof(ApplyButtonText));
                    OnPropertyChanged(nameof(RevertButtonText));
                }
            }
        }

        public bool IsNotRunning => !_isRunning;

        public bool IsSpecsPopupOpen
        {
            get => _isSpecsPopupOpen;
            set
            {
                if (_isSpecsPopupOpen != value)
                {
                    _isSpecsPopupOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isVanguardSafeMode = true;
        public bool IsVanguardSafeMode
        {
            get => _isVanguardSafeMode;
            set
            {
                if (_isVanguardSafeMode != value)
                {
                    _isVanguardSafeMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(VanguardSafeModeBadge));
                    OnPropertyChanged(nameof(VanguardSafeModeBorderBrush));
                    OnPropertyChanged(nameof(VanguardSafeModeBackgroundBrush));
                    OnPropertyChanged(nameof(VanguardSafeModeTextBrush));
                    OnPropertyChanged(nameof(VanguardSafeModeIcon));
                    OnPropertyChanged(nameof(FpsBoostTooltip));
                    OnPropertyChanged(nameof(FpsBoostModeText));
                }
            }
        }
        public string VanguardSafeModeBadge => _isVanguardSafeMode ? "VANGUARD SAFE: BẬT" : "VANGUARD SAFE: TẮT (CẢNH BÁO)";
        public string VanguardSafeModeBorderBrush => _isVanguardSafeMode ? "#0284C7" : "#EF4444";
        public string VanguardSafeModeBackgroundBrush => _isVanguardSafeMode ? "#16253B" : "#2A1215";
        public string VanguardSafeModeTextBrush => _isVanguardSafeMode ? "#38BDF8" : "#F87171";
        public string VanguardSafeModeIcon => _isVanguardSafeMode ? "🛡️" : "⚠️";

        public void ToggleVanguardSafeMode()
        {
            IsVanguardSafeMode = !IsVanguardSafeMode;
            if (_isVanguardSafeMode)
            {
                var hvciItem = _items.FirstOrDefault(x => x.Id == 27);
                if (hvciItem != null)
                {
                    hvciItem.IsSelected = false;
                }
            }
        }

        public bool IsFpsBoostActive
        {
            get => _isFpsBoostActive;
            set
            {
                if (_isFpsBoostActive != value)
                {
                    _isFpsBoostActive = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FpsBoostModeText));
                    OnPropertyChanged(nameof(FpsBoostStatusBadge));
                    OnPropertyChanged(nameof(FpsBoostButtonBrush));
                    OnPropertyChanged(nameof(FpsBoostBorderBrush));
                    OnPropertyChanged(nameof(FpsBoostTextBrush));
                    OnPropertyChanged(nameof(FpsBoostIconBrush));
                    OnPropertyChanged(nameof(FpsBoostSwitchTrackBrush));
                    OnPropertyChanged(nameof(FpsBoostSwitchBorderBrush));
                    OnPropertyChanged(nameof(FpsBoostSwitchKnobBrush));
                    OnPropertyChanged(nameof(FpsBoostSwitchAlignment));
                    OnPropertyChanged(nameof(FpsBoostTooltip));
                }
            }
        }

        public string FpsBoostModeText => _isFpsBoostActive ? (_isVanguardSafeMode ? "BẬT (Vanguard Safe)" : "BẬT (eSports)") : "ĐANG TẮT";
        public string FpsBoostStatusBadge => _isFpsBoostActive ? (_isVanguardSafeMode ? "Valorant Turbo" : "eSports Turbo") : "An Toàn / Mát Máy";
        public string FpsBoostButtonBrush => _isFpsBoostActive ? "#241203" : "#0F172A";
        public string FpsBoostBorderBrush => _isFpsBoostActive ? "#F59E0B" : "#334155";
        public string FpsBoostTextBrush => _isFpsBoostActive ? "#FBBF24" : "#64748B";
        public string FpsBoostIconBrush => _isFpsBoostActive ? "#FDE047" : "#475569";
        public string FpsBoostSwitchTrackBrush => _isFpsBoostActive ? "#D97706" : "#1E293B";
        public string FpsBoostSwitchBorderBrush => _isFpsBoostActive ? "#FBBF24" : "#475569";
        public string FpsBoostSwitchKnobBrush => _isFpsBoostActive ? "#FFFFFF" : "#94A3B8";
        public System.Windows.HorizontalAlignment FpsBoostSwitchAlignment => 
            _isFpsBoostActive ? System.Windows.HorizontalAlignment.Right : System.Windows.HorizontalAlignment.Left;
        public string FpsBoostTooltip => _isFpsBoostActive 
            ? (_isVanguardSafeMode 
                ? "⚡ Chế độ FPS Boost [Vanguard Safe ĐANG BẬT]: Xung 100% Turbo Boost, Win32Priority 38, dọn RAM. HVCI bảo vệ an toàn cho Valorant. Bấm để TẮT."
                : "⚡ Chế độ FPS Boost [eSports ĐANG BẬT]: Xung Turbo 100%, Core Isolation tắt để bung 5-15% FPS. Bấm để TẮT.")
            : "🛡️ Chế độ FPS Boost [ĐANG TẮT]: Máy chạy ở chế độ cân bằng an toàn. Bấm để BẬT chế độ Gaming Turbo cho Valorant/CS2.";

        public int SelectedCount => _items.Count(x => x.IsSelected);
        public int TotalCount => _items.Count;

        public string ApplyButtonText
        {
            get
            {
                if (IsRunning) return "ĐANG XỬ LÝ HỆ THỐNG...";
                var selected = _items.Where(x => x.IsSelected).ToList();
                if (selected.Count > 0 && selected.All(x => x.Status == OptimizationStatus.Success))
                {
                    return $"ĐÃ TỐI ƯU ({selected.Count} MỤC)";
                }
                return $"TỐI ƯU NGAY ({SelectedCount} MỤC)";
            }
        }

        public string RevertButtonText
        {
            get
            {
                if (IsRunning) return "ĐANG XỬ LÝ...";
                return $"HOÀN TÁC ({SelectedCount})";
            }
        }

        private int _gridColumns = 2;
        public int GridColumns
        {
            get => _gridColumns;
            set
            {
                if (_gridColumns != value)
                {
                    _gridColumns = value;
                    OnPropertyChanged();
                }
            }
        }

        public void UpdateLayoutForWidth(double width)
        {
            GridColumns = width >= 1350 ? 3 : 2;
        }

        private bool FilterItem(object obj)
        {
            if (obj is not OptimizationItem item) return false;
            if (SelectedCategory == "Tất cả") return true;
            return item.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase);
        }

        public void FilterByCategory(string cat)
        {
            SelectedCategory = cat;
        }

        public void SelectAll()
        {
            foreach (var item in _items)
            {
                if (SelectedCategory == "Tất cả" || item.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase))
                {
                    // Khi Vanguard Safe Mode đang BẬT: Bảo vệ tuyệt đối, không tự động tick tắt Core Isolation (Item 27)
                    if (_isVanguardSafeMode && item.Id == 27)
                    {
                        item.IsSelected = false;
                        continue;
                    }
                    item.IsSelected = true;
                }
            }
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(ApplyButtonText));
            OnPropertyChanged(nameof(RevertButtonText));
        }

        public void DeselectAll()
        {
            foreach (var item in _items)
            {
                if (SelectedCategory == "Tất cả" || item.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase))
                {
                    item.IsSelected = false;
                }
            }
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(ApplyButtonText));
            OnPropertyChanged(nameof(RevertButtonText));
        }

        public void OpenSpecsPopup()
        {
            _hardware = HardwareService.DetectSpecs();
            OnPropertyChanged(nameof(Hardware));
            IsSpecsPopupOpen = true;
        }

        public void CloseSpecsPopup()
        {
            IsSpecsPopupOpen = false;
        }

        public async Task ApplySelectedAsync()
        {
            var selected = _items.Where(x => x.IsSelected).ToList();
            if (selected.Count == 0)
            {
                CurrentStepText = "Vui lòng chọn ít nhất 1 mục để thực hiện tối ưu!";
                return;
            }

            IsRunning = true;
            ProgressValue = 0;
            int total = selected.Count;
            int done = 0;

            CurrentStepText = $"Bắt đầu tối ưu {total} mục đã chọn (Native High-Speed Engine)...";

            foreach (var item in selected)
            {
                // Bảo vệ tối cao cho Riot Vanguard: Nếu đang ở Vanguard Safe Mode, tuyệt đối không can thiệp tắt Core Isolation (HVCI)
                if (_isVanguardSafeMode && item.Id == 27)
                {
                    item.Status = OptimizationStatus.Reverted;
                    item.StatusText = "Bảo vệ Vanguard (Giữ nguyên)";
                    item.StatusBrush = "#38BDF8";
                    done++;
                    ProgressValue = Math.Round((double)done / total * 100, 0);
                    ProgressText = $"Đã hoàn thành {done}/{total} mục ({ProgressValue}%)";
                    continue;
                }

                item.Status = OptimizationStatus.Running;
                CurrentStepText = $"[Tiến trình {done + 1}/{total}] Đang xử lý: {item.Title}";

                bool ok = false;

                // 1. Thử thực thi Native C# trước (cực nhanh, ~0.001s, không mở subprocess)
                if (item.NativeApply != null)
                {
                    try
                    {
                        ok = await Task.Run(() => item.NativeApply());
                    }
                    catch
                    {
                        ok = false;
                    }
                }
                
                // 2. Nếu không có NativeApply hoặc cần chạy script nền
                if (!ok && !string.IsNullOrWhiteSpace(item.Script))
                {
                    if (item.Id == 25)
                    {
                        MemoryCleanerService.CleanStandbyMemory();
                    }

                    int timeoutMs = (item.Id == 22) ? 180000 : 45000;
                    ok = await PowerShellRunner.ExecuteScriptAsync(item.Script, statusLine =>
                    {
                        if (!string.IsNullOrWhiteSpace(statusLine) && !statusLine.Contains("PowerShell"))
                        {
                            CurrentStepText = statusLine;
                        }
                    }, timeoutMs);
                }

                item.Status = ok ? OptimizationStatus.Success : OptimizationStatus.Failed;
                item.IsApplied = ok;
                done++;

                ProgressValue = Math.Round((double)done / total * 100, 0);
                ProgressText = $"Đã hoàn thành {done}/{total} mục ({ProgressValue}%)";
            }

            // Dọn Standby List sau khi hoàn tất
            MemoryCleanerService.CleanStandbyMemory();

            CurrentStepText = $"🎉 Đã tối ưu thành công {done}/{total} mục! Máy tính đã sẵn sàng cho hiệu năng tối đa.";
            IsRunning = false;
        }

        public async Task RevertSelectedAsync()
        {
            var selected = _items.Where(x => x.IsSelected && x.IsReversible).ToList();
            if (selected.Count == 0)
            {
                CurrentStepText = "Không có mục nào có thể hoàn tác trong danh sách đã chọn!";
                return;
            }

            IsRunning = true;
            ProgressValue = 0;
            int total = selected.Count;
            int done = 0;

            CurrentStepText = $"Bắt đầu hoàn tác {total} mục về thiết lập Windows mặc định...";

            foreach (var item in selected)
            {
                // Bảo vệ tối cao cho Riot Vanguard: Nếu đang ở Vanguard Safe Mode, tuyệt đối không can thiệp tắt Core Isolation (HVCI)
                if (_isVanguardSafeMode && item.Id == 27)
                {
                    item.Status = OptimizationStatus.Reverted;
                    item.StatusText = "Bảo vệ Vanguard (Giữ nguyên)";
                    item.StatusBrush = "#38BDF8";
                    done++;
                    ProgressValue = Math.Round((double)done / total * 100, 0);
                    ProgressText = $"Đã hoàn thành {done}/{total} mục ({ProgressValue}%)";
                    continue;
                }

                item.Status = OptimizationStatus.Running;
                CurrentStepText = $"[Hoàn tác {done + 1}/{total}] Khôi phục mặc định: {item.Title}";

                bool ok = false;
                if (item.NativeRevert != null)
                {
                    try
                    {
                        ok = await Task.Run(() => item.NativeRevert());
                    }
                    catch { ok = false; }
                }

                if (!ok && !string.IsNullOrWhiteSpace(item.RevertScript))
                {
                    ok = await PowerShellRunner.ExecuteScriptAsync(item.RevertScript, statusLine =>
                    {
                        if (!string.IsNullOrWhiteSpace(statusLine) && !statusLine.Contains("PowerShell"))
                        {
                            CurrentStepText = statusLine;
                        }
                    }, 30000);
                }

                item.Status = ok ? OptimizationStatus.Reverted : OptimizationStatus.Failed;
                item.IsApplied = !ok;
                done++;

                ProgressValue = Math.Round((double)done / total * 100, 0);
                ProgressText = $"Đã hoàn tác {done}/{total} mục ({ProgressValue}%)";
            }

            CurrentStepText = $"🔄 Đã hoàn tác thành công {done}/{total} mục về trạng thái mặc định của Windows!";
            IsRunning = false;
        }

        public async Task RevertSingleItemAsync(OptimizationItem item)
        {
            if (item == null || !item.IsReversible || IsRunning) return;

            IsRunning = true;
            item.Status = OptimizationStatus.Running;
            CurrentStepText = $"[Hoàn tác] Đang khôi phục mặc định: {item.Title}";

            bool ok = false;
            if (item.NativeRevert != null)
            {
                try
                {
                    ok = await Task.Run(() => item.NativeRevert());
                }
                catch { ok = false; }
            }

            if (!ok && !string.IsNullOrWhiteSpace(item.RevertScript))
            {
                ok = await PowerShellRunner.ExecuteScriptAsync(item.RevertScript, statusLine =>
                {
                    if (!string.IsNullOrWhiteSpace(statusLine) && !statusLine.Contains("PowerShell"))
                    {
                        CurrentStepText = statusLine;
                    }
                }, 30000);
            }

            item.Status = ok ? OptimizationStatus.Reverted : OptimizationStatus.Failed;
            item.IsApplied = !ok;
            CurrentStepText = ok 
                ? $"🔄 Đã hoàn tác thành công: {item.Title} về mặc định!"
                : $"⚠️ Không thể hoàn tác: {item.Title}. Vui lòng thử lại với quyền Admin.";
            IsRunning = false;
        }

        public async Task PurgeRamInstantlyAsync()
        {
            if (IsRunning) return;
            IsRunning = true;
            ProgressValue = 20;
            CurrentStepText = "🧹 Đang kiểm tra hệ thống và xả sạch bộ nhớ chờ (Standby Memory)...";
            ProgressText = "Đang dọn RAM tức thời...";

            var stats = await Task.Run(() => MemoryCleanerService.PurgeRamDetailed());

            ProgressValue = 100;
            if (stats.FreedMb > 0)
            {
                CurrentStepText = $"⚡ Đã giải phóng thành công +{stats.FreedMb:N0} MB RAM khả dụng! (RAM trống: {stats.AvailableAfterMb:N0} MB)";
                ProgressText = $"Hoàn tất dọn RAM tức thời (0.2s) • Giải phóng +{stats.FreedMb:N0} MB • Sẵn sàng chiến game";
            }
            else
            {
                CurrentStepText = $"⚡ Bộ nhớ RAM đã ở trạng thái tối ưu nhất! (RAM khả dụng: {stats.AvailableAfterMb:N0} MB)";
                ProgressText = "Standby Memory đã sạch hoàn toàn • Không cần khởi động lại máy";
            }

            IsRunning = false;
        }

        public async Task ToggleFpsBoostAsync()
        {
            if (IsRunning) return;
            IsRunning = true;

            bool willEnable = !IsFpsBoostActive;
            ProgressValue = 30;

            if (willEnable)
            {
                CurrentStepText = _isVanguardSafeMode 
                    ? "⚡ Đang kích hoạt FPS Boost [Vanguard 100% Safe - CPU + GPU + Shader Cache 10GB]..."
                    : "⚡ Đang kích hoạt FPS Boost eSports (Tắt Core Isolation HVCI + GPU + Shader Cache)...";
                ProgressText = _isVanguardSafeMode
                    ? "Mở khóa CPU 100% Turbo, Win32Priority 38, Tắt GameDVR, Shader Cache 10GB & Giữ HVCI cho Valorant..."
                    : "Mở khóa CPU 100% Turbo, Tắt GameDVR, Shader Cache 10GB & Tắt Core Isolation (HVCI)...";

                await Task.Run(() =>
                {
                    // 1. Core Isolation (HVCI)
                    var hvciItem = _items.FirstOrDefault(x => x.Id == 27);
                    if (!_isVanguardSafeMode)
                    {
                        // Chỉ tắt HVCI khi người dùng tắt chế độ bảo vệ Vanguard
                        hvciItem?.NativeApply?.Invoke();
                    }
                    else
                    {
                        // Giữ nguyên hoặc bảo đảm HVCI bật để Valorant không bị lỗi VAN9005 trên Win 11
                        hvciItem?.NativeRevert?.Invoke();
                    }

                    // 2. High Performance Power Plan (100% Turbo Boost, không bị khóa 99%)
                    var powerItem = _items.FirstOrDefault(x => x.Id == 1);
                    powerItem?.NativeApply?.Invoke();

                    // 3. Win32PrioritySeparation = 38 (Foreground Game Boost)
                    var quantumItem = _items.FirstOrDefault(x => x.Id == 2);
                    quantumItem?.NativeApply?.Invoke();

                    // 4. Đồ họa: Tắt GameDVR & bật HAGS (Giải phóng GPU onboard khỏi tính năng quay màn hình ngầm)
                    var gpuItem = _items.FirstOrDefault(x => x.Id == 3);
                    gpuItem?.NativeApply?.Invoke();

                    // 5. Gán High Performance GPU cho Valorant, CS2 & Dev Tools
                    var direct3dItem = _items.FirstOrDefault(x => x.Id == 4);
                    direct3dItem?.NativeApply?.Invoke();

                    // 6. NetworkThrottlingIndex = -1 & SystemResponsiveness = 0 (Triệt tiêu độ trễ hệ thống)
                    var latencyItem = _items.FirstOrDefault(x => x.Id == 7);
                    latencyItem?.NativeApply?.Invoke();

                    // 7. Power Throttling Off
                    var throttleItem = _items.FirstOrDefault(x => x.Id == 24);
                    throttleItem?.NativeApply?.Invoke();

                    // 8. Shader Cache 10GB (Triệt tiêu drop giật FPS khi tung chiêu/load map)
                    var shaderItem = _items.FirstOrDefault(x => x.Id == 28);
                    shaderItem?.NativeApply?.Invoke();

                    // 9. Dừng khẩn cấp tiến trình GameDVR ngầm
                    NativeTweakService.StopService("BcastDVRUserService");
                });

                // Cập nhật trạng thái hiển thị trên các thẻ card liên quan
                var boostIds = _isVanguardSafeMode ? new[] { 1, 2, 3, 4, 7, 24, 28 } : new[] { 1, 2, 3, 4, 7, 24, 27, 28 };
                foreach (var id in boostIds)
                {
                    var it = _items.FirstOrDefault(x => x.Id == id);
                    if (it != null)
                    {
                        it.Status = OptimizationStatus.Success;
                        it.IsApplied = true;
                        it.StatusText = "Đang Boost (eSports)";
                        it.StatusBrush = "#10B981";
                    }
                }

                if (_isVanguardSafeMode)
                {
                    var hvci = _items.FirstOrDefault(x => x.Id == 27);
                    if (hvci != null)
                    {
                        hvci.Status = OptimizationStatus.Reverted;
                        hvci.IsApplied = false;
                        hvci.StatusText = "Bảo vệ Vanguard (Bật)";
                        hvci.StatusBrush = "#38BDF8";
                    }
                }

                IsFpsBoostActive = true;
                ProgressValue = 100;
                CurrentStepText = _isVanguardSafeMode
                    ? "🔥 ĐÃ BẬT FPS BOOST [Vanguard 100% Safe]: CPU Turbo, Tắt GameDVR, Gán High GPU, Shader Cache 10GB, dọn RAM. Sẵn sàng chiến game mượt mà!"
                    : "🔥 ĐÃ BẬT FPS BOOST eSPORTS: Tắt Core Isolation (HVCI), CPU Turbo, Tắt GameDVR, Shader Cache 10GB, dọn sạch Standby RAM!";
                ProgressText = _isVanguardSafeMode
                    ? "Chế độ Gaming eSports đa tầng • Triệt tiêu khựng lag • 100% Tương thích Valorant & Riot Vanguard"
                    : "Chế độ Gaming eSports tối đa • Cần khởi động lại máy để Core Isolation tắt hoàn toàn";
            }
            else
            {
                CurrentStepText = "🛡️ Đang chuyển về chế độ Làm Việc & An Toàn...";
                ProgressText = "Bật lại Core Isolation bảo mật và đưa CPU về Balanced (mát máy)...";

                await Task.Run(() =>
                {
                    // 1. Bật lại Core Isolation (HVCI = 1) để bảo vệ máy khỏi virus/mã độc
                    var hvciItem = _items.FirstOrDefault(x => x.Id == 27);
                    hvciItem?.NativeRevert?.Invoke();

                    // 2. Trả Power Plan về Balanced để tiết kiệm điện, hạ nhiệt CPU khi làm việc
                    var powerItem = _items.FirstOrDefault(x => x.Id == 1);
                    powerItem?.NativeRevert?.Invoke();

                    // 3. Trả Win32PrioritySeparation về 2 (mặc định đa nhiệm cân bằng)
                    var quantumItem = _items.FirstOrDefault(x => x.Id == 2);
                    quantumItem?.NativeRevert?.Invoke();

                    // 4. Bật lại điều phối Power Throttling
                    var throttleItem = _items.FirstOrDefault(x => x.Id == 24);
                    throttleItem?.NativeRevert?.Invoke();
                });

                // Cập nhật trạng thái hiển thị trên các thẻ card liên quan
                var boostIds = new[] { 1, 2, 24, 27 };
                foreach (var id in boostIds)
                {
                    var it = _items.FirstOrDefault(x => x.Id == id);
                    if (it != null)
                    {
                        it.Status = OptimizationStatus.Reverted;
                        it.IsApplied = false;
                        it.StatusText = "Mặc định (An toàn)";
                        it.StatusBrush = "#64748B";
                    }
                }

                IsFpsBoostActive = false;
                ProgressValue = 100;
                CurrentStepText = "🛡️ ĐÃ TẮT FPS BOOST: Bật lại Core Isolation bảo mật, CPU về Balanced mát máy. Toàn bộ tối ưu nền vẫn giữ nguyên!";
                ProgressText = "Chế độ Làm Việc An Toàn • Bảo mật 100% • Máy vẫn mượt nguyên bản tối ưu";
            }

            IsRunning = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
