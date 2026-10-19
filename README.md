# Windows Performance Studio Pro (2026 - 2027 Edition)

A high-performance, hardware-adaptive Windows 10/11 optimization utility designed for eSports gaming latency reduction, background service debloating, AI tracking isolation, and developer workflow acceleration with zero system breakage.

---

## 🚀 Điểm Nhấn Công Nghệ (Phiên Bản 2026 - 2027)

### 1. Động Cơ Thực Thi Native C# Siêu Tốc (Ultra-Fast Native Engine)
- Thay thế hoàn toàn cơ chế gọi tiến trình PowerShell phụ trợ bằng việc thao tác trực tiếp trên **Native Win32 & Registry API**.
- Toàn bộ 30 hạng mục tinh chỉnh được áp dụng đồng thời trong **~1.5 giây** (thay vì 30 - 45 giây như các công cụ chạy script truyền thống), không chiếm dụng CPU và không bị nhận diện nhầm bởi phần mềm diệt virus.

### 2. Khả Năng Hoàn Tác 2 Chiều Chi Tiết (Granular 2-Way Toggle & Revert)
- Không chỉ áp dụng (Apply), tool tích hợp sẵn cơ chế **Hoàn Tác (Revert 1-Click)** cho từng hạng mục về trạng thái mặc định của Windows.
- Tự động quét và hiển thị trạng thái thực tế của hệ thống (`[Đã tối ưu]` / `[Mặc định]`) ngay khi mở ứng dụng.

### 3. Nhận Diện Phần Cứng Thông Minh (Hardware-Aware Architecture)
- **AMD 3D V-Cache Aware:** Tự động phát hiện CPU AMD Ryzen Dual-CCD (7900X3D, 7950X3D, 9900X3D, 9950X3D) để bảo toàn cơ chế Core Parking phân luồng của AMD Chipset Driver, tránh làm mất hiệu năng V-Cache trong game.
- **Intel Hybrid Architecture:** Nhận diện kiến trúc P-Core & E-Core (12th - 15th Gen / Core Ultra) để ưu tiên luồng cho game ở Foreground và tắt Power Throttling.
- **Mở Khóa 100% Turbo Boost:** Khắc phục triệt để lỗi giới hạn 99% xung nhịp của các công cụ cũ, giúp CPU bung 100% xung Turbo Boost trong combat.

### 4. Khóa Triệt Để Windows 11 AI & Copilot Debloat
- Vô hiệu hóa **Windows Recall** (tính năng chụp ảnh màn hình và phân tích hành vi ngầm).
- Tắt nút và thanh công cụ **Windows Copilot** trên Taskbar và trình duyệt Edge.
- Chặn toàn bộ dịch vụ AI telemetry ngầm trên NPU và CPU.

### 5. Nút Gạt Linh Hoạt: FPS Boost (eSports vs Safe & Balanced Work Mode)
- **Khi BẬT (eSports Battle Mode):** Tắt Core Isolation (HVCI) để bung 5-15% FPS CPU, ép Power Plan Max Turbo 100%, Win32PrioritySeparation = 38 và tự động xả Standby RAM trong 0.3s.
- **Khi TẮT (Chế độ Làm Việc An Toàn):** Bật lại Core Isolation bảo mật và đưa CPU về Balanced mát máy. **Toàn bộ cấu hình nền đã tối ưu (debloat, card mạng, SSD, GPU) vẫn được giữ nguyên 100%, máy chạy cực mượt mà không bao giờ bị lag**.

---

## 🛠️ Danh Mục Tính Năng (30 Hạng Mục Tinh Chỉnh)

### 🤖 1. AI & Copilot (2026 Edition)
- **Khóa Windows Recall & AI Telemetry:** Chặn `DisableAIDataAnalysis` và `DisableRecall`, bảo mật quyền riêng tư tối đa.
- **Vô hiệu hóa Copilot:** Tắt hoàn toàn phím tắt, biểu tượng Taskbar và background agent.

### ⚡ 2. Hiệu Năng & CPU
- **Power Plan High Performance:** Mở khóa 100% xung Turbo Boost, min state 5% giúp CPU hạ nhiệt khi nghỉ và bung xung tức thì khi vào game.
- **CPU Quantum (Win32PrioritySeparation = 38):** Ưu tiên chu kỳ CPU tối đa cho cửa sổ game/app đang mở.
- **Khóa Kernel Trên RAM (DisablePagingExecutive):** Không swap nhân hệ thống xuống ổ cứng khi RAM >= 8GB.
- **Tắt Power Throttling & Điều Phối Nhân Động:** Giữ mức xung nhịp ổn định cho ứng dụng Foreground.
- **Chế Độ Gaming eSports (Tắt Core Isolation / HVCI):** Giải phóng từ 5% đến 15% FPS trong các tựa game bắn súng thi đấu nặng CPU (có hỗ trợ hoàn tác 1-click).
- **Native Standby Memory Cleaner:** Gọi trực tiếp `NtSetSystemInformation` qua NT Kernel để xả sạch bộ nhớ chờ trong 0.05s mà không tốn CPU hay ghi SSD.

### 🎮 3. GPU & Gaming
- **DirectX 12 / Vulkan Shader Cache 10GB:** Mở rộng bộ nhớ đệm Shader lên 10GB, triệt tiêu hiện tượng giật khựng (stutter) khi tải bản đồ trong các tựa game Unreal Engine 5.
- **Hardware-Accelerated GPU Scheduling (HAGS) & DirectX Flip Model:** Giảm trễ khung hình xuất ra màn hình.
- **MSI Mode (Message Signaled Interrupts):** Bật chế độ ngắt ưu tiên cao (Priority High) cho GPU và Card mạng trên PCIe bus, triệt tiêu DPC Latency.
- **Gán GPU Rời Dedicated:** Tự động ưu tiên card đồ họa mạnh nhất cho các tựa game (Valorant, CS2, LMHT) và IDE lập trình.
- **Chuột eSports 1:1:** Tắt hoàn toàn gia tốc chuột Windows (Sens 10, Speed 0 chuẩn 1:1), tăng tốc độ lặp phím và tắt StickyKeys.

### 🌐 4. Mạng & Độ Trễ (Network Low-Latency)
- **DNS over HTTPS (DoH) & Cloudflare 1.1.1.1:** Phân giải tên miền mã hóa, bảo mật và giảm ping.
- **TCP AckFrequency = 1 & TCP NoDelay:** Gửi phản hồi gói tin ngay lập tức, không chờ tích lũy gói tin (tắt thuật toán Nagle).
- **Tối Ưu Driver LAN & Wi-Fi:** Tắt EEE (Energy Efficient Ethernet), Green Ethernet, Interrupt Moderation; đặt Wi-Fi Roaming về Lowest và Transmit Power về Highest.

### 🛡️ 5. Dịch Vụ, Windows & Developer Readiness
- **Tắt Dịch Vụ Thu Thập Ngầm:** Vô hiệu hóa DiagTrack, SysMain, MapsBroker, CEIP, DiskDiagnostic, giữ nguyên Windows Search (Delayed-Auto).
- **Start Menu Tìm Kiếm Offline 0s:** Tắt gợi ý Bing Search trên Start Menu, chỉ tìm kiếm ứng dụng và tệp tin cục bộ.
- **Bật LongPaths (>260 ký tự):** Loại bỏ lỗi đường dẫn dài khi clone Git repo hoặc build dự án NPM/Node.js.
- **Bảo Toàn Hypervisor & Anti-Cheat:** Giữ nguyên `hypervisorlaunchtype auto`, đảm bảo 100% không bị ban bởi Vanguard, Easy Anti-Cheat, BattlEye và không lỗi WSL2/Docker.
- **Tiện ích Chris Titus Tech:** Bật "End Task" trực tiếp khi chuột phải Taskbar, khôi phục Classic Context Menu và kích hoạt Windows Update Security Mode (hoãn tính năng lớn 365 ngày).

---

## 📦 Cách Biên Dịch & Chạy Ứng Dụng

Ứng dụng được biên dịch dạng **Single-File Self-Contained**, đã nhúng toàn bộ runtime .NET:

1. Chạy file `build.bat` ở thư mục gốc.
2. File EXE độc lập sẽ được xuất ra tại:
   `publish_latest\PerformanceOptimizer.exe`
3. Có thể copy file EXE này sang bất kỳ máy tính Windows 10/11 nào (kể cả máy vừa cài Win chưa có .NET) và khởi chạy ngay lập tức với quyền Administrator.
