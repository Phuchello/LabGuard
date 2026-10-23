# Phụ thuộc và kế hoạch tích hợp LabGuard

Nguồn nhiệm vụ/người phụ trách/trạng thái: [WBS 40 task](WBS_TASKS.csv), [phân công chính thức](TEAM_ASSIGNMENTS.md). Bộ hiện hành gồm 34 nhiệm vụ gốc và sáu nhiệm vụ bổ sung đã tiếp nhận. Lượt đồng bộ công nghệ chỉ cụ thể hóa B-008, C-003, C-004; giữ toàn bộ ID, người phụ trách, phụ thuộc, trạng thái, giờ lịch sử và bằng chứng nghiệm thu đã có. W1–W5 và các mốc tương đối trong CSV là ước lượng toàn đồ án, không phải ngày báo cáo GK.

## Các giai đoạn

| Giai đoạn | Phúc | Quang | Vy | Ngọc |
|---|---|---|---|---|
| 1. Kết nối/giao thức/UI ban đầu | A-001..A-003; startup PR #11 | B-001 listener, B-002 session/AUTH | C-001 thiết kế UI mới, C-002 Central client; mẫu ScottPlot SC-01 khi Vy duyệt | D-001 TCP, D-002 Echo, D-007 rà dữ liệu |
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

Hai công nghệ đã được đưa vào kế hoạch qua [PR #16](https://github.com/Phuchello/LabGuard/pull/16). Lượt đồng bộ ngày **23/10/2026** gắn sáu mốc dưới đây vào ba nhiệm vụ hiện có của [WBS](WBS_TASKS.csv) và [Excel](../../LabGuard_Task_Tracker.xlsx); không thêm task cấp cao. **SC-01..SC-03 và TC-01..TC-03 đều CHƯA LÀM (NOT STARTED)**; chưa cài package, chưa có mã hay nghiệm thu công nghệ. Phiên bản tham chiếu `ScottPlot.WinForms` **5.1.59** và `Testcontainers.MsSql` **4.16.0** cần kiểm tra tương thích khi triển khai, không phải gói đã cài hoặc bản mới nhất. Giữ .NET 9, bốn giai đoạn, đủ 40 ID, ownership, trạng thái, giờ lịch sử và phụ thuộc WBS cũ.

Theo yêu cầu cập nhật của người điều phối ngày 23/10/2026, **DashboardForm 0.1 là prototype có thể bỏ**. Nhóm dự kiến thiết kế lại Admin; không yêu cầu giữ bố cục hoặc control cũ. Giữ Windows Forms, project Admin, hợp đồng mạng và chức năng quản lý thiết bị/sự cố hợp lệ. UI mới chưa được hiện thực; đây không phải kết quả của PR #15 hoặc biên bản họp được tự tạo.

Ưu tiên: **xác thực + telemetry TCP + SQL persistence → binding Admin + quy trình sự cố → ScottPlot với dữ liệu đã xác minh → mở rộng test SQL khi mã tương ứng có thật → nghiệm thu toàn hệ thống**. Mẫu biểu đồ và nền schema-test có thể chuẩn bị độc lập sau khi owner tương ứng duyệt, không chặn phần cốt lõi khác. Việc chuẩn bị này không hoàn thành B-008 trước prerequisite B-005 trong WBS.

| Mốc / Task_ID | Chủ trì / sản phẩm và điều kiện | Kiểm tra dự kiến |
|---|---|---|
| SC-01 / LG-C-003 | **Vy**: thiết kế thành phần biểu đồ CPU/RAM dùng lại; chốt lịch sử giới hạn, refresh và chọn máy. Chuẩn bị mẫu có nhãn sau khi Vy duyệt, không cần giả kết nối thật | TEST-SC01: build Windows, timestamp/%, chọn máy, giới hạn điểm/số máy, nhãn mẫu và ảnh/log |
| SC-02 / LG-C-003, LG-C-004 | **Vy**: sau SC-01 và thiết kế UI C-001 được chốt, tích hợp vào Admin WinForms mới; tách nhận TCP và render, giữ quản lý thiết bị/sự cố | TEST-SC02: ngữ cảnh chọn máy/sự cố, quyền/quy trình hợp lệ, UI đáp ứng; không remote restart/lệnh tùy ý; ảnh/log Windows |
| SC-03 / LG-C-003, LG-C-004 | **Vy** phối hợp **Quang/Phúc**: sau SC-02, C-002, B-002/B-004 và đường telemetry B-005 sẵn sàng; giữ điều kiện B-003 của C-003 và B-006 của C-004 | TEST-SC03: chỉ nguồn Central đã xác thực/cho phép; đổi máy không lẫn dữ liệu, missing/stale/offline, thread/memory/độ đáp ứng; ảnh/log Windows |
| TC-01 / LG-B-008 | **Quang**: thiết kế project integration test opt-in riêng, SQL Server 2022 container được hỗ trợ và cấu hình chỉ trỏ DB test; chốt runtime/image/tương thích | TEST-TC01: DB cô lập, credential thử nghiệm, không truy cập production; unit test thường Docker-free, Docker không vào deployment |
| TC-02 / LG-B-008 | **Quang**: sau TC-01, áp dụng nguyên trạng DDL chuẩn và kiểm tra schema; chuẩn bị độc lập được nhưng chưa hoàn thành B-008 trước B-005 | TEST-TC02: ba bảng, PK/uniqueness/GUID/rowversion đúng DDL, không FK tự tạo; chạy lặp lại và cleanup cả khi lỗi/hủy |
| TC-03 / LG-B-008, kiểm chứng LG-B-005 | **Quang** phối hợp **Phúc**: sau TC-02 và mã B-005 persistence/giao dịch/chống trùng/đường ACK quan sát được | TEST-TC03: persistence/dedup/rollback bằng SQL thật; commit trước ACK, không ACK lỗi/chưa commit; isolation, cleanup, PASS/FAIL/NOT RUN |

**Ước lượng công sức bổ sung, chưa cam kết:** ScottPlot **6–9 giờ công** cho cả SC-01..SC-03/C-003/C-004; Testcontainers **7–10 giờ công** cho cả TC-01..TC-03/B-008. Tổng đề xuất **13–19 giờ công**, cần Vy/Quang rà phần trùng trước khi cam kết; không phân bổ lại các ngày/tuần cũ. WBS giữ C-003 = 3 giờ, C-004 = 3 giờ, B-008 = 2,5 giờ lịch sử. Khoảng bổ sung ghi ở phần diễn giải/ghi chú, không cộng hai lần C-003/C-004 hoặc thay Base_Hours/Contingency_Hours/Total_Hours. Hai section mốc trong Excel tham chiếu task gốc, không phải sáu task mới.

Các Test_ID mới là tham chiếu kế hoạch, **chưa phải tên test đã có hoặc kết quả PASS**. Giữ TEST-C03, TEST-C04, TEST-B08 và mọi bằng chứng lịch sử. Chưa có lệnh cài/chạy hai công nghệ trong lượt cập nhật này.

Khoảng ScottPlot chỉ là phần công bổ sung cho biểu đồ, không phải ước lượng đã chốt cho toàn bộ việc thiết kế lại Admin. Chủ nhiệm vụ cần rà cả phần cốt lõi/UI còn thiếu trước khi cam kết tiến độ.

### ScottPlot: phạm vi và nghiệm thu dự kiến

- C-003 chọn thiết bị và hiển thị CPU/RAM theo thời gian; C-004 giữ danh sách sự cố và ngữ cảnh máy đang chọn. SC-02 tích hợp vào UI thiết kế lại, không buộc tái sử dụng DataGridView/control prototype. Chức năng quản lý sự cố hợp lệ và hợp đồng mạng vẫn được giữ.
- SC-01: biểu đồ tương tác trong WinForms, nhãn **DỮ LIỆU MẪU**, timestamp và đơn vị CPU/RAM `%` rõ ràng. Vy chốt cửa sổ thời gian/giới hạn số điểm và số máy giữ trong bộ nhớ, khoảng refresh và chính sách bỏ điểm cũ; kiểm tra giới hạn khi cập nhật/đổi máy.
- SC-03: chỉ hiển thị số liệu thật từ nguồn Central đã xác thực và được phép; đổi máy không lẫn dữ liệu. Mất/cũ/chưa có số liệu phải hiện trạng thái và thời điểm cập nhật, không biến thành số 0 hoặc số liệu mẫu trá hình. OFFLINE do Central cung cấp không thay cho chất lượng dữ liệu.
- Mọi cập nhật control phải về UI thread qua cơ chế WinForms hiện hành; đồng bộ dữ liệu với render theo hướng dẫn thư viện, không chặn vòng nhận TCP. Nghiệm thu: biểu đồ render đúng, tương tác/đổi máy vẫn đáp ứng, không lỗi cross-thread; ghi log test và ảnh/kiểm tra trực quan trên Windows ở từng mốc, không dùng ảnh prototype thay bằng chứng tích hợp.
- Không thêm message type/DTO. SC-03 chỉ xây lịch sử giới hạn từ các mẫu có nguồn/thời điểm được xác minh; không coi ba event/poll là ba mẫu liên tiếp. Nếu dữ liệu hiện hành chưa đủ, hiện phần thiếu và phối hợp B, không tự tạo timestamp/API lịch sử. Truy vấn DB qua GET_METRICS/METRICS_RESULT cần duyệt riêng; Admin không truy cập SQL/Agent trực tiếp.
- **Hoãn/rollback:** nếu render gây giật, lỗi thread, vượt giới hạn bộ nhớ hoặc nguồn chưa hợp lệ, bảo toàn chức năng thiết bị/sự cố trong UI được chọn; hoãn binding thật, giữ mẫu có nhãn hoặc tắt riêng biểu đồ. Không buộc quay về layout prototype, không đổi hợp đồng để vượt chặn và không cản phần cốt lõi.

### Testcontainers: phạm vi và nghiệm thu dự kiến

- Chỉ môi trường **dev/test**, không đưa Docker/Testcontainers thành phụ thuộc production. Quang chọn/pin image SQL Server **2022** phù hợp và kiểm tra runtime trước khi chạy; không mặc nhiên dùng image mặc định của thư viện.
- Bộ test container **opt-in rõ ràng**, tách khỏi đường chạy unit test mặc định để unit test vẫn chạy không cần Docker. Chưa có lệnh bật/bộ test này trong repo. Thiếu hoặc không chạy được container runtime phải báo **NOT RUN + lý do**, không báo PASS; lỗi assertion/schema khi đã chạy là FAIL.
- Mỗi lượt tạo container/database mới như `LabGuardTestDb_<mã-lượt>` và áp dụng nguyên trạng [database/schema.sql](../../database/schema.sql). Chỉ dùng endpoint/credentials của container thử nghiệm; từ chối đích `LabGuardDb` hoặc cấu hình production. Không sửa database hiện hữu; DDL này dành cho database mới, không phải migration.
- Nền schema-test kiểm chứng ba bảng: `ProcessedBatches` có PK/uniqueness bộ `(DeviceId, BootId, BatchId)`; BootId/BatchId ở `ProcessedBatches` và `MetricSamples` là `UNIQUEIDENTIFIER`; `MetricSamples.SampleId` và `Incidents.IncidentId` là PK identity theo DDL; `Incidents.RowVersion` là token 8 byte đổi khi UPDATE, không phải timestamp. Không đòi FK/unique constraint chưa tồn tại trong schema.
- Sau B-005: kiểm chứng lưu METRIC_BATCH, gửi lại cùng khóa không tạo bản lưu trùng, lỗi giao dịch rollback; đối chiếu SQL và trace ACK thật để chứng minh **commit thành công trước BATCH_ACK**, và **không ACK batch chưa commit**. Không đánh dấu hoàn tất bằng test DDL hoặc Mock Harness khi mã nghiệp vụ chưa có.
- Nghiệm thu: SQL Server thật trong container nhận đúng schema; chạy lặp lại độc lập, teardown/dispose cả khi test lỗi/hủy, không để lại container/volume do test tạo và không chạm dữ liệu production. Ghi version/image, kết quả/log từng ca và bằng chứng cleanup; unit test không-container vẫn hoạt động.
- **Hoãn/rollback:** thiếu runtime thì hoãn lane container, giữ unit test; sai isolation/cleanup thì dừng lane này và chỉ dọn tài nguyên test đã xác định. Không trỏ test sang SQL production để thay thế, không đổi schema/protocol cho vừa test; giữ B-008 chưa nghiệm thu cho tới khi đủ bằng chứng.

### RFC chưa quyết định

[Issue #17](https://github.com/Phuchello/LabGuard/issues/17) vẫn là RFC nháp: Disk, Network RX/TX, CPU temperature, cách lưu SQL và các hợp đồng đóng băng chưa có quyết định thực hiện được cung cấp. Lượt này chỉ lập kế hoạch ScottPlot CPU/RAM và test DDL hiện hành. Thiết kế chart có thể chừa khả năng mở rộng, nhưng không tự đưa các metric/tên trường/schema đề xuất thành yêu cầu đã duyệt hoặc thay trạng thái task cũ. B-009 vẫn cần giải quyết chênh lệch kế hoạch/schema riêng.

Tham khảo chính thức khi hiện thực: [ScottPlot WinForms](https://scottplot.net/quickstart/winforms/), [đồng bộ render/async](https://scottplot.net/faq/async/), [Testcontainers MSSQL](https://dotnet.testcontainers.org/modules/mssql/). Đây là hướng dẫn kỹ thuật, không phải kết quả đo/test của LabGuard.

[PR #15 — safe-code-cleanup](https://github.com/Phuchello/LabGuard/pull/15) đang mở, chưa merge tại lúc rà kế hoạch; không đưa code/validation của PR đó vào baseline main. Vy phối hợp review UI riêng khi hiện thực chart, không coi cleanup đã xong hoặc ép nó thành điều kiện chặn mọi bước chuẩn bị. Lượt này không sửa PR #15.

## Verification

195 tests Windows local được báo cáo từ PR #11, không phải GitHub CI và không thay nghiệm thu chức năng hỗ trợ xử lý sự cố/LAN/SQL/Service. [Hướng dẫn Agent](../agent_deployment/README.md) và [test plan](../agent_deployment/DEPENDENCY_AND_TEST_PLAN.md). .NET 10 giữ là migration toàn solution riêng.
