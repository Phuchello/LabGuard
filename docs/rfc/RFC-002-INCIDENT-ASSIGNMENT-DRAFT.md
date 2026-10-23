# RFC-002 — Định danh người được phân công sự cố

**Trạng thái:** DRAFT — PENDING quyết định của nhóm
**Cơ sở đối chiếu:** main tại a90ef8d1fcab0f1c18e363cbcd77d61e1d8dd2dc
**Phạm vi:** Làm rõ kiểu định danh, bảng người dùng, lưu người nhận xử lý và lịch sử giao việc. Không triển khai schema hoặc mã Central/Admin.

Các nhãn **FACT**, **INFERENCE** và **PROPOSAL** lần lượt chỉ dữ kiện repository, suy luận và phương án chờ duyệt. Issue #17 là RFC cũ đang mở, không phải biên bản họp hoặc bằng chứng phê duyệt B-009.

## 1. Hiện trạng và điểm không khớp

| Nội dung | Bằng chứng | Kết luận |
|---|---|---|
| WBS | docs/internal/WBS_TASKS.csv:19 (LG-B-009) yêu cầu AssignedToUserId, IncidentHistory, INCIDENT_ASSIGN và FK tới dbo.Users. Task còn CHƯA LÀM. | Đây là yêu cầu kế hoạch, không phải schema hay code hiện có. |
| Schema | database/schema.sql:43-57 có Incidents.AssigneeId NVARCHAR(64), RowVersion ROWVERSION; 11-23 có ProcessedBatches; toàn file chỉ khai báo ProcessedBatches, MetricSamples, Incidents. Không có Users, ProcessedCommands hoặc IncidentHistory. Dòng 59-62 ghi một số phần chưa được hiện thực. | Không có account table, assignment history hay persisted command dedup table trong canonical DDL hiện tại. |
| DTO danh tính | src/LabGuard.Protocol/Dtos/AdminDtos.cs:9-16 LoginResultPayload.UserId là int?; dòng 65-75 IncidentSummaryDto.AssignedToUserId cũng int?. | Wire summary và login response cùng gợi ý kiểu số nguyên. Đây chưa chứng minh nguồn tài khoản tồn tại. |
| Authentication plan / seed | docs/spec/PROTOCOL_SPEC.md:74-82,205-206 mô tả LOGIN/session và bootstrap Admin; database/seed.sql:1-6 tuyên bố chưa có dữ liệu seed, tài khoản hoặc mật khẩu. | Đặc tả yêu cầu danh tính nhưng không có tài khoản khởi tạo hay seed thực tế trong baseline. |
| Lệnh và RequestDigest | src/LabGuard.Protocol/Dtos/AdminDtos.cs:87-103 định nghĩa `CommandParams` dạng Dictionary; docs/spec/PROTOCOL_SPEC.md:149-164 và docs/spec/CONTRACT_MATRIX.md:201-212 yêu cầu digest từ JSON chuẩn hóa có đủ các trường. src/LabGuard.Protocol/Serialization/ProtocolSerializer.cs:49-86 thực hiện chuỗi phân cách `\|` trên các tham số chọn lọc. | Có sai lệch giữa hợp đồng đóng băng và helper hiện có; xem phân tích RequestDigest bên dưới. |
| Dedup lệnh | docs/spec/PROTOCOL_SPEC.md:149-165 giữ khóa UserId+CommandId, RequestDigest và replay kết quả; docs/spec/CONTRACT_MATRIX.md:201-212 mô tả UserId NVARCHAR(64). | Kiểu UserId trong tài liệu vẫn khác gợi ý int của DTO; cần chốt đồng bộ cùng thuật toán digest trước khi hiện thực ProcessedCommands. |
| Quyền | docs/spec/PROTOCOL_SPEC.md:177-194 quy định INCIDENT_ASSIGN chỉ Admin; Operator không được phép. | Kiểm tra quyền bắt buộc tại Central theo danh tính session đã xác thực, không tin UserId do client tự gửi. |
| OCC | docs/spec/PROTOCOL_SPEC.md:166-175, docs/spec/CONTRACT_MATRIX.md:225-238, database/schema.sql:55 mô tả RowVersion và xung đột 409. | Cột RowVersion đã có trong DDL nhưng không có handler thực hiện UPDATE với expected version trong Central hiện tại. |
| Phản hồi / sự kiện | AdminDtos.cs:96-104 CommandResultPayload trả success/status/error/UpdatedRowVersionHex, không có người được gán; src/LabGuard.Protocol/Dtos/EventDtos.cs:48-56 IncidentUpdatePayload không có assignee. | Cần quyết định refresh/list response hoặc bổ sung trường/sự kiện khi assignment thành công. |
| Central/auth thực tế | Central source hiện chỉ có src/LabGuard.Central/Program.cs, src/LabGuard.Central/CentralServer.cs và src/LabGuard.Central/LabGuard.Central.csproj. CentralServer.cs:9-13,156-160,193-215 đọc frame, ghi log rồi phát EnvelopeReceived; nhánh LOGIN tại 198-200 chỉ ghi log. Không có IncidentAssign, repository hay Auth handler. | Đăng nhập, kiểm tra quyền, transaction, assignment và OCC chưa được chứng minh là hoạt động. |
| Fixtures/tests | tests/LabGuard.Protocol.Tests/Fixtures/incident_ack.json chỉ là lệnh ACK; tests/LabGuard.Protocol.Tests/Fixtures/command_result.json là response mẫu. tests/LabGuard.Protocol.Tests/Fixtures/ProtocolFixtures.cs:333-345 có LoginResult mẫu UserId=1; tests/LabGuard.Protocol.Tests/ProtocolSpecificationTests.cs:74-155 kiểm tra known vector chuỗi `\|` và chuẩn hóa helper, không tạo vector từ canonical JSON trong spec. Không có kiểm thử Central cho assignment. | Các test hiện xác nhận thuật toán của helper, không xác nhận helper tuân thủ hợp đồng JSON, account, FK, lịch sử hoặc xử lý lệnh. |
| Kế hoạch liên quan | docs/internal/WBS_TASKS.csv:17 (LG-B-007/OCC), 19 (LG-B-009), 27 (LG-C-008), 36-41 (LG-D-007..LG-D-010, LG-B-010, LG-C-012); docs/internal/DEPENDENCY_MAP.md:36. | Phải chờ xác thực, persistence, lifecycle và giao diện lõi; estimate LG-B-009/LG-C-008 không tự bao gồm phần lõi còn thiếu. |

## 2. Vấn đề, yêu cầu và giới hạn

**FACT:** SQL hiện có assignee dạng chuỗi; DTO và serializer có định danh dạng số; đặc tả idempotency lưu UserId chuỗi. Không có bảng tài khoản để kiểm tra ID, bảng lịch sử hoặc handler lệnh thực tế.
**INFERENCE:** Chỉ đổi tên cột hoặc ép kiểu sẽ để lại sai lệch giữa đăng nhập, session, digest, FK, UI và lịch sử.
**PROPOSAL:** Chọn một account identity ổn định, dùng xuyên LOGIN_RESULT, session Central, INCIDENT_ASSIGN, Incidents, IncidentHistory và ProcessedCommands.

Yêu cầu: giữ INCIDENT_ACK, INCIDENT_ASSIGN, INCIDENT_START, INCIDENT_RESOLVE; giữ vai trò Viewer/Operator/Admin; giữ expectedRowVersion, 409 stale-update và khóa idempotency UserId+CommandId + RequestDigest. Chỉ lệnh hợp lệ, có quyền và cập nhật thành công mới ghi lịch sử. Người gán phải lấy từ session đã xác thực. Mọi thao tác DB và lịch sử của cùng mutation phải nguyên tử.

Không thuộc RFC: thay đổi luồng xử lý sự cố, thêm vai trò, tạo hệ thống ticket thứ hai, chỉnh frozen DTO/spec/SQL ngay, hoặc giả định đăng nhập đã hiện thực.

### RequestDigest implementation versus frozen contract

**FACT — Hợp đồng đóng băng:** `docs/spec/PROTOCOL_SPEC.md:149-164` và `docs/spec/CONTRACT_MATRIX.md:201-212` yêu cầu deserialize `CommandPayloadDto`, chuẩn hóa toàn bộ `commandParams`, rồi tuần tự hóa JSON UTF-8 không khoảng trắng theo thứ tự `commandId`, `commandParams`, `commandType`, `expectedRowVersion`, `incidentId` và băm SHA-256. Quy tắc gồm sắp xếp khóa, biểu diễn null, Unicode NFC và dạng số chuẩn.

**FACT — Mã hiện tại:** `src/LabGuard.Protocol/Serialization/ProtocolSerializer.cs:49-86` băm UTF-8 của chuỗi `commandType|incidentId|expectedRowVersion|assignedToUserId|resolutionNote|commandId`. Helper chuẩn hóa một số trường (type viết hoa, rowversion chữ thường/pad, user ID số, note Trim, UUID chuẩn) nhưng không nhận `CommandParams` nguyên bản, không tạo JSON canonical và không chuẩn hóa chuỗi theo NFC. DTO tại `src/LabGuard.Protocol/Dtos/AdminDtos.cs:87-103` chứa `Dictionary<string, object?> CommandParams`.

**FACT — Mức áp dụng hiện tại:** `rg` trên `src` và `tests` tìm thấy helper được gọi trong các test giao thức, chưa có caller sản phẩm. `CentralServer` chỉ đọc frame/ghi log/phát sự kiện; `database/schema.sql` không có `ProcessedCommands` và ghi rõ một số phần chưa triển khai. Vì vậy, đây là lỗi khớp hợp đồng ở mã helper, chưa phải bằng chứng một luồng idempotency đang bị khai thác trong hệ thống vận hành.

**Tác động và mức độ — INFERENCE:** Hợp đồng áp dụng cho `INCIDENT_ACK`, `INCIDENT_ASSIGN`, `INCIDENT_START` và `INCIDENT_RESOLVE`. Helper không thể bao phủ bất kỳ tham số nghiệp vụ nào ngoài những giá trị được chuyển vào chữ ký; hiện không có caller để xác định caller tương lai sẽ bỏ sót tham số nào. Test `RequestDigest_Normalizations_AreConsistent` (`tests/LabGuard.Protocol.Tests/ProtocolSpecificationTests.cs:96-155`) chứng minh một khác biệt thực tế: note có khoảng trắng đầu/cuối bị Trim thành cùng digest. Mã cũng ánh xạ cả `null` và chuỗi rỗng thành chuỗi rỗng trước khi băm; theo quy tắc frozen JSON, null khác chuỗi rỗng, nên hai payload khác nhau có thể cùng digest. Chuỗi được chuẩn hóa NFC nhưng không bị Trim; chiều ngược lại, dạng Unicode tương đương sau NFC có thể cho digest khác vì helper không chuẩn hóa NFC. Hàm nhận `assignedToUserId` đã có kiểu `int?`, nên không thể kiểm tra loại token JSON gốc là số hay chuỗi; nếu caller tự chuyển chuỗi số sang int thì có thể làm mất khác biệt, nhưng chưa thấy caller để kết luận hành vi đó đang xảy ra. Khóa/thứ tự thuộc tính và khoảng trắng định dạng JSON thô không được truyền vào helper, vì vậy chúng không ảnh hưởng kết quả; điều này chưa chứng minh canonicalization đầy đủ do helper không nhận các trường còn lại của payload. Test hiện cũng không xác nhận mọi thay đổi trong `commandParams` đều làm đổi digest.

Chuỗi phân cách `|` không escape/length-prefix các trường. Ghi chú tự do có thể chứa `|`; helper cũng nhận commandType/expectedRowVersion dưới dạng chuỗi mà không tự kiểm tra allowlist/định dạng hex. Chưa chứng minh được va chạm giữa hai payload hợp lệ theo miền kiểu dữ liệu trong frozen contract chỉ từ ký tự này, vì phần cuối là UUID chuẩn và các trường trước note có kiểu hẹp. Rủi ro delimiter tăng nếu caller truyền chuỗi ngoài hợp đồng mà không xác thực trước khi ghép; cần vector boundary riêng. Không ghi nhận một khai thác va chạm đã xảy ra.

**Phân loại:** Lỗi sai khớp hợp đồng ở helper là **FACT**; cần ưu tiên sửa trước khi đưa dedup vào sử dụng, nhưng ảnh hưởng runtime hiện tại là **INFERENCE** vì chưa có Central handler/DB để xác nhận. Không dùng việc các test hiện tại thành công làm bằng chứng tuân thủ spec: known-vector test tại `tests/LabGuard.Protocol.Tests/ProtocolSpecificationTests.cs:74-94` tự tính SHA-256 trên đúng chuỗi phân cách `|` mà helper đang dùng.

| Phương án | Đánh giá |
|---|---|
| **RD-A — hiện thực hợp đồng JSON canonical đang đóng băng** | Giữ nguyên tài liệu đã duyệt; sau khi chủ sở hữu giao thức xác nhận, sửa helper để nhận/chuẩn hóa toàn bộ payload, bổ sung vector độc lập cho thứ tự field/key, khoảng trắng JSON, null/rỗng, Unicode NFC, kiểu số/chuỗi, giá trị số và từng tham số của cả bốn operation. Đây là phương án được khuyến nghị vì hợp đồng đã định nghĩa rõ biểu diễn và bao phủ `commandParams`. |
| **RD-B — sửa hợp đồng sang thuật toán khác** | Chỉ xem xét nếu cả nhóm chính thức duyệt sửa frozen contract. Thuật toán mới phải có biểu diễn tự định ranh giới, bao phủ mọi tham số, định nghĩa chuẩn hóa và được cập nhật đồng bộ trong spec, fixture, serializer và kiểm thử liên ngôn ngữ. Chuỗi `\|` hiện tại không được giữ nguyên nếu chưa giải quyết escaping, trường thiếu/null và tương thích. |

**PROPOSAL — Khuyến nghị RD-A, trạng thái PENDING.** Không sửa spec hoặc serializer trong PR tài liệu này. Trước triển khai, A xác nhận algorithm/DTO, B xác nhận khóa ProcessedCommands và chính sách lưu/replay; sau đó thêm test vector độc lập do test dựng từ payload canonical theo tài liệu, không dùng cùng hàm helper để tạo expected value.

**Tương thích và chuyển đổi — PENDING:** DDL hiện không có ProcessedCommands và không có Central handler; chưa thấy bằng chứng digest production nào đã được lưu. Nếu đã có dữ liệu thử nghiệm hoặc một bản triển khai ngoài repository, không thể khôi phục payload gốc từ SHA-256 để đổi digest. Trước rollout cần kiểm kê rows/retries; không so sánh digest của hai thuật toán khác nhau. Nếu rows tồn tại, nhóm phải quyết định deployment boundary/algorithm marker hoặc chờ hết TTL 7 ngày theo hợp đồng trước khi chuyển, giữ nguyên response replay trong giai đoạn chuyển đổi. Không tự xóa hay diễn giải lại bản ghi đã lưu.

**Điều kiện trước ProcessedCommands:** chốt account identity (`UserId` vẫn còn sai kiểu giữa DTO và contract), chốt canonical RequestDigest và compatibility policy, tạo schema/repository, sau đó mới thử transaction dedup: cùng `(UserId, CommandId)` và digest trả lại response cũ; khác digest bị từ chối; SQL commit thất bại không trả success. Tất cả test này hiện là **TEST PLAN — NOT RUN**.

## 3. So sánh định danh

| Tiêu chí | Option I — UserId số nguyên | Option II — UserId chuỗi ổn định |
|---|---|---|
| Dữ kiện hiện có | Khớp LoginResultPayload.UserId, IncidentSummaryDto.AssignedToUserId và tham số digest int?; fixture login có UserId=1. | Khớp Incidents.AssigneeId NVARCHAR(64) và kiểu UserId NVARCHAR(64) trong mô tả ProcessedCommands. |
| Nguồn tài khoản | Chưa có Users/account repository; phải thiết kế Users với khóa số nguyên và cùng ID trong Central session. | Cũng chưa có account repository; phải chọn định dạng ổn định, ví dụ UUID string, không dùng tên hiển thị có thể đổi. |
| FK / tra cứu | FK int đơn giản tới Users.UserId; tránh biến tên người thành khóa. | FK NVARCHAR cần cùng collation/length; không trim/normalize tùy tiện; username chỉ dùng nếu bất biến và duy nhất. |
| DTO / digest | Giữ kiểu int hiện có ở response/summary/serializer. Cần thống nhất CommandParams parse số nguyên và ProcessedCommands.UserId INT. | Cần đổi UserId/AssignedToUserId DTO, digest method, fixtures và tài liệu idempotency sang string cùng quy tắc chuẩn hóa. |
| Tương thích | Tương thích với gợi ý wire hiện có, nhưng SQL và mô tả idempotency cần migration/chỉnh hợp đồng. | Tương thích một phần với DDL hiện tại, nhưng xung đột với DTO/fixture và thay đổi wire/API. |
| Rủi ro | Không được coi int là đã được nhóm duyệt chỉ vì DTO hiện dùng int; phải kiểm chứng login/account owner. | Chuỗi có nguy cơ chứa username/PII hoặc khác chuẩn giữa dịch vụ nếu không quy định identity bất biến. |
| Khi phù hợp | Khi tài khoản và LOGIN_RESULT tiếp tục dùng ID số nguyên xuyên hệ thống. | Khi nhóm đã có hoặc chính thức chọn khóa chuỗi bất biến làm danh tính chuẩn. |

### Khuyến nghị

**PROPOSAL — Ưu tiên Option I (UserId số nguyên), có điều kiện:** Đây là phương án ít sửa contract nhất vì LoginResultPayload, IncidentSummaryDto, serializer và fixture hiện đã dùng số nguyên. Nó cũng cho phép liên kết FK bằng khóa định danh ổn định thay vì tên người. Tuy nhiên, mã hiện tại chưa có account store/login handler và tài liệu ProcessedCommands còn ghi NVARCHAR(64). Quang và cả nhóm phải xác nhận account model trước khi chốt; nếu identity chính thức là chuỗi, Option II phải được chọn và toàn bộ DTO, digest, fixture, SQL được đồng bộ trong RFC sửa đổi.

Không đưa actor UserId vào payload để client tự khai. Central lấy UserId từ phiên đăng nhập. Với IncidentCommandPayload.CommandParams, assignedToUserId chỉ là target; phải parse đúng số nguyên, kiểm tra tồn tại và quyền trước mutation. Digest canonical hóa cùng giá trị target như hiện có.

## 4. Mô hình IncidentHistory đề xuất

**PROPOSAL — Bảng tối thiểu cho audit nghiệp vụ, không thay ProcessedCommands:**

| Trường logic | Ý nghĩa |
|---|---|
| IncidentHistoryId | Khóa bản ghi lịch sử; kiểu cụ thể để B duyệt cùng schema. |
| IncidentId | Phiếu sự cố bị thay đổi. FK tới Incidents dự kiến. |
| EventType | INCIDENT_ACK, INCIDENT_ASSIGN, INCIDENT_START hoặc INCIDENT_RESOLVE. |
| PreviousWorkflowState / NewWorkflowState | Trạng thái trước và sau; null nếu event chỉ thay người được giao. |
| PreviousAssigneeUserId / NewAssigneeUserId | Định danh trước/sau; null biểu diễn chưa được giao. |
| ActorUserId | Người thực hiện lấy từ session Central; FK tới Users khi account model được duyệt. |
| OccurredAtUtc | Giờ UTC phía server, không dùng timestamp client làm thời điểm kiểm toán. |
| Note | Ghi chú giải quyết nếu lệnh hiện hành cho phép; nullable. |
| CommandId | Mã tương quan/idempotency của thao tác; giữ riêng bảng dedup và không dùng lịch sử thay thế nó. |

Đề xuất FK từ history tới Incidents và Users, không cascade-delete history khi xóa phiếu/tài khoản. Tài khoản có liên quan nên chuyển inactive/soft-delete để giữ dấu vết. Schema hiện tại không có FK; các quan hệ trên là đề xuất cần nhóm và B duyệt, không phải hiện trạng.

**Đơn vị giao dịch đề xuất:** kiểm tra quyền và target; bắt đầu transaction; kiểm tra khóa lệnh/digest theo semantics đã duyệt; UPDATE Incidents WHERE IncidentId và RowVersion=expected; nếu affected-row count bằng 0 rollback và trả 409 kèm dữ liệu mới nhất; nếu có đúng một hàng, thêm một IncidentHistory; lưu response command idempotency; commit; trả COMMAND_RESULT. Nếu commit thất bại, không trả success. Duplicate cùng digest trả lại kết quả đã ghi mà không mutation/lịch sử lần hai. ID trùng với digest khác bị từ chối.

Mã lỗi cho target không tồn tại cần chọn từ error catalog trước khi đóng băng; không phát minh error code đang có. Unauthorized INCIDENT_ASSIGN phải trả theo mã 403 hiện hành và không sửa Incidents/history.

## 5. Hợp đồng, schema, migration và tương thích

- **PROPOSAL:** Nếu chọn số nguyên, đổi candidate SQL assignee sang AssigneeUserId INT NULL, liên kết tới Users.UserId, thống nhất kiểu ProcessedCommands.UserId thành INT, và giữ UserId+CommandId làm khóa dedup. AssignedToUserId có thể vẫn là tên JSON hiện tại; đổi tên chỉ khi nhóm muốn một tên thống nhất và đã duyệt versioning.
- Users cần được thiết kế cùng auth B-004: UserId, định danh đăng nhập duy nhất, thông tin xác thực đã băm theo đặc tả, Role và trạng thái tài khoản. RFC này không chốt các cột auth hoặc seed credential.
- Thêm IncidentHistory tối thiểu như trên. Mỗi mutation thành công tạo một event; request bị từ chối, thiếu quyền, target không tồn tại hoặc OCC conflict không tạo event thành công.
- Gửi target user dưới dạng integer JSON trong commandParams nếu Option I được duyệt. Server xác minh JSON token là số nguyên trong miền int; không chấp nhận string "5", float hoặc giá trị ngoài miền.
- Giữ RowVersion SQL Server là token opaque 8-byte; Admin gửi expectedRowVersion từ bản đã tải. Phải kiểm tra row count và refresh dữ liệu khi 409; cột RowVersion tự tăng không thay thế logic điều kiện update.
- Giữ idempotency (authenticated UserId, CommandId) + digest. Không chuyển khóa thành IncidentHistory hoặc chỉ dựa vào unique CommandId toàn cục.
- DDL hiện chỉ là CREATE TABLE cho database mới. Nếu schema chưa được dùng, tạo DDL phiên bản đã duyệt khi B-005/B-007/B-009 sẵn sàng. Nếu database đã chứa dữ liệu, backup và migration riêng: lập bảng Users, ánh xạ AssigneeId cũ chỉ khi có nguồn xác thực đáng tin cậy, không tự chuyển tên tùy ý; giữ giá trị chưa ánh xạ để rà soát, không mất lịch sử.
- Backward compatibility: client cũ không có trường assignment chỉ có thể thực hiện các lệnh không cần target. Client mới chỉ bật Assign khi server công bố khả năng đã hỗ trợ và người dùng có quyền. Nếu đổi kiểu/field wire, cần version/capability hoặc coordinated deployment; không khẳng định tương thích chỉ vì JSON.

## 6. An toàn, độ tin cậy và kiểm thử

Central là nguồn quyết định identity/role. Bắt buộc kiểm tra session hợp lệ, token hết hạn, vai trò Admin và target user active. Không tin ActorUserId, Role hoặc tên hiển thị do client gửi. Giới hạn độ dài note và log không ghi mật khẩu/token. FK bảo vệ target tồn tại; transaction bảo vệ cập nhật và lịch sử cùng nhau.

Ca kiểm thử dự kiến:

- Login trả identity theo loại đã chọn; session Central map đúng user/role.
- Gán cho user hợp lệ ghi assignee và đúng một event history trong cùng transaction.
- Target null hợp lệ theo chính sách bỏ gán; target không tồn tại/inactive bị từ chối, không đổi row hay ghi event.
- Operator/Viewer gọi INCIDENT_ASSIGN bị từ chối; dữ liệu không đổi.
- Hai Admin cùng gửi expectedRowVersion V1: một cập nhật thành công, một nhận 409, chỉ một event thành công.
- Retry cùng UserId+CommandId+digest trả lại response đã lưu và không ghi thêm event; cùng key với digest khác bị từ chối.
- SQL rollback giữa UPDATE và insert history không để mutation dở dang hoặc COMMAND_RESULT thành công.
- Actor lấy từ phiên đã xác thực; giả mạo actor trong payload không ảnh hưởng audit.
- Response/event refresh cho Admin hiển thị target assignee mới và RowVersion mới; stale view được tải lại.

Tất cả là **TEST PLAN — NOT RUN**. Tests hiện có trong Protocol chỉ bao phủ framing/fixtures/digest; không có Central SQL assignment tests tại baseline.

## 7. Tệp và task bị ảnh hưởng nếu được duyệt

| Tệp / Task ID | Nội dung dự kiến | Chủ trì / review |
|---|---|---|
| docs/spec/PROTOCOL_SPEC.md, docs/spec/CONTRACT_MATRIX.md, docs/spec/ARCHITECTURE_DECISIONS.md; src/LabGuard.Protocol/Dtos/AdminDtos.cs, src/LabGuard.Protocol/Serialization/ProtocolSerializer.cs, fixtures/tests | Chốt kiểu identity, assignment command, capability/version và idempotency | A — Phúc; review B/C/D |
| database/schema.sql và migration/repository, Central auth/session/incident handler (chưa có tại baseline), Central tests | Users/account source, assignee FK, IncidentHistory, OCC+transaction+dedup | B — Quang; review A/C |
| Admin client, assignment selector, IncidentSummary/IncidentUpdate response binding | Chỉ bật thao tác theo quyền; reload khi conflict; hiển thị người được giao | C — Vy; review B |
| LG-D-007, LG-D-008, LG-D-009, LG-D-010, LG-B-010, LG-C-012 | Hiển thị bằng chứng và đề xuất bước kiểm tra từ dữ liệu Central; kiểm tra nguồn/độ mới/quyền | D — Ngọc phối hợp B/C |
| WBS LG-B-004, LG-B-005, LG-B-007, LG-B-009, LG-C-002, LG-C-008, TEST-B09, TEST-C08 | Rà phụ thuộc, effort, nghiệm thu sau khi có quyết định | Chủ nhiệm vụ; chưa sửa ở RFC |

## 8. Trách nhiệm và effort sơ bộ

| Người | Phần việc dự kiến | Ước lượng tăng thêm |
|---|---|---:|
| Võ Trọng Phúc (A) | Review contract/freeze, kiểu target, digest và fixture sau khi được duyệt | 1–2 giờ |
| Nguyễn Bá Quang (B) | Account identity alignment, repository/transaction, assignment/history, FK/migration, dedup/OCC tests | 8–12 giờ |
| Lý Tuyết Vy (C) | Tải user hợp lệ, thao tác Assign theo quyền, phản hồi success/conflict và làm mới giao diện | 3–5 giờ |
| Đỗ Minh Ngọc (D) | Kiểm thử luồng bằng chứng/sự cố tích hợp, kiểm tra nội dung history và giới hạn gợi ý | 2–3 giờ |
| Review / ghép tích hợp | Kiểm tra chéo, chạy kịch bản Central/SQL/Admin sau khi core sẵn sàng | 2–4 giờ |
| **Tổng sơ bộ** | Phần bổ sung sau khi B-004 authentication, B-005 persistence, B-007 lifecycle và C-002 kết nối có nền tảng | **16–26 giờ** |

Các số này là **PROVISIONAL**, không phải cam kết lịch và không thay giờ hiện tại của WBS B-009/C-008. Nếu phải xây account/auth core từ đầu hoặc migration dữ liệu đã triển khai, B cần ước lượng riêng trước khi lập kế hoạch.

## 9. Kế hoạch phụ thuộc và rollback

1. Nhóm xác nhận identity numeric/string; Quang xác nhận account source, seed/admin và dữ liệu database thực.
2. Hoàn thành hoặc xác minh LG-B-004 (auth/RBAC), LG-B-005 (repository/transaction) và LG-B-007 (incident lifecycle/OCC); không ước lượng LG-B-009 như một thay đổi độc lập.
3. A/B chốt contract + schema; migration review và fixture compatibility; cập nhật WBS/Excel sau effort review.
4. B triển khai transaction, FK/target validation, dedup và history; kiểm thử SQL cô lập.
5. C tích hợp selector/response/reload; D kiểm tra quy trình bằng chứng qua Central; phối hợp kiểm thử quyền, đồng thời và lỗi database.
6. Chỉ bật Assign khi account source, Central handler, SQL transaction, response refresh, role checks và regression tests cùng sẵn sàng.

Rollback trước phát hành: tắt khả năng Assign bằng capability/UI, giữ các luồng incident khác. Sau khi history đã được ghi, không xóa dữ liệu kiểm toán để rollback UI; dùng migration có backup, giữ ID cũ và forward-fix khi không thể đảo an toàn. Không xóa cột/bảng đã có dữ liệu bằng script rollback tự động.

## 10. Quyết định còn mở

| Câu hỏi | Trạng thái |
|---|---|
| Account identity chuẩn là INT hay chuỗi bất biến? Ai xác nhận nguồn LoginResult/session thực tế? | PENDING |
| Users/account store thuộc phạm vi B-004 nào; bootstrap Admin và inactive user được xử lý ra sao? | PENDING |
| Chuyển AssigneeId NVARCHAR(64) sang AssigneeUserId INT hay giữ chuỗi đồng bộ toàn DTO? | PENDING |
| ProcessedCommands có được tạo với UserId INT hay nhóm chọn string identity? | PENDING |
| FK tới Users là bắt buộc? Chính sách giữ user được giao khi nghỉ/khóa tài khoản? | PENDING |
| IncidentHistory tối thiểu có các trường nào; có lưu resolution note và CommandId không? | PENDING |
| Assignment response là COMMAND_RESULT rồi refresh/query hay bổ sung IncidentUpdate fields? | PENDING |
| Quy tắc target null/inactive và mã lỗi target không tồn tại? | PENDING |
| B/A/C/D xác nhận effort, migration và WBS/Excel amendment? | PENDING |

## 11. Approval gate

**Approval status: PENDING.** Chưa thấy bằng chứng phê duyệt độc lập cho kiểu identity, Users/FK, schema, history hoặc migration. Quyết định phân công chính thức xác định người phụ trách nhưng không thay thế quyết định kỹ thuật frozen contract. Chỉ bắt đầu sửa code/schema sau khi B xác nhận account/SQL model, A duyệt tác động protocol, C duyệt workflow/response, D rà luồng bằng chứng; cả nhóm ghi nhận lựa chọn và WBS được cập nhật theo effort đã xác nhận.

**Build/test/SQL/Windows/LAN trong RFC này:** NOT RUN — chỉ có tài liệu RFC, không có triển khai.
