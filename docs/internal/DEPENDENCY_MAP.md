# Phụ thuộc và kế hoạch tích hợp LabGuard

Nguồn nhiệm vụ/người phụ trách/trạng thái: [WBS 40 task](WBS_TASKS.csv), [phân công chính thức](TEAM_ASSIGNMENTS.md). Giữ nguyên 34 task trước và tiếp nhận đủ sáu task local. Chuẩn hóa tên và phần diễn giải của sáu nhiệm vụ bổ sung; ID, người phụ trách, phụ thuộc, bằng chứng và trạng thái giữ nguyên. W1–W5 và các mốc tương đối trong CSV là ước lượng toàn đồ án, không phải ngày báo cáo GK.

## Các giai đoạn

| Giai đoạn | Phúc | Quang | Vy | Ngọc |
|---|---|---|---|---|
| 1. Kết nối/giao thức/UI ban đầu | A-001..A-003; startup PR #11 | B-001 listener, B-002 session/AUTH | C-001 layout, C-002 Central client; mẫu ScottPlot C-003/C-004 khi Vy duyệt | D-001 TCP, D-002 Echo, D-007 rà dữ liệu |
| 2. Thu/lưu/heartbeat/phục hồi | A-004, A-005 reconnect, A-006 replay | B-003 liveness, B-004 RBAC, B-005 persistence; nền schema-test B-008 khi Quang duyệt | C-003/C-004 thiết bị/sự cố | D-003 Relay, D-008 quy tắc bằng chứng |
| 3. Incident và hỗ trợ quyết định | A-007 service probe | B-006/B-007 rules/OCC, B-010 dữ liệu ngữ cảnh | C-005..C-008/C-010/C-011 quy trình xử lý, C-012 panel | D-009 thư viện nội bộ Admin |
| 4. Tích hợp và lỗi thực tế | A-008/A-009 | B-008/B-009 và kiểm tra SQL/ACK qua Testcontainers sau B-005 | Permission, selection, OCC và feedback; ScottPlot dữ liệu thật sau các điều kiện bên dưới | D-010 tích hợp/test sáu ca |
| Mở rộng có điều kiện | Review contract khi cần | Review backend khi cần | Không phụ thuộc bắt buộc | D-004 Gateway, D-005 Collector, D-006 Simulator: P1 tùy chọn |

Đây là kế hoạch; xem [PROJECT_STATE](../../PROJECT_STATE.md) cho phần đã có. A-005/A-006 còn CHƯA LÀM. B-005 và C-001 đang làm. PR #11 chỉ thêm startup/config/probe, không hoàn thành session, replay hay SQL.

## Điều kiện để luồng giám sát hoạt động đầy đủ

`Config/DeviceId → TCP → TLS/HELLO/AUTH → metric/heartbeat → Central validate/SQL transaction → BATCH_ACK → replay/dedup verification`.

Mỗi bước cần bằng chứng riêng. Central nhận HELLO không thay xác thực; đưa vào hàng đợi không chứng minh đã nhận; nhận dữ liệu chưa chứng minh đã ghi SQL/ACK. Khi phiên kết nối chưa sẵn sàng, Agent chặn giám sát. Chưa có bộ đọc phản hồi/kiểm tra mất kết nối phát hiện mọi lần phía bên kia đóng kết nối.

## chức năng hỗ trợ xử lý sự cố: dependencies bổ sung được giữ từ local

| Task | Owner | Prerequisites / điều kiện |
|---|---|---|
| LG-D-007 | D | LG-D-001; rà bắt buộc/tùy chọn, nguồn, độ mới và ba trục trạng thái |
| LG-D-008 | D | LG-D-003, LG-D-007; review semantics với B/C |
| LG-D-009 | D | LG-D-008; thống nhất dữ liệu ngữ cảnh với B-010, không tự bật tùy chọn query |
| LG-B-010 | B | B-002..B-007 và D-008; xác minh phần cốt lõi query/snapshot/event/quy trình xử lý trước ghép |
| LG-C-012 | C | C-002..C-008, C-010/C-011, B-010, D-009; phần cốt lõi Admin phải sẵn sàng |
| LG-D-010 | D | D-009, B-010, C-012; kiểm tra tích hợp sau khi phần cốt lõi sẵn sàng |

Chi tiết chính xác và acceptance criteria nằm trong CSV, không thay bằng bảng tóm tắt này. B-010 (2 giờ) và C-012 (3 giờ) chỉ là công sức ghép bổ sung; phần auth/persistence/quy trình xử lý còn thiếu phải tính riêng.

Riêng B-009 giữ yêu cầu lịch sử AssignedToUserId/IncidentHistory nhưng chưa phù hợp schema đóng băng dùng AssigneeId và chưa có IncidentHistory. Task vẫn CHƯA LÀM, cần Quang/nhóm giải quyết kế hoạch qua review/RFC thích hợp; không tự đổi schema trong housekeeping.

Chức năng hỗ trợ xử lý sự cố nhúng trong chi tiết sự cố của Admin: **Bằng chứng → Nhận định → Hướng kiểm tra → Thao tác xử lý**. B kiểm soát dữ liệu/lệnh qua Central; C giữ giao diện, quyền và kiểm tra xung đột cập nhật; D cung cấp giải thích và gợi ý có giới hạn. Không truy vấn Agent/SQL trực tiếp, không app TCP độc lập, không tự thay đổi hoặc đóng sự cố.

Giai đoạn đầu dùng dữ liệu thiết bị/sự cố bắt buộc và hiện thiếu/cũ. Optional GET_METRICS/METRICS_RESULT + lịch sử SQL chỉ xét khi được chọn, backend có thật và nhóm chấp thuận; không sửa frozen contract/schema trong housekeeping.

## Công nghệ đã duyệt — chưa hiện thực

Chỉ bổ sung cách thực hiện trong task có sẵn; **chưa cài package, chưa có mã/kiểm thử/nghiệm thu cho hai công nghệ**. Phiên bản tham chiếu theo brief: `ScottPlot.WinForms` **5.1.59** (ScottPlot 5) và `Testcontainers.MsSql` **4.16.0**; không coi đây là gói đã được cài hoặc bản mới nhất. Giữ .NET 9, bốn giai đoạn, 40 task và toàn bộ ID/trạng thái/ước lượng WBS/Excel hiện hành. Không cần thêm task chính thức.

Ưu tiên: **xác thực + telemetry TCP + SQL persistence → binding Admin + quy trình sự cố → ScottPlot với dữ liệu đã xác minh → mở rộng test SQL khi mã tương ứng có thật → nghiệm thu toàn hệ thống**. Mẫu biểu đồ và nền schema-test có thể chuẩn bị độc lập sau khi owner tương ứng duyệt, không chặn phần cốt lõi khác. Việc chuẩn bị này không hoàn thành B-008 trước prerequisite B-005 trong WBS.

| Mốc nằm trong task hiện có | Owner / mục đích, điều kiện bắt đầu | Ước lượng bổ sung để owner rà soát |
|---|---|---|
| C-003/C-004 — A: thành phần biểu đồ mẫu | **Vy (C)**; tái sử dụng trong dashboard đang có sau khi Vy duyệt; chọn máy bằng mẫu có nhãn rõ, không giả lập kết nối thật | 3–4 giờ |
| C-003/C-004 — B: telemetry thật | **Vy (C)**; sau C-002 và xác thực/phân quyền, dữ liệu thiết bị qua hợp đồng hiện hành; giữ phụ thuộc B-003 của C-003, B-006 của C-004 và ưu tiên binding/quy trình Admin | 3–5 giờ sau khi nguồn dữ liệu sẵn sàng |
| B-008 — nền kiểm thử schema | **Quang (B)**; sau khi Quang duyệt môi trường dev/test có Docker chạy được SQL Server 2022, dùng DDL chuẩn hiện có; chưa cần persistence C# để chuẩn bị bước này | 3–4 giờ |
| B-008 kiểm chứng B-005 — persistence/ACK | **Quang (B)**; B-005 có mã persistence/giao dịch/chống trùng và đường gửi ACK có thể quan sát; phối hợp Phúc cho framing/ACK, không chuyển ownership | 4–6 giờ sau khi mã sẵn sàng |

Đây là khoảng ước lượng sơ bộ cho phần bổ sung, **không thay hoặc tự cộng vào giờ WBS**, không phải hạn giao mới. Owner cần rà lại mức trùng với công việc hiện có trước khi cam kết; không đổi người phụ trách Phúc/Quang/Vy/Ngọc.

### ScottPlot: phạm vi và nghiệm thu dự kiến

- C-003 chọn thiết bị và hiển thị CPU/RAM theo thời gian; C-004 giữ bảng sự cố và ngữ cảnh máy đang chọn. Biểu đồ bổ trợ, không thay DataGridView/quy trình sự cố hoặc redesign Admin.
- Mốc A: biểu đồ tương tác trong WinForms, nhãn **DỮ LIỆU MẪU**, timestamp và đơn vị CPU/RAM `%` rõ ràng. Vy chốt cửa sổ thời gian/giới hạn số điểm và số máy giữ trong bộ nhớ, chính sách bỏ điểm cũ; kiểm tra giới hạn không bị vượt khi cập nhật/đổi máy.
- Mốc B: chỉ hiển thị số liệu thật từ nguồn Central đã xác thực và được phép; đổi máy không lẫn dữ liệu. Mất/cũ/chưa có số liệu phải hiện trạng thái và thời điểm cập nhật, không biến thành số 0 hoặc số liệu mẫu trá hình.
- Mọi cập nhật control phải về UI thread qua cơ chế WinForms hiện hành; đồng bộ dữ liệu với render theo hướng dẫn thư viện, không chặn vòng nhận TCP. Nghiệm thu: biểu đồ render đúng, tương tác/đổi máy vẫn đáp ứng, không lỗi cross-thread; ghi log test và ảnh/kiểm tra trực quan trên Windows ở từng mốc, không dùng ảnh prototype thay bằng chứng tích hợp.
- Không thêm message type/DTO. Mốc B có thể dùng lịch sử giới hạn trong bộ nhớ từ telemetry được phép; truy vấn lịch sử DB chỉ triển khai nếu thật sự cần và được duyệt riêng, không mặc nhiên bật GET_METRICS/METRICS_RESULT hoặc cho Admin truy cập SQL/Agent trực tiếp.
- **Hoãn/rollback:** nếu render gây giật, lỗi thread, vượt giới hạn bộ nhớ hoặc nguồn chưa hợp lệ, giữ bảng thiết bị/sự cố hiện có; hoãn binding thật, giữ mẫu có nhãn hoặc tắt riêng biểu đồ trong đợt triển khai sau. Không thay hợp đồng để vượt chặn và không cản nghiệm thu phần cốt lõi.

### Testcontainers: phạm vi và nghiệm thu dự kiến

- Chỉ môi trường **dev/test**, không đưa Docker/Testcontainers thành phụ thuộc production. Quang chọn/pin image SQL Server **2022** phù hợp và kiểm tra runtime trước khi chạy; không mặc nhiên dùng image mặc định của thư viện.
- Bộ test container **opt-in rõ ràng**, tách khỏi đường chạy unit test mặc định để unit test vẫn chạy không cần Docker. Chưa có lệnh bật/bộ test này trong repo. Thiếu hoặc không chạy được container runtime phải báo **NOT RUN + lý do**, không báo PASS; lỗi assertion/schema khi đã chạy là FAIL.
- Mỗi lượt tạo container/database mới như `LabGuardTestDb_<mã-lượt>` và áp dụng nguyên trạng [database/schema.sql](../../database/schema.sql). Chỉ dùng endpoint/credentials của container thử nghiệm; từ chối đích `LabGuardDb` hoặc cấu hình production. Không sửa database hiện hữu; DDL này dành cho database mới, không phải migration.
- Nền schema-test kiểm chứng ba bảng: `ProcessedBatches` có PK/uniqueness bộ `(DeviceId, BootId, BatchId)`; BootId/BatchId ở `ProcessedBatches` và `MetricSamples` là `UNIQUEIDENTIFIER`; `MetricSamples.SampleId` và `Incidents.IncidentId` là PK identity theo DDL; `Incidents.RowVersion` là token 8 byte đổi khi UPDATE, không phải timestamp. Không đòi FK/unique constraint chưa tồn tại trong schema.
- Sau B-005: kiểm chứng lưu METRIC_BATCH, gửi lại cùng khóa không tạo bản lưu trùng, lỗi giao dịch rollback; đối chiếu SQL và trace ACK thật để chứng minh **commit thành công trước BATCH_ACK**, và **không ACK batch chưa commit**. Không đánh dấu hoàn tất bằng test DDL hoặc Mock Harness khi mã nghiệp vụ chưa có.
- Nghiệm thu: SQL Server thật trong container nhận đúng schema; chạy lặp lại độc lập, teardown/dispose cả khi test lỗi/hủy, không để lại container/volume do test tạo và không chạm dữ liệu production. Ghi version/image, kết quả/log từng ca và bằng chứng cleanup; unit test không-container vẫn hoạt động.
- **Hoãn/rollback:** thiếu runtime thì hoãn lane container, giữ unit test; sai isolation/cleanup thì dừng lane này và chỉ dọn tài nguyên test đã xác định. Không trỏ test sang SQL production để thay thế, không đổi schema/protocol cho vừa test; giữ B-008 chưa nghiệm thu cho tới khi đủ bằng chứng.

Tham khảo chính thức khi hiện thực: [ScottPlot WinForms](https://scottplot.net/quickstart/winforms/), [đồng bộ render/async](https://scottplot.net/faq/async/), [Testcontainers MSSQL](https://dotnet.testcontainers.org/modules/mssql/). Đây là hướng dẫn kỹ thuật, không phải kết quả đo/test của LabGuard.

[PR #15 — safe-code-cleanup](https://github.com/Phuchello/LabGuard/pull/15) đang mở, chưa merge tại lúc rà kế hoạch; không đưa code/validation của PR đó vào baseline main. Vy phối hợp review UI riêng khi hiện thực chart, không coi cleanup đã xong hoặc ép nó thành điều kiện chặn mọi bước chuẩn bị. Lượt này không sửa PR #15.

## Verification

195 tests Windows local được báo cáo từ PR #11, không phải GitHub CI và không thay nghiệm thu chức năng hỗ trợ xử lý sự cố/LAN/SQL/Service. [Hướng dẫn Agent](../agent_deployment/README.md) và [test plan](../agent_deployment/DEPENDENCY_AND_TEST_PLAN.md). .NET 10 giữ là migration toàn solution riêng.
