namespace LabGuard.Admin;

public class DashboardForm : Form
{
    public DashboardForm()
    {
        Text = "LabGuard Admin — Đang chờ Sinh viên C thực hiện";
        Size = new Size(640, 400);
        StartPosition = FormStartPosition.CenterScreen;

        var lbl = new Label
        {
            Text = "Giao diện quản trị LabGuard Admin\n\nChưa hiện thực mã nguồn — Đang chờ Sinh viên C thực hiện.\nNhiệm vụ bắt đầu: C-001 (Thiết kế Dashboard) & C-002 (AdminClient Socket).",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 11F, FontStyle.Regular)
        };

        Controls.Add(lbl);
    }
}
