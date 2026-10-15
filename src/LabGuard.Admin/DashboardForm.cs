using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LabGuard.Admin
{
    public partial class DashboardForm : Form
    {
        public DashboardForm()
        {
            InitializeComponent();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void label1_Click_2(object sender, EventArgs e)
        {

        }

        private void dgvPhongMay_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // 1. Kiểm tra chỉ số dòng hợp lệ
            if (e.RowIndex < 0 || e.RowIndex >= dgvPhongMay.Rows.Count) return;

            // 2. Lấy giá trị trạng thái từ cột "CotTrangThai" (Name bạn đã đặt ở bước trước)
            string status = dgvPhongMay.Rows[e.RowIndex].Cells["CotTrangThai"].Value?.ToString() ?? "OFFLINE";

            var row = dgvPhongMay.Rows[e.RowIndex];

            // 3. Tô màu tương ứng và đặt màu chữ tương phản rõ nét
            if (status == "ONLINE")
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(54, 73, 46);
                row.DefaultCellStyle.ForeColor = Color.White;
            }
            else if (status == "WARNING")
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(218, 166, 62);
                row.DefaultCellStyle.ForeColor = Color.Black;
            }
            else
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(79, 0, 5);
                row.DefaultCellStyle.ForeColor = Color.White;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void dgvPhongMay_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void lblServerIP_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void DashboardFromNew_Load(object sender, EventArgs e)
        {
            // Dữ liệu mẫu (Demo/Mock) minh họa hiển thị giao diện C-001 khi chưa kết nối Central
            if (dgvPhongMay.Rows.Count == 0)
            {
                dgvPhongMay.Rows.Add("PC-DEMO-01", "15.2%", "4.2 / 16.0 GB", "ONLINE");
                dgvPhongMay.Rows.Add("PC-DEMO-02", "88.6%", "14.8 / 16.0 GB", "WARNING");
                dgvPhongMay.Rows.Add("PC-DEMO-03", "0.0%", "0.0 / 16.0 GB", "OFFLINE");
                dgvPhongMay.Rows.Add("PC-DEMO-04", "22.4%", "5.1 / 16.0 GB", "ONLINE");
            }

            if (dgvSuCo.Rows.Count == 0)
            {
                dgvSuCo.Rows.Add("10:30:15", "PC-DEMO-02", "[Demo] CPU vượt ngưỡng 85%");
                dgvSuCo.Rows.Add("10:32:00", "PC-DEMO-03", "[Demo] Mất kết nối Heartbeat");
            }
        }

        /// <summary>
        /// Xóa dữ liệu minh họa khi bắt đầu nhận dữ liệu thật từ máy chủ (dành cho C-002/C-003).
        /// </summary>
        public void ClearDemoData()
        {
            dgvPhongMay.Rows.Clear();
            dgvSuCo.Rows.Clear();
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void btDangNhap_Click(object sender, EventArgs e)
        {

        }
    }
}
