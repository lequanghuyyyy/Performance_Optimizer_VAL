using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PerformanceOptimizer.Models
{
    public enum OptimizationStatus
    {
        Pending,
        Running,
        Success,
        Failed,
        Reverted
    }

    public class OptimizationItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private OptimizationStatus _status = OptimizationStatus.Pending;
        private string _statusText = "Sẵn sàng";
        private string _statusBrush = "#64748B";
        private bool _isApplied = false;

        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Badges { get; set; } = new();
        public string Script { get; set; } = string.Empty;
        public string RevertScript { get; set; } = string.Empty;
        public string Icon { get; set; } = "⚡";

        // Native C# high-speed delegates (zero process overhead)
        public Func<bool>? NativeApply { get; set; }
        public Func<bool>? NativeRevert { get; set; }
        public Func<bool>? CheckStatus { get; set; }

        public bool IsReversible => NativeRevert != null || !string.IsNullOrWhiteSpace(RevertScript);

        public bool CanRevert => IsReversible && _isApplied && _status != OptimizationStatus.Reverted;

        public bool IsApplied
        {
            get => _isApplied;
            set
            {
                if (_isApplied != value)
                {
                    _isApplied = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanRevert));
                    if (_status == OptimizationStatus.Pending)
                    {
                        if (_isApplied)
                        {
                            StatusText = "Đã tối ưu";
                            StatusBrush = "#10B981"; // Emerald
                        }
                        else
                        {
                            StatusText = "Mặc định";
                            StatusBrush = "#64748B"; // Slate
                        }
                    }
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public OptimizationStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    UpdateStatusAppearance();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanRevert));
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusBrush
        {
            get => _statusBrush;
            set
            {
                if (_statusBrush != value)
                {
                    _statusBrush = value;
                    OnPropertyChanged();
                }
            }
        }

        private void UpdateStatusAppearance()
        {
            switch (_status)
            {
                case OptimizationStatus.Running:
                    StatusText = "Đang xử lý...";
                    StatusBrush = "#38BDF8"; // Cyan
                    break;
                case OptimizationStatus.Success:
                    StatusText = "Đã áp dụng";
                    StatusBrush = "#10B981"; // Emerald green
                    _isApplied = true;
                    OnPropertyChanged(nameof(IsApplied));
                    break;
                case OptimizationStatus.Reverted:
                    StatusText = "Mặc định (An toàn)";
                    StatusBrush = "#64748B"; // Slate
                    _isApplied = false;
                    OnPropertyChanged(nameof(IsApplied));
                    break;
                case OptimizationStatus.Failed:
                    StatusText = "Bỏ qua / Lỗi";
                    StatusBrush = "#EF4444"; // Red
                    break;
                default:
                    if (_isApplied)
                    {
                        StatusText = "Đã tối ưu";
                        StatusBrush = "#10B981";
                    }
                    else
                    {
                        StatusText = "Mặc định";
                        StatusBrush = "#64748B";
                    }
                    break;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
