# Quản trị nội bộ LabGuard

- [TEAM_ASSIGNMENTS.md](TEAM_ASSIGNMENTS.md): quyết định trực tiếp của người điều phối ngày 17/10/2026, tên/MSSV và trách nhiệm chính thức.
- [WBS_TASKS.csv](WBS_TASKS.csv): 40 task, giữ nguyên 34 task cũ và bổ sung sáu task local về chức năng hỗ trợ xử lý sự cố. Status là tiến độ kế hoạch, không phải chứng nhận toàn tuyến.
- [OWNERSHIP_MATRIX.md](OWNERSHIP_MATRIX.md): ranh giới sửa code; Ngọc logic của chức năng hỗ trợ xử lý sự cố tích hợp, Vy giao diện Admin, Quang nguồn Central.
- [DEPENDENCY_MAP.md](DEPENDENCY_MAP.md): điều kiện tích hợp và các giai đoạn tương đối toàn đồ án.
- [Tracker Excel](../../LabGuard_Task_Tracker.xlsx): góc nhìn theo thành viên, đồng bộ sáu task mới; lịch sử trạng thái được giữ.

Chênh lệch local/GitHub và kết quả kiểm tra housekeeping được ghi tại [PROJECT_STATE.md](../../PROJECT_STATE.md). Báo cáo/slide GK đã duyệt không thuộc phạm vi cập nhật. Gateway/Collector/Simulator là tùy chọn sau khi phần cốt lõi ổn định; không thay nhiệm vụ chính Công cụ kiểm thử truyền thông TCP và chức năng hỗ trợ phân tích, xử lý sự cố tích hợp trong LabGuard.

LG-B-009 là bản ghi kế hoạch cũ còn mâu thuẫn với schema hiện hành (`AssignedToUserId`/`IncidentHistory` so với `AssigneeId`). Giữ nguyên để review, không coi là quyền sửa frozen schema; Quang/nhóm phải quyết định cách xử lý trước khi làm task này.
