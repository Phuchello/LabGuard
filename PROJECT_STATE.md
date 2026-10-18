# LabGuard — Trạng thái dự án

Cập nhật tài liệu ngày 18/10/2026 trên main sau [PR #11](https://github.com/Phuchello/LabGuard/pull/11), merge commit `70481faf6c04d2862815e2f3fb3c38de759c459f`. Không dùng nhãn task để suy luận toàn hệ thống đã chạy.

## Đã hiện thực và phần còn thiếu

| Phần | Trạng thái đúng phạm vi |
|---|---|
| A-001..A-004 | XONG ở cấp protocol/transport/collector và producers; lịch sử nghiệm thu giữ nguyên |
| Agent startup PR #11 | Đã có async launcher, strict config, per-installation DeviceId, local Windows metrics, development TCP probe, cancellation/log |
| Monitoring Agent | Cố ý bị chặn exit 5 đến khi TLS/session/AUTH sẵn sàng; không gửi heartbeat/metric chỉ vì socket connected |
| A-005 / A-006 | CHƯA LÀM: reconnect / ReplayBuffer; launcher không thay hai task này |
| B-001 | XONG: listener nhiều client, FrameCodec và log nhận frame |
| B-005 | ĐANG LÀM: DDL ba bảng đã merge; C# persistence và SQL toàn tuyến chưa nghiệm thu |
| B-002..B-004, B-006..B-009 | CHƯA LÀM: auth, liveness, RBAC, rules, incident/OCC và kiểm tra SQL |
| C-001 | ĐANG LÀM: layout WinForms 0.1; binding/workflow chưa nghiệm thu |
| C-002..C-011 trong WBS | CHƯA LÀM: kết nối Central và quản lý sự cố; giữ các ID hiện có, không tạo C-009 |
| D-002 / mock harness | Echo XONG (PR #8); mock tooling đã merge PR #9, không thay D-003 |
| D-001 / D-003 | Giữ CHƯA LÀM do chưa có hồ sơ nghiệm thu tương ứng |
| D-007..D-010, B-010, C-012 | Kế hoạch Assistant chính thức, tất cả CHƯA LÀM; không có chức năng mới đã được chứng minh |
| D-004..D-006 | P1 tùy chọn: Gateway/Collector/Simulator sau khi phần cốt lõi ổn định; giữ Simulator placeholder |
| Windows Service / .NET 10 | Chưa triển khai; Service chờ startup/reconnect, migration .NET 10 là việc riêng toàn solution |

Năm mốc độc lập: **TCP connected → application authenticated → telemetry received → SQL committed → application ACK received**. Probe mới kiểm tra transport/HELLO; chưa có reader/liveness bảo đảm phát hiện peer đóng im lặng. Không có bằng chứng triển khai nhiều PC LAN, Service hay monitoring production.

## Lịch sử đã có — không xóa kết quả cũ

| Mốc | Bằng chứng/lưu ý |
|---|---|
| A-001/A-002 | Protocol 107 tests; 16 canonical fixtures và FrameCodec. [Đặc tả](docs/spec/PROTOCOL_SPEC.md) đóng băng |
| A-003 | Writer queue 256, một writer; 16 test transport ban đầu |
| A-004 | 18 xUnit cases; Agent tổng lúc đó 34. [Hồ sơ validation](docs/evidence/A004_METRICS_VALIDATION.md), [implementation notes](docs/evidence/A004_IMPLEMENTATION_NOTES.md) giữ nguyên kết quả Windows reference 10/10 của lượt lịch sử |
| PR #4 | Admin layout 0.1; [ảnh nghiệm thu layout](docs/evidence/admin_dashboard_c001.png), chưa hoàn tất C-001 |
| PR #5 | `database/schema.sql` DDL; không phải toàn bộ SQL integration |
| PR #7 | Central B-001, 9 tests |
| PR #8 | TcpEcho D-002 |
| PR #9 | FakeAgent/FakeCentral và 15 MockHarness tests |
| PR #10 | Đồng bộ tiến độ trước startup; baseline lúc đó 165 tests |
| PR #11 | Startup/config/probe và 30 test cases mới; Agent 64, solution 195 tests |

**Verification gần nhất được báo cáo từ PR #11:** 195 passed trên Windows (Protocol 107, Agent 64, Central 9, MockHarness 15), build 0 warning/error. Đây là local verification, không phải GitHub CI. [Evidence/reproduction](docs/agent_deployment/VALIDATION_EVIDENCE.md). Số 165/34 trong hồ sơ cũ là mốc lịch sử, không phải số test hiện tại.

## Giải quyết chênh lệch WBS và phân công

Base GitHub có 34 task; checkout gốc có 40. Đối chiếu cho thấy 34 task chung giống mọi trường, không có task bị thiếu hoặc sửa trạng thái. Lượt đồng bộ giữ nguyên 34 task, tiếp nhận đủ sáu task local: LG-D-007..LG-D-010, LG-B-010, LG-C-012. Chỉ chuẩn hóa mô tả trách nhiệm của Ngọc và cách viết 2 giờ/3 giờ ở D-007/B-010/C-012; không đổi ID, người phụ trách, phụ thuộc, bằng chứng hoặc trạng thái. Kết quả: 40 task, 6 XONG, 2 ĐANG LÀM, 32 CHƯA LÀM. Excel gốc giống bản GitHub nhưng chưa có sáu task; đồng bộ sáu dòng vào các sheet thành viên, giữ lịch sử cũ.

Phân công theo [quyết định trực tiếp của người điều phối 17/10/2026](docs/internal/TEAM_ASSIGNMENTS.md): Phúc Agent/Protocol, Quang Central/SQL, Vy Admin WinForms, Ngọc Công cụ kiểm thử truyền thông TCP và chức năng hỗ trợ phân tích, xử lý sự cố. Đây là quyết định chính thức, không phải đề xuất chờ duyệt; không tự tạo biên bản/chữ ký.

**Xung đột kế hoạch cũ còn cần quyết định:** LG-B-009 trong 34 dòng lịch sử yêu cầu `AssignedToUserId` và `dbo.IncidentHistory`; schema đóng băng hiện dùng `Incidents.AssigneeId`, chưa có IncidentHistory. Giữ nguyên dòng/ID/evidence/status CHƯA LÀM để không sửa ngầm hợp đồng kế hoạch. Không coi tên cũ là schema hiện hành hoặc tự thêm cột/bảng. Quang/nhóm cần đối chiếu và chọn chỉnh kế hoạch theo schema hoặc theo quy trình RFC riêng trước khi hiện thực B-009. Excel/handbook đã chỉ rõ giới hạn này.

Assistant là logic gợi ý dựa bằng chứng bên trong quy trình xử lý sự cố Admin, dùng dữ liệu Central có thẩm quyền. Quang/Vy giữ trách nhiệm phần cốt lõi. Hai giờ B-010 và ba giờ C-012 chỉ là ghép bổ sung sau khi phần cốt lõi sẵn sàng; cần tính riêng core còn thiếu. Không tự kích hoạt optional history hoặc đổi schema/protocol.

## Kiểm tra housekeeping

Đã thực chạy trên worktree housekeeping, Windows ngày 18/10/2026:

| Kiểm tra | Kết quả thực tế |
|---|---|
| Admin resources | Bỏ references wrench/wrench1, build Admin thành công trước khi xóa hai PNG; giữ wrench2 đang dùng |
| Solution build/test | Exit 0; 0 warnings/errors; 195 passed, 0 failed/skipped (Protocol 107, Agent 64, Central 9, MockHarness 15). Đây là local, không phải CI |
| WBS | Giữ nguyên mọi trường của 34 task cũ; tiếp nhận sáu task local, chỉ chuẩn hóa ba ô mô tả; 40 task, 6 XONG / 2 ĐANG LÀM / 32 CHƯA LÀM |
| Excel | Mở được bằng Excel Desktop read-only; năm trang tính giữ nguyên tên/thứ tự, 40 task/status khớp CSV; không có công thức trước/sau; đã xem render năm trang tính, không đổi ô ngoài phạm vi |
| Liên kết/phạm vi | 23 Markdown được kiểm tra file/anchor: không link hỏng; không tracked build outputs/cache hoặc secret-pattern match; schema/protocol/TFM/Simulator/tools/tests giữ nguyên |
| Bảo toàn | 14 file gốc local đối chiếu hash không đổi, gồm WBS/phân công/Excel gốc, SQL report và bộ GK đã duyệt |
| Handbook | Năm PDF đã build từ nguồn cuối và kiểm tra ảnh render đủ 98 trang: tổng quan 28, A 23, B 17, C 15, D 15; không lỗi glyph/tham chiếu hoặc chữ bị cắt. A có một cảnh báo vượt dòng 0,63 pt, đã kiểm tra không ảnh hưởng nội dung |

Năm PDF trong `docs/pdf/` đã được thay đồng bộ với nguồn LaTeX cuối sau khi kiểm tra. Script build tạo bản mới ở staging, dừng khi compiler lỗi và không tự ghi đè PDF chính thức. Chỉ dọn hai tài nguyên WinForms không dùng; không sửa logic ứng dụng, schema, giao thức hoặc phiên bản .NET.
Logs/TRX, hash manifests và ảnh render được giữ ở scratch local để tiếp tục, không commit artifacts QA. B-009 vẫn là mục kế hoạch cần Quang/nhóm quyết định như mô tả ở trên.
