# LabGuard — Việc tiếp theo

Nguồn kế hoạch: [WBS 40 task](docs/internal/WBS_TASKS.csv), [tracker Excel](LabGuard_Task_Tracker.xlsx), [dependencies](docs/internal/DEPENDENCY_MAP.md). Tên người phụ trách theo [quyết định 17/10/2026](docs/internal/TEAM_ASSIGNMENTS.md). [PROJECT_STATE](PROJECT_STATE.md) giữ lịch sử nghiệm thu.

## Đã có, không mở lại task để ghi tiến độ giả

- [x] A-001..A-004: protocol, framing, transport/outbox và metrics/producers ở cấp component.
- [x] B-001: Central TCP listener (PR #7).
- [x] D-002: TCP Echo (PR #8); mock harness bổ trợ đã merge PR #9.
- [x] PR #11: Agent launcher/config/DeviceId/local collect/development probe. **Không đồng nghĩa A-005/A-006 XONG.**

## Phúc — Agent & Protocol

- [ ] Session reader/TLS/HELLO/AUTH phối hợp Quang; chỉ khởi chạy monitoring khi phiên hợp lệ.
- [ ] A-005 reconnect/backoff; A-006 ReplayBuffer/ACK; A-007 service probe; A-008/A-009 lỗi mạng/LAN theo WBS.
- [ ] Windows Service sau khi startup/shutdown/log/reconnect được xác minh; không yêu cầu Service cho Console thử nghiệm.

## Quang — Central & SQL Server

- [ ] B-002..B-004: AUTH, liveness, RBAC.
- [ ] B-005 đang làm: hoàn thiện persistence/SQL, transaction/dedup và ACK sau commit; DDL đã có.
- [ ] B-006..B-009: rules, incident/OCC và kiểm tra SQL.
- [ ] Trước B-009: giải quyết yêu cầu lịch sử AssignedToUserId/IncidentHistory không khớp schema AssigneeId hiện tại; không tự thêm cột/bảng hoặc đánh dấu DONE.
- [ ] B-010: context có thẩm quyền cho Assistant; 2 giờ ghép bổ sung không bao gồm core chưa sẵn sàng.

## Vy — Admin WinForms

- [ ] C-001 đang làm; C-002 kết nối Central; C-003..C-008, C-010/C-011 grids/workflow/permission/OCC.
- [ ] C-012: nhúng Assistant trong chi tiết incident; 3 giờ ghép không bao gồm networking/workflow còn thiếu.

## Ngọc — Công cụ kiểm thử truyền thông TCP và chức năng hỗ trợ phân tích, xử lý sự cố

- [ ] D-001 hồ sơ hiểu TCP/fixtures và D-003 Relay; giữ lịch sử D-002 XONG.
- [ ] D-007 rà nguồn/missing/stale evidence; D-008 thiết kế quy tắc; D-009 logic nội bộ Admin; D-010 tích hợp và xác minh cùng B/C.
- [ ] Luồng Bằng chứng → Nhận định → Hướng kiểm tra → Thao tác xử lý; không app độc lập, không query SQL/Agent trực tiếp, không tự sửa hoặc Resolve.
- [ ] History SQL qua GET_METRICS/METRICS_RESULT là optional riêng; trước mắt hiện rõ dữ liệu thiếu.
- [ ] D-004..D-006 Gateway/Collector/Simulator chỉ xét sau khi phần cốt lõi ổn định, không phải cam kết chính của Ngọc.

## Toàn nhóm

- [ ] Tích hợp năm mốc TCP → authenticated → telemetry received → SQL committed → ACK, với bằng chứng cho từng mốc.
- [ ] Kiểm tra đa máy LAN được phép, mất mạng, reconnect, Lost ACK/replay/dedup và lỗi SQL sau khi code sẵn sàng.
- [ ] Migration .NET 10 toàn solution là việc riêng; không đổi TFM trong housekeeping này.

Verification được báo cáo từ PR #11: **195 tests passed trên Windows local**, không phải GitHub CI. Chạy lại bằng [script](docs/agent_deployment/verify_agent.ps1). Không biến kế hoạch toàn đồ án thành lịch chuẩn bị PowerPoint 19–26/10.
