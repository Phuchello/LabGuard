# Bằng chứng kiểm tra — 18/10/2026

Hồ sơ kiểm tra của PR #11, đã merge vào main `70481fa`. Giữ các mốc nguồn và kết quả thực chạy trước merge dưới đây; xem [PROJECT_STATE.md](../../PROJECT_STATE.md) cho housekeeping/phân công hiện hành.

**Kết luận:** launcher/configuration/local collection/TCP probe đã chạy trên Windows phát triển. Chưa có monitoring toàn tuyến. TCP connect/HELLO không chứng minh AUTH, telemetry đã lưu SQL hoặc application ACK.

## Phạm vi và nguồn

Nhánh `review/agent-deployment-readiness-20261018` bắt đầu từ main `260759b23d960994f75c7b15455915b68d0c5f8d`, đã fetch lại trước khi tạo worktree riêng. Checkout gốc và thay đổi chưa commit của các phần việc khác được giữ nguyên trong lượt đó. PR #11 sau đó được merge thành `70481fa`; trạng thái task chưa hoàn tất không đổi theo việc merge launcher.

Tám file Agent/test: Program.cs, AgentSettings.cs, AgentConsoleApplication.cs, AgentLog.cs, agentsettings.example.json, LabGuard.Agent.csproj; AgentStartupTests.cs và LabGuard.Agent.Tests.csproj. Có 30 test cases mới; test reference Central chỉ để kiểm tra listener thật. Không đổi AgentSocketClient, collector/services, Central, schema, protocol, WBS hoặc báo cáo/PPTX GK.

## Kết quả trước khi chuyển sang nhánh review

Windows x64, SDK 9.0.318, runtime publish 9.0.20. SDK 10 chưa cài.

| Lượt thực chạy | Kết quả |
|---|---|
| Baseline trước sửa | 165 passed: Protocol 107, Agent 34, Central 9, MockHarness 15 |
| Agent sau sửa | 64 passed, 0 failed, 0 skipped |
| Build solution | Exit 0, 0 warnings, 0 errors; gồm cả Admin WinForms |
| Test solution sau sửa | 195 passed, 0 failed, 0 skipped: Protocol 107, Agent 64, Central 9, MockHarness 15 |
| Publish Release win-x64 self-contained | Exit 0; không trim, bundled runtime 9.0.20 |

Đã chạy executable published trực tiếp với Central Program/CentralServer thật trên loopback, không FakeCentral. Hai config/DeviceId riêng được Central ghi nhận HELLO rồi đóng kết nối; không có metric/heartbeat/token. Đã thử valid config (exit 0), init lại không ghi đè (exit 2), mẫu chưa điền (exit 2), local CPU/RAM/uptime (exit 0), normal monitoring bị chặn (exit 5), Central vắng (ConnectionRefused, exit 4).

Ctrl+C thực tạo `Cancelled`, Central ghi disconnect hợp lệ. PTY/PowerShell wrapper trả 1, không suy native exit 130 từ mã wrapper; unit test cancellation của launcher trả 130 và chờ Central phát ClientDisconnected. Không bảo đảm drain/delivery frame đang chờ.

Peer đóng im lặng trong probe đã tái hiện giới hạn: writerReportsConnected vẫn true, command kết thúc exit 0 vì chưa có reader/liveness. Đây là thiếu readiness phát hiện mất mạng, không ghi PASS cho reconnect. Existing writer-failure unit test vẫn pass khi ghi tiếp kích hoạt lỗi.

## Kiểm tra lại trong worktree review

Đã chạy `verify_agent.ps1 -Publish` trên đúng nhánh review trước commit, với kết quả cuối:

- Script exit 0; build solution exit 0, 0 warnings, 0 errors.
- 195 passed, 0 failed, 0 skipped: Protocol 107, Agent 64, Central 9, MockHarness 15; đủ bốn TRX riêng.
- Publish Release win-x64 self-contained exit 0.
- Executable smoke: init 0, validate 0, collect-local 0, monitoring-blocked 5 đúng dự kiến. Script chuẩn hóa expected exit 5 thành kết quả kiểm tra thành công; không đổi hành vi Agent.
- Bản chạy cuối được giữ local dưới `scratch/agent-readiness/5d50ee56d7d441c3975e2e4daf745498/`; không commit logs/TRX/runtime/config có identity của lượt này.

Không chạy lại probe thao tác tay/Ctrl+C hoặc thử LAN trong lượt push này; bằng chứng thao tác tay ở phần trước là của lượt phát triển gốc, không gán lại cho build mới. Các integration tests với actual CentralServer vẫn được chạy lại trong 195 tests trên nhánh review.

## Tái tạo và giữ bằng chứng

`verify_agent.ps1` thực chạy build/restore solution rồi test với `--no-build --no-restore --logger trx`. `-Publish` thêm self-contained publish và bốn smoke checks (init, validate, collect-local, monitoring-blocked). Log và bốn TRX được lưu ở thư mục GUID dưới `scratch/agent-readiness/`, không đưa vào commit.

Để thử probe thủ công, chạy Central hiện có ở một Console riêng rồi dùng Agent với cấu hình endpoint được phép. Tránh chiếm port do process khác sở hữu; không tự dừng ứng dụng người dùng. Dùng Ctrl+C dừng process thử của mình. Loopback không thay kiểm tra hai host LAN.

Bằng chứng cũ được giữ local, không xóa: console logs baseline/build/test/publish, bốn TRX cuối theo assembly, terminal Agent/Central Ctrl+C, ca peer đóng, source diff/inventory, checksum sáu file GK cùng ZIP. Baseline có collision tên TRX nên số 165 dựa console log đủ bốn bộ; lượt test cuối có đủ bốn TRX riêng. Source snapshot và patch trùng mã không được commit vì Git diff cung cấp nguồn review trực tiếp. Không đẩy raw log, runtime binaries, ZIP, video hoặc ảnh QA.

## Việc còn thiếu / cần quyết định

- Quang: TLS/session/authorization, identity binding, transaction SQL thật/dedup và BATCH_ACK sau commit. Agent nhận HELLO_ACK/AUTH_RESULT trước khi bật monitoring.
- Phúc: session reader/liveness, reconnect, ACK/replay coordination; sau đó mới ghép MetricsService/HeartbeatService với config thực. Probe không có bypass monitoring chưa AUTH.
- Vy kiểm tra góc nhìn Admin khi Central pipeline có thật; Ngọc kiểm tra lỗi mạng và chức năng hỗ trợ phân tích và xử lý sự cố tích hợp trên dữ liệu Central có thẩm quyền. Không thêm tool độc lập hoặc bắt buộc Gateway/Collector.
- Hai máy LAN, nhiều PC thật, máy sạch không runtime, PC phòng máy được cấp phép: NOT RUN. Windows Service: NOT INSTALLED/NOT TESTED.
- .NET 10: NOT MIGRATED/NOT TESTED. Đánh giá toàn 13 csproj (12 trong solution, test_frame_codec ở ngoài), NuGet/WinForms/Designer và RID; không suy compatibility từ build net9. Xem `DEPENDENCY_AND_TEST_PLAN.md`.

.NET 9 hết hỗ trợ 10/11/2026; .NET 10 LTS đến 14/11/2028 theo [Microsoft policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core). Bản net9 chỉ dành cho kiểm tra hiện tại, chưa nghiệm thu deployment dài hạn. Tham khảo [publishing](https://learn.microsoft.com/en-us/dotnet/core/deploying/) và [Windows Service](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service).
