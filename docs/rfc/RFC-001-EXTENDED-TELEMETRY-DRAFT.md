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
| **FACT — Agent** | src/LabGuard.Agent/MetricsCollector.cs:16-35 chỉ khai báo CPU, RAM vật lý và uptime. src/LabGuard.Agent/MetricsService.cs:75-127 tạo đúng hai mẫu CPU/RAM, cùng mốc RecordedAtUtc cho một chu kỳ, rồi gửi METRIC_BATCH. | Chưa có bộ thu thập Disk, NIC hay CPU temperature trong Agent tại baseline này. |
| **FACT — SQL** | database/schema.sql:25-39 định nghĩa một hàng MetricSamples với CpuPercent, MemoryPercent, UptimeSeconds và RecordedAtUtc; không có các trường Disk/NIC/nhiệt độ hoặc dimension tài nguyên. DDL tự ghi chỉ dành cho database mới tại dòng 7-9. | Central cần một quy tắc ánh xạ rõ từ danh sách DTO sang hàng SQL. Schema hiện hành không lưu nguyên trạng từng MetricSampleDto. |
| **FACT — Trạng thái Central** | src/LabGuard.Central/CentralServer.cs:9-13,156-160,193-215 mô tả listener đọc frame, ghi log và phát sự kiện EnvelopeReceived. Không có pipeline lưu SQL trong lớp này. | Không có bằng chứng Central hiện xác thực, ghi dữ liệu hoặc ACK telemetry đã commit. |
| **FACT — Lịch sử** | docs/spec/PROTOCOL_SPEC.md:84-90,207,216-217; src/LabGuard.Protocol/MessageTypes.cs:68-82; docs/spec/CONTRACT_MATRIX.md:129-130 đặt GET_METRICS/METRICS_RESULT trong nhóm P0 OPTIONAL. | Không được coi truy vấn lịch sử là luồng bắt buộc P0 hoặc là API đã hoạt động. |
| **FACT — Kiểm thử** | tests/LabGuard.Agent.Tests/MetricsServiceTests.cs:64-76, tests/LabGuard.Central.Tests/CentralServerTests.cs:115-164 và tests/LabGuard.Protocol.Tests/Fixtures/ProtocolFixtures.cs:187-193 kiểm tra/đóng gói CPU, RAM. | Fixtures xác nhận hình dạng bản tin mẫu; chúng không chứng minh Disk/NIC/nhiệt độ, SQL persistence hoặc truy vấn lịch sử đã chạy. |
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
| CPU temperature | Nhiệt độ CPU package, nếu cảm biến xác nhận được | °C | Đề xuất CPU_TEMP_C. Trạng thái riêng: AVAILABLE, UNSUPPORTED, UNAVAILABLE hoặc READ_ERROR. Không suy diễn từ cảm biến bo mạch. |
| Uptime | Thời gian hệ điều hành hoạt động liên tục | giây, số nguyên 64-bit | Giữ trường UptimeSeconds của batch. |

**Network edge cases — PROPOSAL:** Lần đọc đầu chỉ tạo baseline, chưa có tốc độ đo; gửi trạng thái NO_BASELINE thay vì 0. Nếu counter giảm, interface đổi hoặc biến mất, không tính delta âm. Chỉ xử lý wraparound nếu nguồn counter xác nhận rõ độ rộng và semantics; nếu chưa xác nhận, coi là RESET, lấy baseline mới và báo RESET hoặc PARTIAL. Giá trị 0 B/s chỉ hợp lệ khi phép đo có hai mẫu hợp lệ, counter không đổi và adapter coverage đầy đủ. API .NET là một ứng viên cho bộ đếm per-interface, nhưng filter adapter và cách ổn định định danh phải được thử trên Windows trước khi chốt.

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

**PROPOSAL — Chọn Option A cho phạm vi tối thiểu đã giới hạn:** một ổ chứa hệ điều hành, RX/TX tổng hợp trên adapter vật lý đủ điều kiện, CPU temperature best-effort có trạng thái. Lý do: phạm vi sản phẩm hiện nêu metric nhưng chưa bắt buộc xem từng ổ hoặc adapter; bảng snapshot giúp nhóm NT106 kiểm thử một batch từ Agent qua Central đến SQL/Admin với ít logic truy vấn hơn. Đây không phải kết luận rằng bảng wide luôn đúng.

Option B trở thành lựa chọn ưu tiên nếu nhóm yêu cầu mọi volume/card riêng, nhiều cảm biến nhiệt, metric có tần suất khác nhau, hoặc truy vấn theo resource. Trong trường hợp đó ResourceId là bắt buộc và việc mở rộng Option A sẽ tạo nhiều cột/logic trạng thái khó duy trì hơn.

Candidate Option A columns: DiskUsedPercent, DiskFreeBytes, NetworkRxBytesPerSecond, NetworkTxBytesPerSecond, CpuTemperatureCelsius; các cột nullable. Trạng thái đo nhiệt độ và network cần được lưu riêng để phân biệt zero với unsupported/unavailable/reset. Tên/cột và cách biểu diễn trạng thái là ví dụ PROPOSED, không phải DDL hoặc thay đổi đã chấp thuận.

## 5. Hợp đồng, đảm bảo và tương thích

**PROPOSAL:** Dùng METRIC_BATCH/BATCH_ACK P0 cho mẫu hiện tại và mở rộng danh mục metric chỉ sau RFC được duyệt. Giữ nguyên frame 4-byte Big-Endian, envelope JSON, giới hạn 256 KiB, yêu cầu AUTH trước telemetry, khóa DeviceId+BootId+BatchId, replay tối đa 50 batch và ACK chỉ sau khi SQL commit. Không đổi độ bền hoặc ý nghĩa của batch để phục vụ chart.

DTO hiện tại có tên/value/unit/timestamp nhưng không có trạng thái đo hay resource identity. Candidate additive contract addition là trạng thái đo theo metric trong payload; MetricSampleDto tiếp tục chỉ đại diện giá trị số thực sự quan sát được. Khi status là NO_BASELINE/UNSUPPORTED/UNAVAILABLE/READ_ERROR/RESET, không tạo MetricSampleDto giả. SampleCount đếm các giá trị quan sát được; SamplesRecorded trong ACK đếm giá trị đã ghi bền vững. Nhóm phải quyết định enum/status, quy tắc cho batch cũ thiếu trường và giới hạn số mẫu trước khi sửa frozen DTO/spec.

Không tuyên bố thêm trường JSON là tương thích chỉ vì framing không đổi. Cần kiểm thử parser cũ/mới, serializer và fixture trên đúng .NET 9; phối hợp triển khai Central trước khi Agent phát tên metric mới. Kiểm tra payload lớn nhất vẫn dưới 256 KiB, giới hạn số sample và từ chối metric/unit không nằm trong allowlist.

RecordedAtUtc do Agent gửi là thời điểm đo phía client; nó không thay ReceivedUtc hoặc đồng hồ đơn điệu Central dùng cho liveness. Không dùng timestamp client để quyết định Agent online/offline. Nếu nhiều mẫu cùng batch không cùng timestamp, Option A không giữ được độ chính xác từng mẫu và phải đổi sang Option B hoặc thỏa thuận timestamp chung.

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
