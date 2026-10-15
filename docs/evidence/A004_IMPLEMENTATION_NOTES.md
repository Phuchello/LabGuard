# TỔNG KẾT KỸ THUẬT & GHI CHÚ KIẾN TRÚC NHIỆM VỤ LG-A-004
*(Engineering Notebook & Architecture Decisions for Final Agent Report)*

---

## 1. Yêu cầu nghiệp vụ từ WBS (Requirement from WBS)
- **Mã tác vụ:** LG-A-004 (Độ ưu tiên: P0 Core).
- **Trạng thái hiện tại:** `DONE / ACCEPTED / CLOSED` (Nghiệm thu chính thức hoàn tất: 18/18 A-004 xUnit cases PASS, đối soát tự động Windows Reference 10/10 PASS).
- **Mô tả:** Thu thập định kỳ chỉ số tài nguyên máy trạm Windows (CPU, RAM, Uptime hệ thống) mỗi 5 giây mà không gây nghẽn luồng.
- **Tiêu chí nghiệm thu (Acceptance Criteria):**
  - Thời gian thu thập mỗi chu kỳ < 100 ms *(Thực tế đo trên production WindowsMetricsCollector: mẫu warm-up đầu 1.137 ms, các mẫu tiếp theo 0.026–0.332 ms; trong phiên đối soát tự động max 0.616 ms — tất cả các mẫu đều đạt tiêu chí < 100 ms — PASS)*.
  - Sai lệch CPU/RAM so với nguồn tham chiếu độc lập của Windows (PerformanceCounter CPU, WMI RAM) <= 5.0 percentage points *(Đối soát tự động 10/10 mẫu đạt chuẩn liên tục: CPU lệch trung bình 0.106 pp, max 0.305 pp; RAM lệch trung bình 0.016 pp, max 0.125 pp — PASS)*.
  - Tích hợp phát các bản tin `METRIC_BATCH` và `HEARTBEAT` chuẩn qua tầng mạng A-003 *(Kiểm thử tự động loopback — PASS)*.

---

## 2. Các tệp mã nguồn triển khai (Files Implemented)
1. **`src/LabGuard.Agent/MetricsCollector.cs`:**
   - Định nghĩa `MemorySnapshot`, `IMetricsCollector`, `ISystemMetricsNativeProvider`.
   - Lớp `WindowsMetricsCollector` và `WindowsSystemMetricsNativeProvider` (P/Invoke `kernel32.dll`).
2. **`src/LabGuard.Agent/MetricsService.cs`:**
   - Dịch vụ định kỳ 5s (`PeriodicTimer`), tạo `MetricBatchPayload`, đóng phong bì `WireEnvelope<MetricBatchPayload>` và đưa vào hàng đợi `AgentSocketClient`.
3. **`src/LabGuard.Agent/HeartbeatService.cs`:**
   - Dịch vụ định kỳ 5s (`PeriodicTimer`), tăng dần `SequenceNumber`, đóng gói `HeartbeatPayload` và gửi qua `AgentSocketClient`.
4. **`src/LabGuard.Agent/Program.cs`:**
   - Khung sườn hiển thị trạng thái tiến độ của Agent (A-001/A-002/A-003 đã nghiệm thu, A-004 đã hiện thực mã nguồn).
5. **Bộ kiểm thử tự động:**
   - `tests/LabGuard.Agent.Tests/FakeNativeMetricsProvider.cs`
   - `tests/LabGuard.Agent.Tests/MetricsCollectorTests.cs` — **10 xUnit cases** (8 test methods: 7 `[Fact]` + 1 `[Theory]` với 3 `[InlineData]` rows — kiểm thử công thức CPU delta, clamping [0,100], RAM used, Uptime, Windows smoke test, native error handling, performance latency)
   - `tests/LabGuard.Agent.Tests/MetricsServiceTests.cs` — **4 xUnit cases** (valid METRIC_BATCH qua FakeServer, BootId ổn định / BatchId độc lập, cancellation, fail-fast khi chưa kết nối)
   - `tests/LabGuard.Agent.Tests/HeartbeatServiceTests.cs` — **4 xUnit cases** (valid HEARTBEAT envelope, SequenceNumber đơn điệu, concurrent stress, cancellation)


---

## 3. Luồng dữ liệu thời gian chạy (Runtime Flow)
```text
[Windows Kernel (kernel32)]
       |
       v (P/Invoke: GetSystemTimes, GlobalMemoryStatusEx, GetTickCount64)
[WindowsMetricsCollector]
       |
       +---> [MetricsService (5s PeriodicTimer)] ---> WireEnvelope<MetricBatchPayload> ---\
       |                                                                                   +---> [AgentSocketClient.Outbox] (Channel<byte[]>, Cap=256)
       +---> [HeartbeatService (5s PeriodicTimer)] -> WireEnvelope<HeartbeatPayload> ----/              |
                                                                                                        v
                                                                                              [SINGLE-WRITER LOOP]
                                                                                                        |
                                                                                                        v
                                                                                                  [NetworkStream]
```

---

## 4. Các Win32 APIs được sử dụng (Win32 APIs Used)
- **CPU:** `kernel32!GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime)`
- **RAM:** `kernel32!GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer)`
- **Uptime:** `kernel32!GetTickCount64()`

---

## 5. Công thức tính toán CPU (CPU Formula)
Windows `GetSystemTimes` trả về thời gian dạng `FILETIME` (độ phân giải 100ns). Cần lưu ý rằng trong hệ điều hành Windows, **`kernelTime` đã bao gồm cả `idleTime`**.
```text
kernelDelta = kernelNow - kernelPrevious
userDelta   = userNow   - userPrevious
idleDelta   = idleNow   - idlePrevious

totalDelta  = kernelDelta + userDelta
busyDelta   = totalDelta - idleDelta (nếu totalDelta > idleDelta, ngược lại 0)

cpuPercent  = (busyDelta / totalDelta) * 100.0
clamp range: [0.0, 100.0]
```
- Khởi tạo lần đầu: Collector thực hiện "prime" một mẫu ban đầu ngay trong hàm tạo constructor để lần gọi `ReadCpuPercent()` tiếp theo có delta chuẩn xác mà không cần gọi `Thread.Sleep()`.

---

## 6. Công thức tính toán RAM (RAM Formula)
Sử dụng cấu trúc `MEMORYSTATUSEX` đo RAM vật lý thực tế:
```text
TotalPhysicalBytes     = lpBuffer.ullTotalPhys
AvailablePhysicalBytes = lpBuffer.ullAvailPhys
UsedPhysicalBytes      = TotalPhysicalBytes - AvailablePhysicalBytes

ramPercent             = (UsedPhysicalBytes / TotalPhysicalBytes) * 100.0
clamp range: [0.0, 100.0]
```
- Phân biệt rõ: Đo RAM vật lý thực tế (`ullTotalPhys`/`ullAvailPhys`), không lấy dung lượng ảo đã commit (`ullTotalVirtual`/`ullTotalPageFile`).

---

## 7. Nguồn tính toán Uptime (Uptime Source)
- Sử dụng hàm nguyên bản `kernel32!GetTickCount64()`.
- Trả về số mili-giây đơn điệu (monotonic) tính từ thời điểm Windows khởi động:
```text
uptimeSeconds = GetTickCount64() / 1000
```
- Không dùng `DateTime.Now - bootTime` vì dễ bị ảnh hưởng bởi đồng bộ thời gian NTP hoặc người dùng đổi giờ đồng hồ hệ thống.

---

## 8. Tại sao chọn Win32 API qua P/Invoke?
1. LabGuard Agent là phần mềm giám sát phòng máy chuyên dụng trên Windows.
2. Thư viện `kernel32.dll` luôn có sẵn trên mọi phiên bản Windows từ Windows 7 đến Windows 11 và Windows Server.
3. Không cần cài thêm bất kỳ NuGet package phụ thuộc nào.
4. Thời gian thực thi nhanh, đáp ứng tốt tiêu chuẩn < 100 ms của WBS (mẫu warm-up đầu: 1.137 ms; các mẫu đo kế tiếp: 0.026–0.332 ms).

---

## 9. Tại sao KHÔNG chọn PerformanceCounter làm giải pháp chính?
1. PerformanceCounter đã được cân nhắc, nhưng A-004 ưu tiên gọi trực tiếp Win32 API (`kernel32.dll`) vì có thể thu thập đủ 3 chỉ số CPU/RAM/Uptime mà không cần bổ sung gói phụ thuộc `System.Diagnostics.PerformanceCounter`.
2. Một số counter đo tốc độ/tỷ lệ (rate-based counters) cần nhiều hơn một lần quan sát để tính toán delta trước khi có giá trị có nghĩa.
3. A-004 không khẳng định PerformanceCounter là không hợp lệ hay phổ quát bắt buộc quyền quản trị viên, nhưng việc dùng Win32 API trực tiếp giúp tối giản phụ thuộc và giữ triển khai trong suốt, dễ kiểm thử deterministic qua `ISystemMetricsNativeProvider`.

---

## 10. Tại sao KHÔNG chọn WMI làm giải pháp chính?
1. WMI (Windows Management Instrumentation) chạy qua tầng quản trị và truy vấn WQL qua COM/WMI Provider Host (`WmiPrvSE.exe`), bổ sung một tầng trung gian không cần thiết cho 3 chỉ số cơ bản của A-004.
2. Dịch vụ WMI trên máy trạm có thể bị vô hiệu hóa, bị tạm dừng hoặc gặp lỗi kho lưu trữ (repository corruption).
3. Trong khuôn khổ nhiệm vụ A-004, nhóm không thực hiện benchmark độ trễ chi tiết cho WMI nên không đưa ra tuyên bố định lượng chính xác về thời gian phản hồi của WMI.
4. Gọi trực tiếp Win32 API qua P/Invoke là giải pháp trực tiếp, gọn nhẹ và không phát sinh thêm tầng quản lý phụ trợ.

---

## 11. Tại sao sử dụng PeriodicTimer?
1. `PeriodicTimer` (từ .NET 6) cung cấp cơ chế `await timer.WaitForNextTickAsync(ct)` định kỳ rõ ràng và phù hợp với background service bất đồng bộ, giúp tránh manual busy-loop và đơn giản hóa cancellation.
2. Khi tác vụ xử lý của chu kỳ mất thời gian, `PeriodicTimer` tự động quản lý tick kế tiếp và ngăn ngừa hiện tượng chồng lấn tác vụ (tick overlap) so với callback timer truyền thống.
3. Dừng sạch sẽ khi nhận `CancellationToken` mà không cần xử lý dispose phức tạp.
4. *Lưu ý kỹ thuật:* `PeriodicTimer` tự nó không đảm bảo loại bỏ hoàn toàn độ trôi thời gian (zero timing drift) nếu gặp scheduler delay từ hệ điều hành hoặc khi công việc kéo dài quá chu kỳ (PeriodicTimer does not by itself constitute a guarantee of zero timing drift under scheduler delay or long-running work).

---

## 12. Tại sao các producer không ghi trực tiếp vào NetworkStream?
- Giao thức mạng LabGuard sử dụng TCP Stream. Nếu hai luồng `MetricsService` và `HeartbeatService` cùng gọi `stream.WriteAsync()`, các mảng byte của hai frame JSON sẽ bị trộn lẫn vào nhau (Frame Interleaving / byte tearing).
- Toàn bộ các producer phải đẩy frame vào hàng đợi có chặn `Channel<byte[]>` của `AgentSocketClient`. Chỉ DUY NHẤT một luồng ghi nền (`WriterLoopAsync`) đọc tuần tự từ hàng đợi và ghi trọn vẹn từng frame xuống socket.

---

## 13. Cơ chế ổn định BootId và tính duy nhất của BatchId
- **`BootId`:** Sinh một lần duy nhất (`Guid.NewGuid().ToString("D")`) khi phiên làm việc của Agent bắt đầu. Giữ nguyên giá trị này trong tất cả các `METRIC_BATCH` cho đến khi tiến trình kết thúc. Đây là khóa phân vùng quan trọng để Central phân biệt các lần khởi động khác nhau của cùng một `DeviceId`.
- **`BatchId`:** Sinh mới mỗi chu kỳ (`Guid.NewGuid().ToString("D")`). Đảm bảo mỗi mẻ số liệu là duy nhất, phục vụ cho bảng dedup `(DeviceId, BootId, BatchId)` tại Central và xác nhận `BATCH_ACK` khi lưu CSDL.

---

## 14. Tại sao HEARTBEAT tách biệt hoàn toàn khỏi METRIC_BATCH?
1. **Mục đích kỹ thuật khác nhau:** `HEARTBEAT` chỉ dùng để duy trì liveness và cập nhật Dead Man's Switch monotonic deadline (20s timeout) tại Central. `METRIC_BATCH` dùng để lưu trữ lịch sử hiệu năng 48h và chạy luật phát hiện quá tải CPU.
2. **Nguyên tắc Replay:** Khi đứt mạng, các mẻ `METRIC_BATCH` có giá trị lịch sử nên sẽ được đệm vào `ReplayBuffer` (A-006) để phát lại. Ngược lại, `HEARTBEAT` cũ khi đứt mạng không mang giá trị liveness, tuyệt đối KHÔNG bao giờ được lưu vào `ReplayBuffer`. Việc tách riêng 2 bản tin giữ cho cấu trúc hệ thống rõ ràng và độc lập.

---

## 15. Chiến lược kiểm thử tự động (Test Strategy)
- Tách biệt tầng gọi hệ điều hành qua `ISystemMetricsNativeProvider` để unit test có thể đưa vào các kịch bản giả lập tất định (deterministic): kiểm tra delta CPU, xử lý số âm/tràn số, clamping [0, 100], trừ RAM, chuyển đổi tick count.
- Tích hợp thực tế với `FakeTcpServer` trên loopback: Kiểm tra một frame `METRIC_BATCH` và `HEARTBEAT` thật đi qua `AgentSocketClient`, được server đọc ra bằng `FrameCodec` và thẩm định nghiêm ngặt qua `EnvelopeValidator`.
- Kiểm thử chịu tải đồng thời (concurrent stress test): Cả 2 service cùng bắn tin dồn dập vào hàng đợi mà không xảy ra hiện tượng frame bị hỏng hay trộn byte.

---

## 16. Kết quả kiểm thử thực tế (Actual Test Results)
- `tests/LabGuard.Agent.Tests`: **34 / 34 tests PASSED (100%)**
  - 16 tests A-003 (Socket TCP, BoundedChannel 256, Single Writer, Writer Fault, DisposeAsync).
  - 18 tests A-004:
    - `MetricsCollectorTests.cs` — 10 xUnit cases (7 `[Fact]` + 1 `[Theory]` × 3 `[InlineData]`): CPU delta formula, clamping [0,100], RAM UsedPhysicalBytes, Uptime seconds, Windows smoke test, native API error handling, latency benchmark < 100ms.
    - `MetricsServiceTests.cs` — 4 xUnit cases: valid METRIC_BATCH qua FakeServer, BootId ổn định / BatchId duy nhất, cancellation, fail-fast khi chưa kết nối.
    - `HeartbeatServiceTests.cs` — 4 xUnit cases: valid HEARTBEAT envelope, SequenceNumber đơn điệu, concurrent stress, cancellation.
- `tests/LabGuard.Protocol.Tests`: **107 / 107 tests PASSED (100%)**
- `tools/validation/test_frame_codec`: **10 / 10 scenarios PASSED (100%)**
- **Tổng cộng toàn bộ dự án:** **141 / 141 tests PASSED (100%)**.


---

## 17. Độ trễ thu thập đo lường được (Measured Collection Latency)

> **Nguồn đo lường:** Tất cả số liệu dưới đây được thu thập bằng lớp `WindowsMetricsCollector` thực tế trong production.
> Một harness C# tạm thời (ngoài Git worktree, không được commit) đã gọi trực tiếp `ReadCpuPercent()`, `ReadMemoryBytes()`, `ReadUptimeSeconds()` và đo thời gian bằng `Stopwatch`.

- **Môi trường:** Windows 11 Build 26200 (DESKTOP-OCQVMU6), .NET 9.0
- **Bắt đầu đo:** 2026-09-27T15:54:02.4054304+07:00
- **Khoảng cách giữa các mẫu:** 1,200ms (để GetSystemTimes có delta có nghĩa)

| Mẫu | Timestamp (+07:00) | CPU (%) | RAM (%) | Uptime (s) | Độ trễ (ms) |
|:---:|:---|:---:|:---:|:---:|:---:|
| 1 | 2026-09-27T15:54:02.4054304+07:00 | 41.67% | 81.04% | 89,678 | **1.137** (JIT warm-up) |
| 2 | 2026-09-27T15:54:03.6292122+07:00 | 19.53% | 81.10% | 89,679 | **0.026** |
| 3 | 2026-09-27T15:54:04.8439896+07:00 | 22.51% | 81.61% | 89,680 | **0.086** |
| 4 | 2026-09-27T15:54:06.0439097+07:00 | 23.35% | 80.98% | 89,681 | **0.103** |
| 5 | 2026-09-27T15:54:07.2548919+07:00 | 32.18% | 80.54% | 89,682 | **0.332** |
| 6 | 2026-09-27T15:54:08.4699509+07:00 | 31.98% | 80.64% | 89,684 | **0.165** |
| 7 | 2026-09-27T15:54:09.6723236+07:00 | 23.45% | 80.87% | 89,685 | **0.192** |

- **Đánh giá độ trễ thực tế:** Mẫu đo đầu tiên chịu ảnh hưởng của chi phí JIT/warm-up ghi nhận 1.137 ms; các mẫu đo tiếp theo dao động trong khoảng 0.026–0.332 ms. Tất cả các mẫu đo thực tế đều thỏa mãn yêu cầu < 100 ms của WBS.
- **Kết luận độ trễ:** ✅ PASS — Đạt tiêu chí thời gian thu thập.


---

## 18. Hạn chế đã biết (Known Limitations)
- `WindowsMetricsCollector` gọi Win32 API `kernel32.dll` nên phụ thuộc vào hệ điều hành Windows. Nếu chạy trên Linux/macOS, collector sẽ ném `PlatformNotSupportedException` (tuy nhiên tầng trừu tượng `ISystemMetricsNativeProvider` cho phép chạy unit test trên mọi nền tảng).
- Chỉ số CPU đo mức tải toàn hệ thống (System-wide CPU), không phải riêng tiến trình của Agent.

---

## 19. Các chặng tiếp theo sẽ bổ sung những gì?
- **A-005 (Tự động kết nối lại):** Bổ sung vòng lặp tái kết nối tự động với giải thuật Exponential Backoff có ngẫu nhiên hóa (Jitter) khi socket bị ngắt kết nối.
- **A-006 (Bộ nhớ đệm ngoại tuyến ReplayBuffer):** Bổ sung hàng đợi RAM lưu trữ tối đa 50 mẻ `METRIC_BATCH` (loại bỏ theo FIFO Drop-Oldest khi đầy) và tự động phát lại kèm cờ `isReplay = true` khi mạng được phục hồi.
- **A-007 (Giám sát dịch vụ mạng ServiceMonitor):** Bổ sung tính năng probe cổng mạng TCP (80, 8080) và gửi bản tin `SERVICE_STATUS`.
