# BÁO CÁO ĐỐI SOÁT & ĐO LƯỜNG HIỆU NĂNG THU THẬP SỐ LIỆU (TASK LG-A-004)

## TRẠNG THÁI NGHIỆM THU HIỆN TẠI

> **A-004: DONE / ACCEPTED / CLOSED**
>
> Nghiệm thu chính thức hoàn thành: 18/18 A-004 xUnit tests PASS, 34/34 Agent tests PASS, 141/141 toàn dự án PASS, và đối soát tự động 10 mẫu với Windows Reference (PerformanceCounter CPU + WMI/CIM RAM) đạt 10/10 PASS.

---

## 1. Môi trường kiểm thử thực tế (Execution Environment)

- **Hệ điều hành:** Windows 11 Pro (OS Build: 10.0.26200.0)
- **Tên máy:** DESKTOP-OCQVMU6
- **Kiến trúc CPU:** x64
- **Nền tảng thực thi:** .NET 9.0 (C# 13, Native P/Invoke qua `kernel32.dll`)
- **Thời điểm đo lường:** 2026-09-27T15:54:02+07:00

---

## 2. ĐO LƯỜNG THỰC TẾ — PRODUCTION C# WindowsMetricsCollector

> **Tất cả dữ liệu dưới đây được thu thập bằng lớp `WindowsMetricsCollector` thực tế trong production code (`src/LabGuard.Agent/MetricsCollector.cs`).**
>
> Harness đo lường gọi trực tiếp `ReadCpuPercent()`, `ReadMemoryBytes()`, `ReadUptimeSeconds()` trên cùng đối tượng `WindowsMetricsCollector` được sử dụng trong production — **không tái hiện lại công thức Win32**.
>
> Harness tạm thời đặt ngoài Git worktree (`C:\Temp\LG_A004_Harness\`), không được commit vào repository.

### 2.1. Kết quả đo lường (7 mẫu, khoảng cách 1.2s/mẫu)

| # | Timestamp (với múi giờ chính xác) | CPU (%) | RAM Tổng (GB) | RAM Đã dùng (GB) | RAM (%) | Uptime (giây) | Độ trễ gọi (ms) |
|:---:|:---|:---:|:---:|:---:|:---:|:---:|:---:|
| 1 | 2026-09-27T15:54:02.4054304+07:00 | 41.67% | 15.356 | 12.444 | 81.04% | 89,678 | **1.137** |
| 2 | 2026-09-27T15:54:03.6292122+07:00 | 19.53% | 15.356 | 12.453 | 81.10% | 89,679 | **0.026** |
| 3 | 2026-09-27T15:54:04.8439896+07:00 | 22.51% | 15.356 | 12.532 | 81.61% | 89,680 | **0.086** |
| 4 | 2026-09-27T15:54:06.0439097+07:00 | 23.35% | 15.356 | 12.435 | 80.98% | 89,681 | **0.103** |
| 5 | 2026-09-27T15:54:07.2548919+07:00 | 32.18% | 15.356 | 12.367 | 80.54% | 89,682 | **0.332** |
| 6 | 2026-09-27T15:54:08.4699509+07:00 | 31.98% | 15.356 | 12.382 | 80.64% | 89,684 | **0.165** |
| 7 | 2026-09-27T15:54:09.6723236+07:00 | 23.45% | 15.356 | 12.418 | 80.87% | 89,685 | **0.192** |

**Lưu ý múi giờ:** Tất cả timestamp ghi với offset `+07:00` (giờ Việt Nam, ICT). Không dùng hậu tố `Z` cho giờ địa phương.

> Measured production `WindowsMetricsCollector` latency:
> - First JIT/warm-up sample: **1.137 ms** (includes JIT compilation and `WindowsMetricsCollector` constructor priming of the initial `GetSystemTimes` CPU snapshot).
> - Subsequent measured samples: **0.026–0.332 ms**.
> - All measured samples satisfy the WBS < 100 ms requirement. *(Tất cả các mẫu đo thực tế đều thỏa mãn yêu cầu < 100 ms của WBS).*

### 2.2. Đánh giá tiêu chí độ trễ (Latency Budget)

- **Ngưỡng chấp nhận WBS:** Thời gian thu thập mỗi chu kỳ < 100 ms.
- **Thực tế đo được trên production WindowsMetricsCollector:**
  - Mẫu 1 (JIT/warm-up): 1.137 ms.
  - Các mẫu đo tiếp theo (mẫu 2–7): 0.026 – 0.332 ms.
  - Tất cả các mẫu đo đều thỏa mãn yêu cầu < 100 ms của WBS.
- **Kết luận:** ✅ **PASS** — Đạt tiêu chí thời gian thu thập.

---

## 3. ĐỐI SOÁT HỆ ĐIỀU HÀNH BỔ SUNG — PowerShell / CIM Cross-Check

> **Đây là kiểm tra chéo độc lập sử dụng công cụ PowerShell của Windows, KHÔNG phải nguồn dữ liệu chính thức.**
> Dữ liệu chính thức là Mục 2 (production C# collector).

```powershell
# CPU Processor Time (đo tại cùng thời điểm kiểm thử)
(Get-Counter '\Processor(_Total)\% Processor Time').CounterSamples[0].CookedValue
# Kết quả quan sát: ~20–35% (phù hợp với khoảng CPU đo được)

# Bộ nhớ vật lý hệ thống
Get-CimInstance Win32_OperatingSystem | Select-Object TotalVisibleMemorySize, FreePhysicalMemory
# Kết quả quan sát: Tổng RAM ~15,724 MB, Đang dùng ~12,400–12,550 MB (~79–80%)
```

- **Nhận xét:** Các giá trị từ `WindowsMetricsCollector` phù hợp với quan sát PowerShell trong phạm vi dao động tải động thời gian thực.
- **Lưu ý về WMI/CIM:** WMI/CIM chỉ được dùng để kiểm tra chéo độc lập từ công cụ dòng lệnh, không được chọn làm giải pháp chính trong production của A-004 nhằm tránh thêm tầng truy vấn quản trị. Không có phép đo benchmark chính thức nào đối với WMI được thực hiện trong A-004, do đó tài liệu không đưa ra bất kỳ con số độ trễ định lượng nào cho WMI.

---

## 4. ĐỐI SOÁT TỰ ĐỘNG THAM CHIẾU HỆ ĐIỀU HÀNH WINDOWS (AUTOMATED WINDOWS REFERENCE ACCEPTANCE)

> ✅ **TIÊU CHÍ NGHIỆM THU ĐÃ ĐẠT: ĐỐI SOÁT TỰ ĐỘNG 10/10 MẪU PASS**
>
> **Lưu ý phương pháp luận:** Đặc tả WBS ban đầu yêu cầu đối chiếu thủ công với Task Manager. Nhằm đảm bảo tính khách quan, khả năng tái lập và loại bỏ sai số do quan sát thủ công không đồng bộ, nghiệm thu định lượng chính thức được thực hiện tự động qua đối chiếu với hai nguồn tham chiếu độc lập của Windows:
> 1. **CPU Reference:** Windows `PerformanceCounter("Processor", "% Processor Time", "_Total")`.
> 2. **RAM Reference:** Windows WMI/CIM `Win32_OperatingSystem` (`TotalVisibleMemorySize`, `FreePhysicalMemory`).
>
> Ảnh chụp màn hình Task Manager được sử dụng như một bước kiểm tra trực quan bổ trợ (visual sanity check).

### 4.1. Môi trường kiểm thử & Tham số đối soát
- **Máy kiểm thử:** `DESKTOP-OCQVMU6` | Windows 11 Build `10.0.26200.0` | .NET `9.0.20`
- **Khoảng cách lấy mẫu:** ~5.000 ms (5 giây / mẫu).
- **Tiêu chuẩn nghiệm thu:**
  - CPU absolute difference <= 5.0 percentage points.
  - RAM absolute difference <= 5.0 percentage points.
  - LabGuard collection elapsed time < 100 ms.
  - Yêu cầu: 10/10 mẫu đạt chuẩn liên tục.

### 4.2. Bảng kết quả đối soát tự động 10 mẫu thực tế

| # | Timestamp (+07:00) | LabGuard CPU % | Ref CPU % | CPU Diff (pp) | LabGuard RAM % | Ref RAM % | RAM Diff (pp) | Uptime (s) | Độ trễ (ms) | Kết quả |
|:---:|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| 1 | 2026-09-27T22:00:09.2503741+07:00 | 12.55% | 12.44% | 0.12 pp | 87.12% | 87.24% | 0.12 pp | 111,646 | 0.616 | **PASS** |
| 2 | 2026-09-27T22:00:14.5278461+07:00 | 10.64% | 10.82% | 0.18 pp | 87.52% | 87.52% | 0.00 pp | 111,651 | 0.076 | **PASS** |
| 3 | 2026-09-27T22:00:19.5532826+07:00 | 11.30% | 11.34% | 0.04 pp | 87.82% | 87.82% | 0.00 pp | 111,656 | 0.173 | **PASS** |
| 4 | 2026-09-27T22:00:24.5694444+07:00 | 11.71% | 11.72% | 0.01 pp | 87.65% | 87.65% | 0.00 pp | 111,661 | 0.074 | **PASS** |
| 5 | 2026-09-27T22:00:29.5885527+07:00 | 16.77% | 16.56% | 0.21 pp | 87.59% | 87.59% | 0.00 pp | 111,666 | 0.106 | **PASS** |
| 6 | 2026-09-27T22:00:34.6144526+07:00 | 9.69% | 10.00% | 0.31 pp | 87.61% | 87.61% | 0.00 pp | 111,671 | 0.117 | **PASS** |
| 7 | 2026-09-27T22:00:39.6320949+07:00 | 13.16% | 13.04% | 0.12 pp | 87.93% | 87.94% | 0.01 pp | 111,676 | 0.099 | **PASS** |
| 8 | 2026-09-27T22:00:44.6539846+07:00 | 10.61% | 10.60% | 0.01 pp | 87.66% | 87.67% | 0.00 pp | 111,681 | 0.248 | **PASS** |
| 9 | 2026-09-27T22:00:49.6815561+07:00 | 10.79% | 10.75% | 0.04 pp | 87.53% | 87.54% | 0.01 pp | 111,686 | 0.117 | **PASS** |
| 10 | 2026-09-27T22:00:54.7025038+07:00 | 12.56% | 12.58% | 0.02 pp | 87.58% | 87.58% | 0.00 pp | 111,691 | 0.211 | **PASS** |

### 4.3. Thống kê & Đánh giá nghiệm thu
- **Độ lệch CPU (pp):** Min = 0.005 pp | Max = 0.305 pp | Trung bình = 0.106 pp (Ngưỡng cho phép: <= 5.0 pp)
- **Độ lệch RAM (pp):** Min = 0.000 pp | Max = 0.125 pp | Trung bình = 0.016 pp (Ngưỡng cho phép: <= 5.0 pp)
- **Độ trễ thu thập LabGuard lớn nhất:** 0.616 ms (Ngưỡng cho phép: < 100 ms)
- **Kết quả nghiệm thu:** **10 / 10 mẫu PASS (100%)**. Tiêu chí nghiệm thu LG-A-004 chính thức hoàn thành và đóng lại (**ACCEPTED / CLOSED**).

---

## 5. Ghi chú về bộ kiểm thử tự động (Automated Test Summary)

| File kiểm thử | Số xUnit test case | Ghi chú |
|:---|:---:|:---|
| `MetricsCollectorTests.cs` | **10** | 8 phương thức test: 7 `[Fact]` + 1 `[Theory]` với 3 `[InlineData]` |
| `MetricsServiceTests.cs` | **4** | Kiểm thử chu kỳ gửi METRIC_BATCH qua loopback, BootId/BatchId, cancellation, fail-fast |
| `HeartbeatServiceTests.cs` | **4** | Kiểm thử HEARTBEAT envelope, SequenceNumber đơn điệu, stress đồng thời, cancellation |
| **A-004 tổng cộng** | **18** | |
| A-003 (từ trước) | 16 | `AgentSocketClientTests.cs` |
| **Agent.Tests tổng cộng** | **34** | ✅ PASS 100% |
| Protocol.Tests | 107 | ✅ PASS 100% |
| **TỔNG TẤT CẢ** | **141** | ✅ PASS 100% |

Kết quả `dotnet test`:
```
Passed! - Failed: 0, Passed: 107, Skipped: 0, Total: 107 — LabGuard.Protocol.Tests.dll
Passed! - Failed: 0, Passed:  34, Skipped: 0, Total:  34 — LabGuard.Agent.Tests.dll
```
