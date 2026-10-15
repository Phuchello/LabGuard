using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace LabGuard.Agent;

/// <summary>
/// Ảnh chụp bộ nhớ RAM vật lý của hệ thống (A-004).
/// </summary>
public readonly record struct MemorySnapshot(
    ulong TotalPhysicalBytes,
    ulong AvailablePhysicalBytes,
    ulong UsedPhysicalBytes);

/// <summary>
/// Hợp đồng thu thập chỉ số tài nguyên hệ thống (A-004):
/// CPU utilization (%), RAM physical bytes, và System Uptime (giây).
/// </summary>
public interface IMetricsCollector
{
    /// <summary>
    /// Đọc mức sử dụng CPU hiện tại (0.0% đến 100.0%).
    /// Tính theo delta giữa 2 thời điểm lấy mẫu mà không cần Thread.Sleep.
    /// </summary>
    double ReadCpuPercent();

    /// <summary>
    /// Đọc dung lượng RAM vật lý (Tổng, Còn trống, Đã dùng).
    /// </summary>
    MemorySnapshot ReadMemoryBytes();

    /// <summary>
    /// Đọc thời gian hệ điều hành đã khởi động và hoạt động liên tục (giây).
    /// </summary>
    long ReadUptimeSeconds();
}

/// <summary>
/// Giao diện nội bộ trừu tượng hóa các lệnh gọi Win32 API.
/// Cho phép unit test kiểm chứng chính xác các giải thuật và trường hợp biên (deterministic testing).
/// </summary>
internal interface ISystemMetricsNativeProvider
{
    /// <summary>
    /// Lấy bộ thời gian CPU: Idle, Kernel (bao gồm Idle), và User.
    /// </summary>
    bool GetSystemTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime);

    /// <summary>
    /// Lấy trạng thái dung lượng bộ nhớ RAM vật lý.
    /// </summary>
    bool GlobalMemoryStatusEx(out ulong totalPhysical, out ulong availablePhysical);

    /// <summary>
    /// Lấy số mili-giây kể từ khi máy tính khởi động.
    /// </summary>
    ulong GetTickCount64();
}

/// <summary>
/// Triển khai thực tế trên Windows sử dụng trực tiếp các hàm Win32 API qua P/Invoke.
/// - CPU: kernel32!GetSystemTimes
/// - RAM: kernel32!GlobalMemoryStatusEx
/// - Uptime: kernel32!GetTickCount64
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsSystemMetricsNativeProvider : ISystemMetricsNativeProvider
{
    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;

        public ulong ToUInt64() => ((ulong)dwHighDateTime << 32) | dwLowDateTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", EntryPoint = "GetSystemTimes", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool NativeGetSystemTimes(
        out FILETIME lpIdleTime,
        out FILETIME lpKernelTime,
        out FILETIME lpUserTime);

    [DllImport("kernel32.dll", EntryPoint = "GlobalMemoryStatusEx", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool NativeGlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll", EntryPoint = "GetTickCount64")]
    private static extern ulong NativeGetTickCount64();

    public bool GetSystemTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime)
    {
        if (!NativeGetSystemTimes(out var idle, out var kernel, out var user))
        {
            int errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(errorCode, $"GetSystemTimes failed with Win32 error code {errorCode}.");
        }

        idleTime = idle.ToUInt64();
        kernelTime = kernel.ToUInt64();
        userTime = user.ToUInt64();
        return true;
    }

    public bool GlobalMemoryStatusEx(out ulong totalPhysical, out ulong availablePhysical)
    {
        var memStatus = new MEMORYSTATUSEX();
        memStatus.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();

        if (!NativeGlobalMemoryStatusEx(ref memStatus))
        {
            int errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(errorCode, $"GlobalMemoryStatusEx failed with Win32 error code {errorCode}.");
        }

        totalPhysical = memStatus.ullTotalPhys;
        availablePhysical = memStatus.ullAvailPhys;
        return true;
    }

    public ulong GetTickCount64()
    {
        return NativeGetTickCount64();
    }
}

/// <summary>
/// Bộ thu thập chỉ số CPU, RAM, Uptime trên máy trạm Windows (Task LG-A-004).
/// Thu thập cực nhanh (&lt; 100ms), không gây nghẽn luồng và không phụ thuộc thư viện bên ngoài.
/// </summary>
public sealed class WindowsMetricsCollector : IMetricsCollector
{
    private readonly ISystemMetricsNativeProvider _nativeProvider;
    private readonly object _cpuLock = new();

    private ulong _prevIdleTime;
    private ulong _prevKernelTime;
    private ulong _prevUserTime;
    private bool _isCpuPrimed;

    internal WindowsMetricsCollector(ISystemMetricsNativeProvider nativeProvider)
    {
        _nativeProvider = nativeProvider ?? throw new ArgumentNullException(nameof(nativeProvider));
        PrimeCpuSnapshot();
    }

    /// <summary>
    /// Khởi tạo WindowsMetricsCollector với triển khai Win32 API mặc định.
    /// Tự động lấy mẫu CPU đầu tiên để sẵn sàng tính delta cho các lần gọi sau.
    /// </summary>
    public WindowsMetricsCollector()
        : this(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new WindowsSystemMetricsNativeProvider()
            : throw new PlatformNotSupportedException("WindowsMetricsCollector chỉ hỗ trợ hệ điều hành Windows."))
    {
    }

    private void PrimeCpuSnapshot()
    {
        lock (_cpuLock)
        {
            if (_nativeProvider.GetSystemTimes(out ulong idle, out ulong kernel, out ulong user))
            {
                _prevIdleTime = idle;
                _prevKernelTime = kernel;
                _prevUserTime = user;
                _isCpuPrimed = true;
            }
        }
    }

    /// <summary>
    /// Đọc mức sử dụng CPU (%) tính từ lần đọc trước:
    /// totalDelta = (kernelNow - kernelPrev) + (userNow - userPrev)
    /// idleDelta = idleNow - idlePrev
    /// busyDelta = totalDelta - idleDelta
    /// cpuPercent = (busyDelta / totalDelta) * 100%
    /// Ràng buộc kết quả trong đoạn [0.0, 100.0].
    /// </summary>
    public double ReadCpuPercent()
    {
        lock (_cpuLock)
        {
            if (!_nativeProvider.GetSystemTimes(out ulong idleNow, out ulong kernelNow, out ulong userNow))
            {
                throw new InvalidOperationException("Không thể đọc chỉ số CPU từ GetSystemTimes.");
            }

            if (!_isCpuPrimed)
            {
                _prevIdleTime = idleNow;
                _prevKernelTime = kernelNow;
                _prevUserTime = userNow;
                _isCpuPrimed = true;
                return 0.0;
            }

            ulong kernelDelta = kernelNow >= _prevKernelTime ? kernelNow - _prevKernelTime : 0;
            ulong userDelta = userNow >= _prevUserTime ? userNow - _prevUserTime : 0;
            ulong idleDelta = idleNow >= _prevIdleTime ? idleNow - _prevIdleTime : 0;

            _prevKernelTime = kernelNow;
            _prevUserTime = userNow;
            _prevIdleTime = idleNow;

            // Windows GetSystemTimes: kernelTime ĐÃ BAO GỒM idleTime.
            // Do đó tổng chu kỳ hệ thống = kernelDelta + userDelta
            ulong totalDelta = kernelDelta + userDelta;

            if (totalDelta == 0)
            {
                return 0.0;
            }

            // Thời gian bận (busy) = tổng thời gian - thời gian rảnh rỗi (idle)
            ulong busyDelta = totalDelta > idleDelta ? totalDelta - idleDelta : 0;

            double percent = ((double)busyDelta / totalDelta) * 100.0;

            return Math.Clamp(percent, 0.0, 100.0);
        }
    }

    /// <summary>
    /// Đọc thông tin RAM vật lý:
    /// UsedPhysicalBytes = TotalPhysicalBytes - AvailablePhysicalBytes
    /// </summary>
    public MemorySnapshot ReadMemoryBytes()
    {
        if (!_nativeProvider.GlobalMemoryStatusEx(out ulong totalPhys, out ulong availPhys))
        {
            throw new InvalidOperationException("Không thể đọc thông tin RAM từ GlobalMemoryStatusEx.");
        }

        ulong usedPhys = totalPhys >= availPhys ? totalPhys - availPhys : 0;
        return new MemorySnapshot(totalPhys, availPhys, usedPhys);
    }

    /// <summary>
    /// Đọc thời gian Uptime của máy tính (giây):
    /// uptimeSeconds = GetTickCount64() / 1000
    /// </summary>
    public long ReadUptimeSeconds()
    {
        ulong tickCountMs = _nativeProvider.GetTickCount64();
        return (long)(tickCountMs / 1000);
    }
}
