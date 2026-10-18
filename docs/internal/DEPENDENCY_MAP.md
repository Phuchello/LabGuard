# Phụ thuộc và kế hoạch tích hợp LabGuard

Nguồn nhiệm vụ/người phụ trách/trạng thái: [WBS 40 task](WBS_TASKS.csv), [phân công chính thức](TEAM_ASSIGNMENTS.md). Giữ nguyên 34 task trước và tiếp nhận đủ sáu task local. Chuẩn hóa tên và phần diễn giải của sáu nhiệm vụ bổ sung; ID, người phụ trách, phụ thuộc, bằng chứng và trạng thái giữ nguyên. W1–W5 và các mốc tương đối trong CSV là ước lượng toàn đồ án, không phải ngày báo cáo GK.

## Các giai đoạn

| Giai đoạn | Phúc | Quang | Vy | Ngọc |
|---|---|---|---|---|
| 1. Kết nối/giao thức/UI ban đầu | A-001..A-003; startup PR #11 | B-001 listener, B-002 session/AUTH | C-001 layout, C-002 Central client | D-001 TCP, D-002 Echo, D-007 rà dữ liệu |
| 2. Thu/lưu/heartbeat/phục hồi | A-004, A-005 reconnect, A-006 replay | B-003 liveness, B-004 RBAC, B-005 persistence | C-003/C-004 thiết bị/sự cố | D-003 Relay, D-008 quy tắc bằng chứng |
| 3. Incident và hỗ trợ quyết định | A-007 service probe | B-006/B-007 rules/OCC, B-010 dữ liệu ngữ cảnh | C-005..C-008/C-010/C-011 quy trình xử lý, C-012 panel | D-009 thư viện nội bộ Admin |
| 4. Tích hợp và lỗi thực tế | A-008/A-009 | B-008/B-009 và kiểm tra SQL/ACK | Permission, selection, OCC và feedback | D-010 tích hợp/test sáu ca |
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

## Verification

195 tests Windows local được báo cáo từ PR #11, không phải GitHub CI và không thay nghiệm thu chức năng hỗ trợ xử lý sự cố/LAN/SQL/Service. [Hướng dẫn Agent](../agent_deployment/README.md) và [test plan](../agent_deployment/DEPENDENCY_AND_TEST_PLAN.md). .NET 10 giữ là migration toàn solution riêng.
