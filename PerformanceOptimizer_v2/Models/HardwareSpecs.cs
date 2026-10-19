namespace PerformanceOptimizer.Models
{
    public class HardwareSpecs
    {
        public string CpuName { get; set; } = "Đang nhận diện...";
        public string CpuDetails { get; set; } = "Cores / Threads";
        public string CpuArchitectureNote { get; set; } = "Kiến trúc x64 chuẩn";
        public bool IsAmdDualCcdX3D { get; set; } = false;
        public bool IsIntelHybrid { get; set; } = false;

        public string GpuName { get; set; } = "Đang nhận diện...";
        public string GpuDetails { get; set; } = "MSI Mode + HAGS Support";
        
        public string RamTotal { get; set; } = "Đang nhận diện...";
        public string RamDetails { get; set; } = "Zero Disk Paging";
        
        public string DiskTotal { get; set; } = "Đang nhận diện...";
        public string DiskDetails { get; set; } = "Ổ đĩa hệ thống Windows";
        
        public string OsVersion { get; set; } = "Windows 11";
        public string NetworkAdapter { get; set; } = "Đang nhận diện...";
        public string WindowsAiStatus { get; set; } = "Sẵn sàng tinh chỉnh";
        public string GamingSuitability { get; set; } = "100% Sẵn sàng cho Gaming & Esport";
    }
}
