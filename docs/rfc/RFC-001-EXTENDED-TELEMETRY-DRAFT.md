# RFC-001 — Mở rộng telemetry tài nguyên

**Trạng thái:** DRAFT — PENDING quyết định của nhóm
**Cơ sở đối chiếu:** main tại a90ef8d1fcab0f1c18e363cbcd77d61e1d8dd2dc
**Phạm vi:** Chuẩn bị quyết định về CPU, RAM, dung lượng đĩa, lưu lượng mạng, nhiệt độ CPU và nguồn dữ liệu cho Admin. Đây chưa phải phê duyệt thay đổi hợp đồng.

Các nhãn **FACT**, **INFERENCE** và **PROPOSAL** lần lượt chỉ sự thật có thể đối chiếu trong repository, suy luận từ các bằng chứng đó, và phương án chờ nhóm quyết định. Chưa có quyết định tập thể hay kết quả kiểm thử telemetry mở rộng được cung cấp. RFC #17 vẫn mở; ngày họp ghi trong Issue không phải bằng chứng rằng cuộc họp đã diễn ra.

## 1. Hiện trạng và điểm không khớp

| Nội dung | Bằng chứng hiện hành | Kết luận |
|---|---|---|
| **FACT — Phạm vi hợp đồng** | docs/spec/PROTOCOL_SPEC.md:60-66 ghi METRIC_BATCH có CPU, RAM, NIC, Uptime và loại Disk khỏi MVP. docs/spec/ARCHITECTURE_DECISIONS.md:32,101 cũng loại Disk. | Mục tiêu sản phẩm đã nêu trong Issue #17 rộng hơn hợp đồng P0 đang đóng băng. |
| **FACT — DTO** | src/LabGuard.Protocol/Dtos/TelemetryDtos.cs:15-30 có MetricName, MetricValue, Unit, RecordedAtUtc; MetricBatchPayload có Samples, UptimeSeconds, BootId, BatchId, IsReplay. Không có ResourceId hoặc trạng thái chất lượng/khả dụng. | DTO diễn tả nhiều tên chỉ số, nhưng không nhận diện ổ đĩa hay card mạng khi một máy có nhiều tài nguyên. DTO cũng không phân biệt số 0 đo được với phép đo không khả dụng. |
| **FACT — Agent code và runtime** | src/LabGuard.Agent/MetricsCollector.cs:16-35 có bộ thu thập CPU, RAM vật lý và uptime. src/LabGuard.Agent/MetricsService.cs:71-127 có thể tạo METRIC_BATCH gồm hai mẫu CPU/RAM cùng RecordedAtUtc và đưa envelope vào hàng đợi của AgentSocketClient. Tuy nhiên, src/LabGuard.Agent/AgentConsoleApplication.cs:17-21,65-103 chặn chế độ monitoring thường vì TLS/HELLO/AUTH/session reader chưa có; `--collect-once` chỉ đọc tại chỗ; `--probe` chỉ kiểm tra TCP và xếp HELLO vào hàng đợi, không xác nhận Central đã nhận hoặc xác thực. | Collector và đóng gói mẫu đã có ở mức thành phần, nhưng không có monitoring định kỳ khả dụng theo đường khởi chạy thông thường. Không có bằng chứng Agent → Central → SQL → Admin đang vận hành. |
| **FACT — SQL** | database/schema.sql:25-39 định nghĩa một hàng MetricSamples với CpuPercent, MemoryPercent, UptimeSeconds và RecordedAtUtc; không có các trường Disk/NIC/nhiệt độ hoặc dimension tài nguyên. DDL tự ghi chỉ dành cho database mới tại dòng 7-9. | Central cần một quy tắc ánh xạ rõ từ danh sách DTO sang hàng SQL. Schema hiện hành không lưu nguyên trạng từng MetricSampleDto. |
| **FACT — Trạng thái Central** | src/LabGuard.Central/CentralServer.cs:9-13,156-160,193-215 mô tả listener đọc frame, ghi log và phát sự kiện EnvelopeReceived. Không có pipeline lưu SQL trong lớp này. | Không có bằng chứng Central hiện xác thực, ghi dữ liệu hoặc ACK telemetry đã commit. |
| **FACT — Lịch sử** | docs/spec/PROTOCOL_SPEC.md:84-90,207,216-217; src/LabGuard.Protocol/MessageTypes.cs:68-82; docs/spec/CONTRACT_MATRIX.md:129-130 đặt GET_METRICS/METRICS_RESULT trong nhóm P0 OPTIONAL. | Không được coi truy vấn lịch sử là luồng bắt buộc P0 hoặc là API đã hoạt động. |
| **FACT — Kiểm thử thành phần** | tests/LabGuard.Agent.Tests/MetricsServiceTests.cs:64-76 và tests/LabGuard.Protocol.Tests/Fixtures/ProtocolFixtures.cs:187-193 kiểm tra/đóng gói CPU, RAM; tests/LabGuard.Central.Tests/CentralServerTests.cs:115-164 kiểm tra listener/frame. | Các test này không chứng minh phiên xác thực, gửi định kỳ từ launcher, SQL persistence, BATCH_ACK bền vững hay Admin binding đã chạy đầu-cuối. |
| **FACT — Bảo đảm giao nhận** | docs/spec/PROTOCOL_SPEC.md:12-16,60-63,149-151; docs/spec/ARCHITECTURE_DECISIONS.md:18,91-93; database/schema.sql:11-23. | Giữ frame TCP length-prefix, JSON, giới hạn 256 KiB, DeviceId+BootId+BatchId, replay 50 batch và BATCH_ACK chỉ sau commit thành công. |
| **FACT — Kế hoạch** | docs/internal/WBS_TASKS.csv:7,15,23,36-41; docs/internal/DEPENDENCY_MAP.md:19,40,71,79; PR #16 đã nhập kế hoạch ScottPlot/Testcontainers, PR #18 đã cập nhật tracker. | A-006, B-005 và các phụ thuộc Admin/lịch sử vẫn phải được kiểm chứng khi triển khai. ScottPlot/Testcontainers mới là kế hoạch; không tính lại công sức của chúng vào RFC này. |

## 2. Vấn đề, yêu cầu và giới hạn

**FACT:** Mục tiêu hoàn chỉnh được nêu trong Issue #17 gồm CPU, RAM, Disk, lưu lượng mạng, CPU temperature khi phần cứng/quyền cho phép và uptime.
**INFERENCE:** Cần chốt danh mục metric, đơn vị, dimension, trạng thái thiếu dữ liệu và ánh xạ SQL trước khi nhiều thành viên cùng triển khai Agent, Central và Admin.
**PROPOSAL:** Phạm vi ban đầu dễ kiểm thử nhất là ổ chứa Windows đang chạy hệ điều hành, tổng RX/TX trên các adapter vật lý đủ điều kiện, cùng nhiệt độ gói CPU chỉ khi có nguồn tin cậy. Việc hiển thị nhiệt độ không được thay bằng nhiệt độ bo mạch hay giá trị giả. Nếu nhóm yêu cầu xem từng ổ/card riêng, chuyển lựa chọn lưu trữ sang phương án B dưới đây.

Yêu cầu:

- Giữ CPU %, RAM % và uptime nguyên nghĩa hiện tại; uptime là số nguyên giây kiểu 64-bit.
- Disk: phần trăm đã dùng và dung lượng còn trống theo byte, lấy trên ổ hệ thống. Không đưa ổ tháo rời hoặc ổ không sẵn sàng vào phép tổng hợp.
- Network: RX/TX theo byte/giây, không dùng bit/giây. Lấy chênh lệch bộ đếm byte tích lũy giữa hai lần đọc, chia cho thời gian đơn điệu thực tế đã trôi qua.
- Nhiệt độ: độ C, chỉ gắn nhãn CPU package khi nguồn đo xác nhận đúng cảm biến đó. Có thể khảo sát nguồn Windows/ACPI hoặc API nhà cung cấp sau khi kiểm tra trên thiết bị mục tiêu; không có nguồn thống nhất cho mọi PC. Tài liệu Microsoft lưu ý Win32_TemperatureProbe không cung cấp CurrentReading ở triển khai WMI hiện tại, còn thermal zone có thể bao gồm nhiều thiết bị tỏa nhiệt ([Win32_TemperatureProbe](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-temperatureprobe), [Windows thermal design guide](https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/design-guide)).
- Phân biệt giá trị 0 đo được với trạng thái không đo được. Không thay thiếu dữ liệu bằng 0.
- Mọi tên metric mới và đơn vị dưới đây là **đề xuất**, chưa phải tên giao thức được duyệt.

Không thuộc RFC: đổi TCP, framing, xác thực, replay/dedup, quy trình ACK, thiết kế lại kiến trúc, thêm dịch vụ trung gian, cài package hoặc bật producer mới. Không triển khai GET_METRICS cho đến khi nhóm duyệt và có Central handler, persistence, phân quyền và kiểm thử thực tế.

## 3. Danh mục metric đề xuất

| Metric | Ngữ nghĩa đề xuất | Đơn vị | Phạm vi và trường hợp thiếu |
|---|---|---|---|
| CPU | Mức sử dụng CPU theo ngữ nghĩa đang có | % | Giữ nguyên. Giá trị hợp lệ 0–100. |
| RAM | RAM vật lý đã dùng / tổng RAM vật lý | % | Giữ nguyên. Giá trị hợp lệ 0–100. |
| Disk | Dung lượng đã dùng trên ổ hệ điều hành | % | Đề xuất DISK_USED_PERCENT và DISK_FREE_BYTES. Ổ hệ thống chỉ; nếu truy vấn lỗi hoặc không sẵn sàng thì báo thiếu/không khả dụng, không trả 0. |
| Network RX | Tốc độ byte nhận trên các adapter vật lý được chọn | B/s | Đề xuất NET_RX_BYTES_PER_SEC. Tính từ counter delta / thời gian thực; adapter ảo, loopback, tunnel bị loại. |
| Network TX | Tốc độ byte gửi trên các adapter vật lý được chọn | B/s | Đề xuất NET_TX_BYTES_PER_SEC. Tổng hợp theo máy trong phạm vi ban đầu. .NET có bộ đếm byte tích lũy theo interface; tốc độ là phép tính do ứng dụng thực hiện ([IPInterfaceStatistics](https://learn.microsoft.com/en-us/dotnet/api/system.net.networkinformation.ipinterfacestatistics?view=net-9.0)). |
| CPU temperature | Nhiệt độ CPU package, nếu cảm biến xác nhận được | °C | Đề xuất CPU_TEMP_C. Khi có mẫu số, trạng thái AVAILABLE được suy ra; nếu không có mẫu, có thể báo UNSUPPORTED, UNAVAILABLE hoặc READ_ERROR. Không suy diễn từ cảm biến bo mạch. |
| Uptime | Thời gian hệ điều hành hoạt động liên tục | giây, số nguyên 64-bit | Giữ trường UptimeSeconds của batch. |

**Network edge cases — PROPOSAL:** Lần đọc đầu chỉ tạo baseline, chưa có tốc độ đo; nếu được duyệt, báo `NO_BASELINE`, không gửi 0 giả. Nếu counter giảm, interface đổi hoặc biến mất, không tính delta âm; lấy baseline mới và báo `COUNTER_RESET` hoặc `PARTIAL` theo mức dữ liệu còn lại. Chỉ xử lý wraparound nếu nguồn counter xác nhận rõ độ rộng và semantics. Giá trị 0 B/s chỉ hợp lệ khi phép đo có hai mẫu hợp lệ, counter không đổi và adapter coverage đầy đủ. API .NET là một ứng viên cho bộ đếm per-interface, nhưng filter adapter và cách ổn định định danh phải được thử trên Windows trước khi chốt.

**CPU temperature — PROPOSAL:** WMI/ACPI có thể được khảo sát, nhưng không mặc định là nhiệt độ CPU package; Win32_TemperatureProbe không có CurrentReading được điền trong WMI hiện tại. Nếu không xác định cảm biến đúng, trạng thái phải là UNSUPPORTED hoặc UNAVAILABLE. Quyền, driver, thiết bị và cách đọc cụ thể cần được thử trên máy thật trước khi chọn.

**Dimension:** DTO hiện tại không thể phân biệt hai volume hoặc hai adapter có cùng MetricName. Phương án phạm vi ban đầu tránh sự nhập nhằng bằng một ổ hệ thống và một cặp RX/TX tổng hợp theo máy. Nếu nhóm yêu cầu từng volume/interface, phải có ResourceId ổn định và nhãn hiển thị tương ứng; không ghép định danh vào tên metric tùy tiện.

## 4. Phương án lưu SQL

| Tiêu chí | A — Mở rộng MetricSamples hiện có | B — MetricReadings chuẩn hóa |
|---|---|---|
| Mô hình | Một hàng cho mỗi batch/snapshot, bổ sung các cột nullable Disk/RX/TX/temperature và cột trạng thái đo tương ứng. | Một hàng cho mỗi metric observation; các trường candidate là DeviceId, BootId, BatchId, MetricName, ResourceId tùy chọn, MetricValue, Unit, RecordedAtUtc và trạng thái đo. Đây là mô hình logic, chưa phải DDL. |
| DTO/batch mapping | Central kiểm tra một giá trị cho mỗi metric đã khai báo, cùng timestamp trong batch; ánh xạ MetricName vào cột. | Mỗi MetricSampleDto ánh xạ gần trực tiếp thành một reading. ResourceId cần được bổ sung khi chọn per-volume/per-adapter. |
| SampleCount / ACK | Giữ SampleCount là số observation có giá trị; SamplesRecorded là số observation đã lưu bền vững, không phải số hàng SQL. Nếu batch snapshot wide có một hàng, vẫn ACK đúng số observation sau commit. | SampleCount và SamplesRecorded đều đếm observation có giá trị đã lưu; trạng thái unavailable không tính thành sample. BATCH_ACK chỉ sau commit toàn bộ batch. |
| Timestamp / missing | Cột RecordedAtUtc hiện có một mốc cho hàng. Cần yêu cầu mọi metric của batch dùng chung thời điểm đo; trạng thái không có giá trị không đổi thành zero. | Giữ timestamp từng MetricSampleDto; thuận lợi khi các phép đo lấy lệch thời điểm. Reading không có giá trị cần trạng thái riêng, không giả thời điểm đo. |
| Dedup / transaction | Giữ khóa ProcessedBatches bất biến; ghi snapshot và dấu batch trong cùng transaction. Trùng batch không thêm hàng và vẫn trả ACK theo hợp đồng. | Giữ nguyên khóa/dedup; ghi tất cả readings của batch và dấu batch trong cùng transaction. Trùng batch không thêm readings và vẫn trả ACK theo hợp đồng. |
| Lịch sử / truy vấn | Truy vấn hiện tại gọn, ít nhóm/ghép; phù hợp một ổ và aggregate mạng. Cột mới và trạng thái mới cần migration mỗi khi mở rộng danh mục. | Query cần lọc/group theo tên metric, dimension, thời gian; cần index và giới hạn dữ liệu. Thêm metric/dimension linh hoạt hơn. |
| Migration / tương thích | Cột nullable giúp đọc dữ liệu cũ; phải backfill hoặc giữ null và tuyệt đối không suy ra số liệu lịch sử. | Cần migration dữ liệu cũ CPU/RAM/Uptime, giữ nguyên timestamp và batch identity. Không được tạo ResourceId hoặc thời điểm giả cho dữ liệu cũ. |
| Kiểm thử / công sức | Ít truy vấn SQL và phù hợp phạm vi đơn-resource; có kiểm thử mapping/nullable state. | Bao quát nhiều resource dễ hơn nhưng tăng migration, grouping, query, chỉ mục và số lượng bản ghi. |

### Khuyến nghị

**PROPOSAL — Chọn Option A cho phạm vi tối thiểu đã giới hạn:** một ổ chứa hệ điều hành, RX/TX tổng hợp trên adapter vật lý đủ điều kiện, CPU temperature best-effort có trạng thái. Lý do: phạm vi sản phẩm hiện nêu metric nhưng chưa bắt buộc xem từng ổ hoặc adapter; bảng snapshot cần ít logic truy vấn hơn Option B. Đây là lựa chọn thiết kế chờ duyệt, không phải kết quả kiểm thử hay khẳng định pipeline Agent–Central–SQL–Admin đã hoạt động.

Option B trở thành lựa chọn ưu tiên nếu nhóm yêu cầu mọi volume/card riêng, nhiều cảm biến nhiệt, metric có tần suất khác nhau, hoặc truy vấn theo resource. Trong trường hợp đó ResourceId là bắt buộc và việc mở rộng Option A sẽ tạo nhiều cột/logic trạng thái khó duy trì hơn.

Candidate Option A columns: DiskUsedPercent, DiskFreeBytes, NetworkRxBytesPerSecond, NetworkTxBytesPerSecond, CpuTemperatureCelsius; các cột mở rộng nullable. Trạng thái và thời điểm quan sát của từng metric có thể dùng cột riêng như NetworkState/NetworkObservedAtUtc hoặc TemperatureState/TemperatureObservedAtUtc; các tên này chỉ là ví dụ PROPOSED, không phải DDL. Dữ liệu cũ giữ NULL; NULL khác với số 0 hợp lệ. Với trạng thái `PARTIAL`, chỉ lưu giá trị tổng hợp nếu giao diện ghi rõ phạm vi adapter bị thiếu; nếu không thể xác định phạm vi thì lưu trạng thái, không lưu giá trị suy đoán. Không backfill số liệu hoặc timestamp cho bản ghi cũ.

## 5. Hợp đồng, đảm bảo và tương thích

**PROPOSAL — giữ nguyên hợp đồng đóng băng:** Dùng METRIC_BATCH/BATCH_ACK P0 cho mẫu hiện tại và chỉ mở rộng danh mục metric sau khi RFC được duyệt. Giữ frame 4-byte Big-Endian, envelope JSON, giới hạn 256 KiB, yêu cầu AUTH trước telemetry, khóa DeviceId+BootId+BatchId, ReplayBuffer tối đa 50 batch và chỉ ACK sau khi SQL commit. Đây là yêu cầu thiết kế, không phải trạng thái vận hành: Agent launcher hiện chặn monitoring thường, A-006 ReplayBuffer chưa hoàn thành, Central chưa có SQL ingestion/durable ACK. Không coi probe TCP/HELLO là telemetry đã gửi hoặc đã lưu. Không đổi độ bền hay ý nghĩa của batch để phục vụ chart.

DTO hiện tại có tên/value/unit/timestamp nhưng không có trạng thái đo hay resource identity. Candidate additive contract addition là trạng thái đo theo metric trong payload; `MetricSampleDto` tiếp tục chỉ đại diện giá trị số thực sự quan sát được. Với `UNSUPPORTED`, `UNAVAILABLE`, `READ_ERROR`, `NO_BASELINE` hoặc `COUNTER_RESET`, không tạo mẫu số giả; `PARTIAL` chỉ đi cùng mẫu số nếu quy tắc phạm vi adapter được duyệt. Semantics `SampleCount`, `SamplesRecorded`, batch cũ thiếu trường và giới hạn số mẫu còn phải được quyết định trước khi sửa frozen DTO/spec.

### Trạng thái phép đo và nguồn xác định dữ liệu — PROPOSAL

Không thêm trường hay enum nào vào hợp đồng đang đóng băng trong RFC này. `MetricSampleDto` hiện dùng `double MetricValue` không nullable; `MetricBatchPayload` có `SampleCount` và `Samples`; `BatchAckPayload` có `SamplesRecorded` (src/LabGuard.Protocol/Dtos/TelemetryDtos.cs:15-40). Fixture hiện có cho thấy `sampleCount` bằng số phần tử `samples` và `samplesRecorded` bằng số mẫu được ghi (docs/spec/CONTRACT_MATRIX.md:295-302), nhưng không có validator Central hiện hành quy định rõ zero-sample hay status-only batch. Central hiện đọc frame, ghi log và phát sự kiện, chưa xác nhận/lưu SQL hoặc thực hiện BATCH_ACK (src/LabGuard.Central/CentralServer.cs:193-215; database/schema.sql:59-62).

| Phương án trên wire | Tương thích / dung lượng | Lưu SQL, timestamps và UI | Zero, lỗi, sample count và công sức |
|---|---|---|---|
| **MS-A — danh sách trạng thái đo riêng** | Giữ mẫu số hiện có; thêm danh sách tùy chọn trạng thái cho từng metric. Cần kiểm thử consumer cũ và thay đổi hợp đồng được duyệt. Tốn thêm ít JSON cho trạng thái thiếu; không lặp lại số đo. | Trạng thái có metric/resource key, `observedAtUtc`, mã trạng thái và `reasonCode` giới hạn. Hợp với cột số nullable và cột state/time riêng của Option A; UI có thể phân biệt không hỗ trợ, lỗi đọc và thiếu baseline. | Số 0 vẫn là mẫu số hợp lệ. `SampleCount = Samples.Count` (mẫu số); `SamplesRecorded` là số mẫu số đã ghi bền vững. Status không được tính là sample. Validation chéo cần thiết; công sức vừa phải. |
| **MS-B — trạng thái gắn vào từng mẫu** | Mở rộng `MetricSampleDto`; để biểu diễn không có giá trị phải đổi `MetricValue` sang nullable hoặc dùng cấu trúc kết quả khác. Là thay đổi kiểu DTO có rủi ro tương thích và cần kiểm thử JSON cũ/mới. | Timestamp nằm cùng observation; ánh xạ trạng thái đơn giản hơn cho mỗi mẫu. Cần quy định mẫu status-only và nullable numeric storage. | Dễ đếm nhầm status là sample nếu không đổi rõ semantics `SampleCount`/`SamplesRecorded`; công sức cao hơn vì thay đổi kiểu/validator. |
| **MS-C — không thêm status lên wire; Central/Admin suy ra thiếu và cũ** | Tương thích wire tốt nhất và ít thay đổi nhất. | Central ghi thời điểm nhận theo đồng hồ server; Admin có thể báo chưa nhận/đã cũ. Không thể phân biệt Agent không hỗ trợ với lỗi đọc hoặc lỗi gửi. | Dùng được như bước triển khai đầu cho các mẫu P0 hiện có; không đáp ứng đầy đủ nguyên nhân thiếu của metric mở rộng. Công sức thấp nhất. |

**Khuyến nghị có điều kiện:** Dùng MS-C cho giai đoạn đầu để hiển thị sự vắng mặt/độ cũ dựa trên dữ liệu Central thực sự nhận được; sau khi nhóm duyệt mở rộng telemetry, chọn MS-A cho các metric mở rộng. MS-B không được ưu tiên vì buộc đổi `double` thành nullable và làm tăng rủi ro tương thích. Mọi phương án giữ nguyên TCP framing, batch key, replay và điều kiện ACK.

**Candidate semantics cho MS-A:**

| Trạng thái | Ý nghĩa đề xuất | Ứng dụng |
|---|---|---|
| `AVAILABLE` | Có mẫu số hợp lệ; đây là trạng thái ngầm định khi có `MetricSampleDto`, không cần status record riêng. | CPU/RAM và metric mở rộng khi đọc thành công; giá trị 0 vẫn là AVAILABLE. |
| `UNSUPPORTED` | Thiết bị/phần cứng hoặc API được chọn không hỗ trợ phép đo. | Nhiệt độ CPU không có sensor được hỗ trợ. |
| `UNAVAILABLE` | Tạm thời không có nguồn/reading sử dụng được, nhưng không có lỗi đọc cụ thể để phân loại. | Sensor không trả reading hoặc không có adapter đủ điều kiện. |
| `READ_ERROR` | Thao tác đọc có lỗi xác định; chỉ gửi mã lý do giới hạn, không gửi exception tùy ý. | Lỗi đọc disk, counter, hoặc sensor. |
| `NO_BASELINE` | Chưa có hai lần đọc counter hợp lệ để tính tốc độ. | Mẫu mạng đầu tiên sau khi bắt đầu hoặc khi thay adapter. |
| `COUNTER_RESET` | Counter giảm hoặc reset; tốc độ cho khoảng đó không hợp lệ và cần baseline mới. | RX/TX sau reboot/reset interface. |
| `PARTIAL` | Có số đo nhưng không đủ toàn bộ adapter thuộc phạm vi tổng hợp đã chọn. | Tổng RX/TX chỉ từ các adapter còn đọc được; phải chỉ rõ đây là số đo chưa đủ phạm vi. |

Trong MS-A, `PARTIAL` có thể đi kèm mẫu số ước lượng trên phần adapter còn đọc được; các trạng thái còn lại không đi kèm mẫu số. Tối đa một trạng thái cho mỗi metric/resource và mốc quan sát trong một batch. Không cho phép status mâu thuẫn với mẫu số cùng metric; từ chối mã trạng thái lạ hoặc `reasonCode` ngoài allowlist. Metric trùng lặp bị từ chối. CPU/RAM P0 bị thiếu ngoài dự kiến làm batch không hợp lệ; không âm thầm coi đó là sensor unsupported. Disk lỗi đọc dùng `READ_ERROR`; network không có lần đọc đầu dùng `NO_BASELINE`, counter reset dùng `COUNTER_RESET`, mất một phần adapter dùng `PARTIAL`, không còn adapter đủ điều kiện dùng `UNAVAILABLE`.

**SampleCount / ACK proposal:** `SampleCount` phải khớp số phần tử `samples` có giá trị số, không đếm status records; `SamplesRecorded` báo số mẫu số đã ghi bền vững sau commit, không phải số hàng snapshot. Trạng thái phải được ghi cùng transaction để tránh ACK khi chỉ trạng thái hoặc chỉ số đo đã được lưu. Batch không có mẫu số (`SampleCount = 0`) hiện chưa được hợp đồng/validator xác nhận; đề xuất từ chối status-only batch ở phiên bản đầu. Status records có thể đi kèm batch có ít nhất một mẫu số hợp lệ. Lặp lại cùng batch sau commit không ghi thêm dữ liệu và trả ACK theo quy tắc dedup hiện có. Đây là đề xuất cần quyết định PENDING, không phải semantics đã được duyệt.

**Tách chất lượng đo, độ mới và kết nối:** trạng thái đo mô tả kết quả lấy mẫu ở Agent; `RecordedAtUtc` là mốc do Agent ghi cho phép đo và không thay thế thời gian server nhận batch. Độ mới của telemetry là nhận định của Central/Admin dựa trên thời điểm Central nhận dữ liệu và/hoặc tuổi `RecordedAtUtc`; dữ liệu cũ không phải trạng thái Agent OFFLINE. `LastSeenUtc` là trường nullable trong DeviceSummaryDto nhưng DTO không định nghĩa nguồn; không được tự coi giá trị này là heartbeat hợp lệ. Liveness phải do Central quyết định từ phiên đã xác thực và heartbeat hợp lệ, dùng đồng hồ đơn điệu phía Central theo PROTOCOL_SPEC.md:55-58. Liveness nội bộ của Central (listener/process health) cũng là trạng thái riêng, không suy ra từ một mẫu đo của Agent.

**SQL Option A candidate, không thực thi:** giữ các cột CPU/RAM/uptime hiện có; các metric mở rộng dùng cột số nullable. Với mỗi phép đo có thể vắng mặt, lưu state và `ObservedAtUtc` riêng; `NULL` nghĩa là không có số đo, khác với số 0 đã đo. Bản ghi cũ không có state mở rộng để NULL/unknown; không suy diễn state hay timestamp. Nếu chỉ một số phép đo có trong batch, lưu giá trị và state từng metric độc lập trong một transaction cùng dấu batch. SQL NULL không biểu thị unsupported hay read error; state column mới làm việc đó. Việc thêm cột chỉ được tiến hành sau quyết định và migration review.

Không tuyên bố thêm trường JSON là tương thích chỉ vì framing không đổi. Cần kiểm thử parser cũ/mới, serializer và fixture trên đúng .NET 9; phối hợp triển khai Central trước khi Agent phát tên metric mới. Kiểm tra payload lớn nhất vẫn dưới 256 KiB, giới hạn số sample và từ chối metric/unit không nằm trong allowlist.

`RecordedAtUtc` do Agent gửi là thời điểm đo phía client; nó không thay `ReceivedUtc` hoặc đồng hồ đơn điệu Central dùng cho liveness. Không dùng timestamp client để quyết định Agent online/offline. Nếu nhiều mẫu cùng batch không cùng timestamp, Option A không giữ được độ chính xác từng mẫu và phải đổi sang Option B hoặc thỏa thuận timestamp chung.

### Giai đoạn truy vấn Admin

1. **P0 — snapshot:** Sau khi có bộ xử lý Central xác thực/lưu trữ, nhóm có thể đề xuất đưa giá trị hiện hành vào các phản hồi/sự kiện thiết bị P0 hiện có, như DEVICE_PAGE và DEVICE_UPDATE, bằng cách mở rộng DTO đã duyệt. Các message type này hiện có trong catalog nhưng handler/producer mở rộng chưa được chứng minh; Admin vẫn chỉ dùng dữ liệu nhận từ Central. UI cần hiện rõ metric thiếu, trạng thái đo và thời điểm nhận/đo theo đúng nguồn.
2. **P0 optional — history:** Chỉ sau quyết định riêng, hiện thực GET_METRICS/METRICS_RESULT cho lịch sử SQL, gồm lọc thiết bị/khoảng thời gian, phân trang/giới hạn và quyền truy vấn. Admin không truy cập SQL hay Agent trực tiếp. ScottPlot 5 đã được đưa vào kế hoạch qua PR #16, nhưng PR #16 không cài package hoặc triển khai chart.

## 6. SQL, migration và an toàn dữ liệu

- **PROPOSAL:** Transaction ingestion ghi dấu ProcessedBatches, snapshot và trạng thái đo liên quan theo một đơn vị nguyên tử. Chỉ commit thành công mới phát BATCH_ACK. SQL rollback hoặc lỗi trước commit thì không ACK thành công; replay sau đó dùng cùng bộ khóa và không tạo dữ liệu trùng.
- Giữ ProcessedBatches TTL và ý nghĩa khóa hiện có. Không đổi batch identity, replay order hoặc chính sách 50 batch trong RFC này.
- DDL hiện ghi “new databases only”. Trước triển khai phải xác minh database thật đã tạo từ phiên bản nào. Với database chưa có dữ liệu, dùng schema đã duyệt. Với database có dữ liệu, chuẩn bị migration có backup, kiểm tra số hàng, rollback rõ ràng; không chạy lại CREATE TABLE như migration.
- Nếu chọn A, cột mới nullable. Dữ liệu cũ không có Disk/NIC/temperature tiếp tục NULL/unknown; không backfill 0. Nếu có yêu cầu chuyển sang B, mapping các trường CPU/RAM cũ giữ nguyên DeviceId, BootId, BatchId và RecordedAtUtc; mọi mapping lịch sử phải được đối soát số hàng và không tự tạo dimension.
- Chỉ mục truy vấn theo DeviceId+RecordedAtUtc cần được đo trên dataset kiểm thử sau khi query contract rõ. Không hứa hiệu năng hoặc đặt ngưỡng trước khi có đo kiểm.
- Chặn giá trị ngoài miền CPU/RAM phần trăm, giá trị âm không hợp lệ, timestamp lỗi và unit không đúng registry. Server xác minh DeviceId đã xác thực; không tin danh tính do payload tự khai báo.
- Mã trạng thái không chứa thông tin nhạy cảm về phần cứng; log không ghi token hoặc thông tin xác thực. Nhiệt độ hiển thị là dữ liệu tham khảo, không phải lệnh điều khiển phần cứng.

## 7. Tệp và task bị ảnh hưởng nếu được duyệt

| Phần | Tệp / Task ID | Chủ trì |
|---|---|---|
| Hợp đồng | docs/spec/PROTOCOL_SPEC.md, CONTRACT_MATRIX.md, ARCHITECTURE_DECISIONS.md; DTO TelemetryDtos.cs; fixtures và ProtocolSpecificationTests | A — Võ Trọng Phúc; review B/C/D |
| Thu thập | MetricsCollector.cs, MetricsService.cs, cấu hình Agent và Agent tests; LG-A-004, LG-A-006 | A — Võ Trọng Phúc |
| Ingestion / persistence | CentralServer hoặc handler mới, repository và tests; database/schema.sql; LG-B-005, LG-B-008 | B — Nguyễn Bá Quang; review A |
| Admin snapshot/history | AdminDtos, client, màn hình thiết bị và chart; LG-C-002, LG-C-003, LG-C-004, SC-01..SC-03; GET_METRICS chỉ khi được duyệt | C — Lý Tuyết Vy; review B |
| Bằng chứng sự cố | LG-D-007, LG-D-008, LG-D-009, LG-D-010, LG-B-010, LG-C-012 | D — Đỗ Minh Ngọc phối hợp B/C; không xây ứng dụng riêng |

Không sửa WBS hoặc Excel bằng RFC. Sau khi quyết định, chủ nhiệm vụ cần rà effort/phụ thuộc rồi cập nhật theo quy trình nhóm.

## 8. Người phụ trách và effort sơ bộ

| Người | Phần việc nếu RFC được duyệt | Ước lượng tăng thêm |
|---|---|---:|
| Võ Trọng Phúc (A) | Chốt metric registry/wire status; thu thập disk, adapter counter, nhiệt độ best-effort; unit tests và payload/batch edge cases | 9–13 giờ |
| Nguyễn Bá Quang (B) | Chọn/migrate SQL, ánh xạ batch, transaction, dedup và ACK; schema/integration tests | 6–9 giờ |
| Lý Tuyết Vy (C) | Hiển thị metric mới, thiếu/cũ/không hỗ trợ; history chỉ sau GET_METRICS handler; chart theo điều kiện | 3–5 giờ |
| Đỗ Minh Ngọc (D) | Rà bằng chứng, độ mới và ca hỗ trợ sự cố với metric thiếu/không hợp lệ; kiểm tra TCP khi tích hợp | 2–3 giờ |
| **Tổng sơ bộ Option A** | Khoảng công nhóm, chưa tính effort cốt lõi còn thiếu và ScottPlot/Testcontainers đã có kế hoạch riêng | **20–30 giờ** |
| **Option B** | Dimension per-resource, migration/query/index và test bổ sung | **26–38 giờ tổng nhóm** |

**PROPOSAL:** Đây là ước lượng kỹ thuật sơ bộ, không phải cam kết lịch. Các số phụ thuộc phạm vi sensor/adapter và tình trạng B-005/A-006. Không cộng lặp 6–9 giờ ScottPlot hoặc 7–10 giờ Testcontainers đã ghi trong PR #18; chủ nhiệm vụ phải rà phần giao nhau trước khi cập nhật WBS.

## 9. Kế hoạch phụ thuộc và nghiệm thu

1. **Quyết định trước mã:** nhóm chốt ổ đĩa, adapter, nhiệt độ, metric identifiers/units/status, A hay B, chính sách DB đã có dữ liệu, và compatibility.
2. **A — hợp đồng và test:** cập nhật DTO/spec/fixtures sau khi thay đổi freeze được duyệt; kiểm tra metric allowlist, units, trạng thái và JSON tương thích.
3. **A — collector:** disk snapshot, counter delta với monotonic elapsed time, reset/adapter change, nguồn nhiệt chính xác; không tạo giá trị thay thế.
4. **B — ingestion:** AUTH/session → validate → transaction/dedup → commit → BATCH_ACK. Dùng SQL Server cô lập opt-in khi Testcontainers được owner cho phép; không coi schema-test là bằng chứng persistence.
5. **C — Admin:** snapshot có nguồn Central; history chỉ qua optional GET_METRICS/METRICS_RESULT sau khi Central handler và RBAC hoạt động. Làm chart sau điều kiện SC-03.
6. **D — kiểm tra tích hợp:** dùng evidence do Central trả; hiện rõ thiếu/cũ/không khả dụng, không kết luận nguyên nhân nếu chưa đủ bằng chứng.
7. **Review chéo:** A/B kiểm tra metric và transaction; C kiểm tra hiển thị; D kiểm tra bằng chứng/quy trình; chạy unit/integration trên Windows và ghi đúng môi trường/kết quả. LAN test chỉ ghi khi thật sự chạy.

Ca nghiệm thu dự kiến gồm CPU/RAM giữ nghĩa cũ; disk free/used; RX/TX delta theo thời gian; first network sample không giả thành 0; reset/wrap/adapter đổi; zero traffic thật; temperature unsupported/unavailable/read error; missing vs stale; nhiều Agent cùng BatchId; replay cùng batch; SQL rollback không ACK; duplicate không lặp dữ liệu; payload vượt giới hạn; Admin chỉ dùng dữ liệu Central; history pagination/authorization nếu được duyệt. Tất cả là **TEST PLAN — NOT RUN**.

## 10. Rollback và quyết định còn mở

**PROPOSAL:** Nếu telemetry mở rộng gây lỗi, Central ngừng chấp nhận metric mới và Agent ngừng phát chúng sau cấu hình/phát hành có kiểm soát; CPU/RAM cũ tiếp tục theo hợp đồng đã duyệt. Giữ batch key và dữ liệu lịch sử. Không xóa cột/tables có dữ liệu bằng rollback tự động. Nếu migration đã chạy, rollback phải là migration rà soát có backup; nếu không thể đảo an toàn, forward-fix được ưu tiên.

| Câu hỏi cần quyết định | Trạng thái |
|---|---|
| Disk: chỉ ổ hệ thống (khuyến nghị) hay tất cả fixed volumes? Có đưa ổ removable vào sau này? | PENDING |
| Network: tổng hợp adapter vật lý (khuyến nghị) hay mỗi adapter? Chọn tiêu chí vật lý/hoạt động nào? | PENDING |
| Nhiệt độ: chấp nhận best-effort; nguồn/nhãn nào xác nhận CPU package và cách báo UNSUPPORTED/UNAVAILABLE? | PENDING |
| Chấp nhận metric names/units/status; SampleCount/SamplesRecorded đếm observation thế nào? | PENDING |
| Cách biểu diễn status đo: MS-A, MS-B hay MS-C; status-only batch có được phép không? | PENDING |
| SQL: Option A cho scope đơn-resource hay Option B nếu bắt buộc dimension? | PENDING |
| Database triển khai sẽ là mới hay cần migration dữ liệu hiện hữu? Ai kiểm kê database? | PENDING |
| Khi nào bật optional GET_METRICS/METRICS_RESULT, ai duyệt handler và RBAC? | PENDING |
| A/B/C/D xác nhận effort và cập nhật WBS/Excel ở bước nào? | PENDING |

## 11. Approval gate

**Approval status: PENDING.** Không có bằng chứng trong Issue #17 comments hay các nguồn đã kiểm tra cho thấy nhóm đã quyết định các metric, schema hoặc migration này. Người điều phối có thể đề xuất phạm vi, nhưng quyết định frozen contract cần chủ sở hữu liên quan và nhóm ghi nhận thực tế. Chỉ bắt đầu sửa mã/DTO/schema sau khi:

- A, B, C, D phản hồi phạm vi và ranh giới trách nhiệm;
- chủ sở hữu A/B duyệt contract, ingestion, SQL và migration;
- C duyệt nguồn/hiển thị; D duyệt cách trình bày bằng chứng;
- quyết định được ghi vào nguồn chính thức; WBS/Excel được cập nhật sau effort review;
- PR riêng có review chéo và test thực chạy.

**Test/build/SQL/Windows/LAN trong RFC này:** NOT RUN — tài liệu chỉ chuẩn bị quyết định, không sửa code hay schema.
