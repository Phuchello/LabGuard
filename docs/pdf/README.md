# BỘ TÀI LIỆU PDF CHUYÊN BIỆT — DỰ ÁN LABGUARD NT106

> **Đồ án môn học NT106: Lập trình mạng căn bản — Khoa Mạng máy tính & Truyền thông — Trường ĐH Công nghệ Thông tin (UIT)**  
> **BIÊN SOẠN: VÕ TRỌNG PHÚC**  
> **Nhóm thực hiện:** 4 thành viên (Thành viên A, B, C, D)

---

## 1. DANH SÁCH 5 TẬP TÀI LIỆU PDF CHÍNH THỨC

Thư mục `docs/pdf/` chứa đúng **5 cuốn sách PDF** được biên dịch chuẩn hóa bằng **XeLaTeX** (hỗ trợ font tiếng Việt Unicode Times New Roman, layout chuẩn A4, mục lục siêu liên kết và định dạng đồ họa chất lượng cao):

| STT | Tên tệp PDF | Mục tiêu & Đối tượng phục vụ | Phân hệ làm chủ | Trạng thái kỹ thuật |
| :---: | :--- | :--- | :---: | :---: |
| **01** | `LabGuard_Tong_Quan_A-Z.pdf` | **Sổ tay toàn diện đồ án (Master Book)** dành cho cả 4 thành viên: Sổ tay tổng quan A-Z về kiến trúc, giao thức, phân công, Git, kiểm thử và vấn đáp. | Toàn hệ thống | **Chính thức** |
| **02** | `LabGuard_Huong_Dan_Thanh_Vien_A.pdf` | **Sổ tay Thành viên A (Agent & Protocol):** Giải thích chi tiết Wire Contract, FrameCodec, hướng dẫn viết Socket Client, Replay Buffer, đo CPU/RAM và 20 câu hỏi vấn đáp. | `src/LabGuard.Agent/`<br>`src/LabGuard.Protocol/` | **A-001/A-002/A-003: XONG**<br>A-004+: Chưa làm |
| **03** | `LabGuard_Huong_Dan_Thanh_Vien_B.pdf` | **Sổ tay Thành viên B (Central Server & Database):** Hướng dẫn lập trình TCP Server đa luồng, thiết kế SQL Server, động cơ luật Rule Engine, Dead Man's Switch, OCC và 20 câu hỏi vấn đáp. | `src/LabGuard.Central/`<br>`database/` | **B-001+: Dự kiến** |
| **04** | `LabGuard_Huong_Dan_Thanh_Vien_C.pdf` | **Sổ tay Thành viên C (Admin Client & UI):** Hướng dẫn thiết kế WinForms Dashboard, xử lý an toàn luồng Cross-Thread UI (`BeginInvoke`), tương tác sự cố OCC, RBAC và 20 câu hỏi vấn đáp. | `src/LabGuard.Admin/` | **C-001+: Dự kiến** |
| **05** | `LabGuard_Huong_Dan_Thanh_Vien_D.pdf` | **Sổ tay Thành viên D (Gateway & Collector):** Hướng dẫn thiết kế Gateway và Collector (Scale Layer), hàng đợi chống tràn bộ nhớ (Bounded Channel), cùng công cụ kiểm thử hỗ trợ Simulator và Wireshark; 20 câu hỏi vấn đáp. | Gateway & Collector Layer<br>`src/LabGuard.Simulator/` *(hỗ trợ)* | **D-001+: Dự kiến** |

---

## 2. CẤU TRÚC THƯ MỤC NGUỒN (SOURCE CODE)

Toàn bộ mã nguồn LaTeX có thể chỉnh sửa nằm trong thư mục `docs/pdf/source/`:

```
docs/pdf/
├── build.ps1                                   # Script PowerShell tự động biên dịch toàn bộ 5 PDF
├── README.md                                   # Tài liệu hướng dẫn sử dụng thư mục PDF
├── LabGuard_Tong_Quan_A-Z.pdf                  # Thành phẩm Tập 1 (Master Book)
├── LabGuard_Huong_Dan_Thanh_Vien_A.pdf         # Thành phẩm Tập 2 (Thành viên A)
├── LabGuard_Huong_Dan_Thanh_Vien_B.pdf         # Thành phẩm Tập 3 (Thành viên B)
├── LabGuard_Huong_Dan_Thanh_Vien_C.pdf         # Thành phẩm Tập 4 (Thành viên C)
├── LabGuard_Huong_Dan_Thanh_Vien_D.pdf         # Thành phẩm Tập 5 (Thành viên D)
└── source/
    ├── labguard_pdf.sty                        # Gói định dạng phong cách chuyên nghiệp dùng chung
    ├── 01_LabGuard_Tong_Quan_A-Z.tex           # Mã nguồn Tập 1
    ├── 02_LabGuard_Huong_Dan_Thanh_Vien_A.tex  # Mã nguồn Tập 2
    ├── 03_LabGuard_Huong_Dan_Thanh_Vien_B.tex  # Mã nguồn Tập 3
    ├── 04_LabGuard_Huong_Dan_Thanh_Vien_C.tex  # Mã nguồn Tập 4
    └── 05_LabGuard_Huong_Dan_Thanh_Vien_D.tex  # Mã nguồn Tập 5
```

---

## 3. HƯỚNG DẪN BIÊN DỊCH LẠI PDF (HOW TO BUILD)

### Yêu cầu môi trường
* Hệ điều hành: Windows 10/11
* Phần mềm LaTeX: **MiKTeX** (phiên bản 25.x trở lên) hoặc **TeXLive**.
* Trình biên dịch: `xelatex` (được gọi tự động trong script).
* Python 3.x (dùng thư viện `pymupdf` để kiểm đếm số trang thành phẩm).

### Lệnh thực thi tự động qua PowerShell
Mở PowerShell tại thư mục gốc của repository hoặc tại thư mục `docs/pdf/` và chạy:

```powershell
# Chạy script tự động biên dịch toàn bộ 5 tập PDF:
./docs/pdf/build.ps1
```

Script sẽ tự động:
1. Duyệt qua từng tệp `.tex` trong `source/`.
2. Chạy `xelatex` **2 lượt liên tiếp** để đồng bộ chỉ số trang và siêu liên kết.
3. Di chuyển các tệp PDF thành phẩm ra thư mục `docs/pdf/`.
4. Dọn dẹp sạch sẽ các tệp rác trung gian (`.aux`, `.log`, `.out`, `.toc`, `.lof`, `.lot`).
5. In bảng tổng kết dung lượng và số trang của từng tài liệu.

Để chỉ dọn dẹp các tệp tạm mà không biên dịch lại:
```powershell
./docs/pdf/build.ps1 -CleanOnly
```

---

## 4. HƯỚNG DẪN SỬ DỤNG CHO SINH VIÊN

1. **Tuần đầu tiên:** Cả nhóm 4 bạn cùng đọc kỹ cuốn **`LabGuard_Tong_Quan_A-Z.pdf`** để hiểu bức tranh toàn cảnh, nắm rõ luồng dữ liệu TCP, cơ chế dính gói/vỡ gói và ma trận phân công RACI.
2. **Khi bắt tay vào lập trình:** Mỗi bạn mở cuốn cẩm nang riêng của mình (A, B, C, hoặc D) để đọc khung kiến trúc, các giải thuật mẫu, và tiêu chí nghiệm thu (Checklist) của từng tác vụ.
3. **Trước ngày bảo vệ cuối kỳ:** Đọc kỹ danh mục các câu hỏi vấn đáp chuyên sâu ở cuối mỗi cuốn sách để tự tin trả lời lưu loát trước hội đồng chấm thi của Trường ĐH Công nghệ Thông tin (UIT).
