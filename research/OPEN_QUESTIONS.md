# LABGUARD — BẢNG CÂU HỎI MỞ & ĐIỂM CHỜ XÁC MINH (OPEN QUESTIONS)
**Dự án:** LABGUARD — Hệ thống giám sát và quản lý sự cố phòng máy  
**Môn học:** NT106 — Lập trình mạng căn bản (UIT)  
**Trạng thái:** Đã cập nhật sau vòng Audit của ChatGPT Work — Checkpoint 1  

---

## 1. CÂU HỎI MỞ VỀ MẶT HỌC THUẬT & YÊU CẦU GIẢNG VIÊN (NT106 COURSE QUESTIONS)

1. **Barem / Rubric chấm điểm chính thức của Giảng viên hướng dẫn môn NT106**:
   - *Hiện trạng:* Nhóm hiện chưa có phiếu giao đề tài chính thức với barem điểm chi tiết cho đồ án NT106 học kỳ này. Các tiêu chí hiện tại được đối chiếu từ đề cương môn học chung và báo cáo tham khảo của các khóa trước.
   - *Rủi ro:* Trọng số điểm giữa độ sâu lập trình mạng (Socket, Concurrency, Fault-tolerance, Framing, Security) và số lượng chức năng trên giao diện WinForms chưa được lượng hóa chính xác.
   - *Hành động của nhóm:* Thành viên C dùng 1 giờ dự phòng trong Tuần 1 để gặp trực tiếp hoặc trao đổi với Giảng viên hướng dẫn nhằm xin barem/checklist chấm điểm chính thức.

2. **Xác nhận hình thức demo qua mạng Internet**:
   - *Hiện trạng:* Master Plan đưa việc kiểm tra kết nối Internet vào phạm vi P0 (có phương án dự phòng fallback). Nhiều đường truyền Internet cá nhân hoặc 4G không có địa chỉ IPv4 công cộng tĩnh (nằm sau CGNAT hoặc router không mở port inbound).
   - *Câu hỏi cần Giảng viên xác nhận:*
     - "Nếu nhóm demo trên 2–3 laptop vật lý kết nối qua mạng LAN cục bộ của phòng lab hoặc qua WiFi hotspot, kèm video/bằng chứng phân tích định tuyến qua Internet, thì đã đáp ứng đầy đủ yêu cầu của môn học chưa?"
     - "Trong trường hợp demo qua Internet từ xa, Giảng viên có chấp nhận việc kết nối qua hạ tầng VPN Overlay (như WireGuard/Tailscale) hoặc VPS có Public IP trung gian hay không?"
   - *Lưu ý nguyên tắc:* Nhóm không tự khẳng định đa số mạng sinh viên dùng CGNAT nếu chưa khảo sát thực tế; nhóm không tự ý gửi tin nhắn tự động cho Giảng viên mà sinh viên trực tiếp trao đổi trong giờ học/hướng dẫn.

---

## 2. CÁC ĐIỂM KỸ THUẬT ĐÃ ĐƯỢC LÀM RÕ & ĐÓNG LẠI (CLOSED QUESTIONS)

3. **Vấn đề SQLite vs SQL Server 2022 Express**:
   - *Kết luận (ADR-005):* ĐÓNG LẠI. Giữ nguyên SQL Server 2022 Express làm baseline duy nhất theo Master Plan. Bỏ ý tưởng "chuyển nóng khẩn cấp sang SQLite khi đi thi". Interface `IRepository` chỉ dùng để tổ chức code sạch và viết unit test.
4. **Quy tắc tháo gỡ phụ thuộc khi hoãn P1**:
   - *Kết luận (ADR-006):* ĐÓNG LẠI. Nếu cuối Tuần 3 chưa đạt M4, dứt khoát DEFER P1; các task P0 ở Tuần 5 (A016, B016, C016) tự động chuyển sang phụ thuộc các task MVP tương ứng (A012, B012, C011).
5. **Đồng bộ ngưỡng CPU và Offline Detection**:
   - *Kết luận (ADR-003 & RL-009):* ĐÓNG LẠI. Mở `HIGH_CPU` khi $\geq 90\%$ (3 mẫu liên tiếp); phục hồi khi $\leq 75\%$ (3 mẫu liên tiếp). Offline khi quá 20 giây tính theo đồng hồ đơn điệu server kể từ heartbeat hợp lệ gần nhất. Bỏ hoàn toàn khẳng định "phục hồi dưới 20s".
