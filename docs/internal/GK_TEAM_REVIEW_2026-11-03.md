# LabGuard — Review & quyết định trước khi đồng bộ GK

Ngày: 03/11/2026. **Đây là checklist đề xuất; không phải quyết định/biên bản đã có chữ ký.**

| Mã | Chủ trì | Việc xác nhận | Điều kiện chấp nhận |
|---|---|---|---|
| GK-01 | A/B/D | Khôi phục baseline local `f5144ed`, 5 file evaluator, 15 TRX, source hashes | Nêu nguồn rõ; không reset/clean, không gán local lên main |
| GK-02 | B/điều phối | DB-01 engine/catalog/permission | Quyết định thực trước DB migration/test |
| GK-03 | A+B+C | C3 identity/authenticated Admin read | Contract/role/DTO được duyệt |
| GK-04 | A+B | C4 telemetry/dedup/collation/time | Canonical cases + SQL tests |
| GK-05 | B+C+D | C1/C2 incident mutations | State machine/permission/audit được duyệt |
| GK-06 | D+A+lab owner | E0 Runner code plan và lab P1/P2/S/DNS | File allowlist, no live probes trước quyền riêng |
| GK-07 | C+D | UX evidence/UNKNOWN/recovery; demo GK mô phỏng | Có nhãn mock hoặc live dựa evidence |
| GK-08 | Cả nhóm | Duyệt báo cáo/slide/nội dung 50/20/20/10 | Không bịa số đo hoặc người đã đồng ý |

**WBS trên GitHub main** giữ 40 task: 6 XONG / 2 ĐANG LÀM / 32 CHƯA LÀM. Workbook phụ lục có sheet evidence tách riêng, không đồng nhất với tracker chính thức.

**Căn cứ:** [STATUS](../gk/STATUS_2026-11-03.md), [TEAM_ASSIGNMENTS](TEAM_ASSIGNMENTS.md), [WBS](WBS_TASKS.csv), [Checkpoint 4.3] (ở bộ bằng chứng ngoài Git), TRX `evaluator-qa.trx`.
