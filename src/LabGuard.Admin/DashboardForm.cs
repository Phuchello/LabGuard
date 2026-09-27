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

            // 3. Tô màu tương ứng
            if (status == "ONLINE")
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(54, 73, 46);
            }
            else if (status == "WARNING")
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(218, 166, 62);
            }
            else
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(79, 0, 5);
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
