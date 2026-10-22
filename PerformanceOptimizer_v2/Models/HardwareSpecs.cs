namespace PerformanceOptimizer.Models
{
    public class HardwareSpecs
    {
        // 1. CPU Information & Architecture
        public string CpuName { get; set; } = "Đang nhận diện...";
        public string CpuDetails { get; set; } = "Cores / Threads";
        public string CpuArchitectureNote { get; set; } = "Kiến trúc x64 chuẩn";
        
        // 2. Hardware Classification Flags (Adaptive Optimization Engine)
        public bool IsLaptop { get; set; } = false;
        public bool IsAmdProcessor { get; set; } = false;
        public bool IsAmdDualCcdX3D { get; set; } = false;
        public bool IsIntelHybrid { get; set; } = false;
        public bool HasDedicatedGpu { get; set; } = false;
        public double RamSizeGb { get; set; } = 16.0;

        // 3. GPU Information
        public string GpuName { get; set; } = "Đang nhận diện...";
        public string GpuDetails { get; set; } = "MSI Mode + HAGS Support";
        
        // 4. Memory & Disk
        public string RamTotal { get; set; } = "Đang nhận diện...";
        public string RamDetails { get; set; } = "Zero Disk Paging";
        public string DiskTotal { get; set; } = "Đang nhận diện...";
        public string DiskDetails { get; set; } = "Ổ đĩa hệ thống Windows";
        
        // 5. System Status
        public string OsVersion { get; set; } = "Windows 11";
        public string NetworkAdapter { get; set; } = "Đang nhận diện...";
        public string WindowsAiStatus { get; set; } = "Sẵn sàng tinh chỉnh";
        public string GamingSuitability { get; set; } = "100% Sẵn sàng cho Gaming & Esport";
    }
}
