namespace LabGuard.Admin
{
    partial class DashboardForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
            panel1 = new Panel();
            pictureBox1 = new PictureBox();
            label1 = new Label();
            btDangNhap = new Button();
            lblServerIP = new Label();
            lblConnectionStatus = new Label();
            panel2 = new Panel();
            splitter1 = new Splitter();
            pictureBox2 = new PictureBox();
            button2 = new Button();
            button1 = new Button();
            panel4 = new Panel();
            dgvPhongMay = new DataGridView();
            CotTenMay = new DataGridViewTextBoxColumn();
            CotCPU = new DataGridViewTextBoxColumn();
            CotRAM = new DataGridViewTextBoxColumn();
            CotTrangThai = new DataGridViewTextBoxColumn();
            dgvSuCo = new DataGridView();
            CotThoiGian = new DataGridViewTextBoxColumn();
            CotTenMaySuCo = new DataGridViewTextBoxColumn();
            CotChiTietSuCo = new DataGridViewTextBoxColumn();
            panel3 = new Panel();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            panel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPhongMay).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvSuCo).BeginInit();
            panel3.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(19, 39, 63);
            panel1.Controls.Add(pictureBox1);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(btDangNhap);
            panel1.Controls.Add(lblServerIP);
            panel1.Controls.Add(lblConnectionStatus);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(1363, 100);
            panel1.TabIndex = 0;
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImageLayout = ImageLayout.None;
            pictureBox1.Image = Properties.Resources.system;
            pictureBox1.Location = new Point(51, 13);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(79, 62);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            label1.Font = new Font("Century Gothic", 35F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.FromArgb(233, 212, 195);
            label1.Location = new Point(354, 13);
            label1.Name = "label1";
            label1.Size = new Size(734, 62);
            label1.TabIndex = 0;
            label1.Text = "LabGuard";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btDangNhap
            // 
            btDangNhap.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btDangNhap.BackColor = Color.FromArgb(185, 81, 24);
            btDangNhap.Enabled = false;
            btDangNhap.Cursor = Cursors.Hand;
            btDangNhap.FlatAppearance.BorderColor = Color.FromArgb(233, 212, 195);
            btDangNhap.FlatStyle = FlatStyle.Flat;
            btDangNhap.Font = new Font("Century Gothic", 9.5F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            btDangNhap.ForeColor = Color.FromArgb(233, 212, 195);
            btDangNhap.Location = new Point(1138, 35);
            btDangNhap.Name = "btDangNhap";
            btDangNhap.Size = new Size(186, 31);
            btDangNhap.TabIndex = 0;
            btDangNhap.Text = "Login";
            btDangNhap.UseVisualStyleBackColor = false;
            // 
            // lblServerIP
            // 
            lblServerIP.AutoSize = true;
            lblServerIP.Font = new Font("Century Gothic", 10.2F, FontStyle.Bold);
            lblServerIP.ForeColor = Color.FromArgb(233, 212, 196);
            lblServerIP.Location = new Point(136, 56);
            lblServerIP.Name = "lblServerIP";
            lblServerIP.Size = new Size(150, 19);
            lblServerIP.TabIndex = 1;
            lblServerIP.Text = "Máy chủ: IP:5000";
            // 
            // lblConnectionStatus
            // 
            lblConnectionStatus.AutoSize = true;
            lblConnectionStatus.Font = new Font("Century Gothic", 10.2F, FontStyle.Bold);
            lblConnectionStatus.ForeColor = Color.FromArgb(233, 212, 196);
            lblConnectionStatus.Location = new Point(136, 24);
            lblConnectionStatus.Name = "lblConnectionStatus";
            lblConnectionStatus.Size = new Size(160, 19);
            lblConnectionStatus.TabIndex = 0;
            lblConnectionStatus.Text = "Demo: Chưa kết nối";
            // 
            // panel2
            // 
            panel2.BackColor = Color.FromArgb(185, 81, 24);
            panel2.BorderStyle = BorderStyle.FixedSingle;
            panel2.Controls.Add(splitter1);
            panel2.Controls.Add(pictureBox2);
            panel2.Controls.Add(button2);
            panel2.Controls.Add(button1);
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 629);
            panel2.Name = "panel2";
            panel2.Size = new Size(1363, 74);
            panel2.TabIndex = 1;
            // 
            // splitter1
            // 
            splitter1.BackColor = Color.FromArgb(19, 39, 63);
            splitter1.Location = new Point(0, 0);
            splitter1.Name = "splitter1";
            splitter1.Size = new Size(771, 72);
            splitter1.TabIndex = 3;
            splitter1.TabStop = false;
            // 
            // pictureBox2
            // 
            pictureBox2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pictureBox2.BackgroundImageLayout = ImageLayout.None;
            pictureBox2.Image = Properties.Resources.wrench2;
            pictureBox2.Location = new Point(777, 9);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(79, 57);
            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2.TabIndex = 1;
            pictureBox2.TabStop = false;
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button2.BackColor = Color.FromArgb(54, 73, 46);
            button2.Enabled = false;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Font = new Font("Segoe UI Semibold", 10F);
            button2.ForeColor = Color.FromArgb(233, 212, 195);
            button2.Location = new Point(1027, 9);
            button2.Name = "button2";
            button2.Size = new Size(159, 57);
            button2.TabIndex = 1;
            button2.Text = "Giải quyết xong";
            button2.UseVisualStyleBackColor = false;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button1.BackColor = Color.FromArgb(79, 0, 5);
            button1.Enabled = false;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Segoe UI Semibold", 10F);
            button1.ForeColor = Color.FromArgb(233, 212, 195);
            button1.Location = new Point(862, 9);
            button1.Name = "button1";
            button1.Size = new Size(159, 57);
            button1.TabIndex = 0;
            button1.Text = "Tiếp nhận sự cố";
            button1.UseVisualStyleBackColor = false;
            // 
            // panel4
            // 
            panel4.Controls.Add(dgvPhongMay);
            panel4.Dock = DockStyle.Fill;
            panel4.Location = new Point(0, 100);
            panel4.Name = "panel4";
            panel4.Size = new Size(964, 529);
            panel4.TabIndex = 3;
            // 
            // dgvPhongMay
            // 
            dgvPhongMay.AllowUserToAddRows = false;
            dgvPhongMay.AllowUserToDeleteRows = false;
            dataGridViewCellStyle4.BackColor = Color.White;
            dataGridViewCellStyle4.SelectionBackColor = Color.FromArgb(185, 81, 24);
            dgvPhongMay.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle4;
            dgvPhongMay.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPhongMay.BackgroundColor = Color.FromArgb(148, 178, 196);
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = Color.White;
            dataGridViewCellStyle5.Font = new Font("Century Gothic", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle5.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle5.SelectionBackColor = Color.FromArgb(185, 81, 24);
            dataGridViewCellStyle5.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = DataGridViewTriState.True;
            dgvPhongMay.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            dgvPhongMay.ColumnHeadersHeight = 29;
            dgvPhongMay.Columns.AddRange(new DataGridViewColumn[] { CotTenMay, CotCPU, CotRAM, CotTrangThai });
            dgvPhongMay.Dock = DockStyle.Fill;
            dgvPhongMay.Location = new Point(0, 0);
            dgvPhongMay.Name = "dgvPhongMay";
            dgvPhongMay.RowHeadersVisible = false;
            dgvPhongMay.RowHeadersWidth = 51;
            dgvPhongMay.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPhongMay.Size = new Size(964, 529);
            dgvPhongMay.TabIndex = 0;
            dgvPhongMay.CellFormatting += dgvPhongMay_CellFormatting;
            // 
            // CotTenMay
            // 
            CotTenMay.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            CotTenMay.HeaderText = "Computer Name";
            CotTenMay.MinimumWidth = 6;
            CotTenMay.Name = "CotTenMay";
            CotTenMay.Width = 250;
            // 
            // CotCPU
            // 
            CotCPU.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            CotCPU.HeaderText = "CPU Usage (%)";
            CotCPU.MinimumWidth = 6;
            CotCPU.Name = "CotCPU";
            CotCPU.Width = 140;
            // 
            // CotRAM
            // 
            CotRAM.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            CotRAM.HeaderText = "RAM Usage (GB)";
            CotRAM.MinimumWidth = 6;
            CotRAM.Name = "CotRAM";
            CotRAM.Width = 140;
            // 
            // CotTrangThai
            // 
            CotTrangThai.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            CotTrangThai.HeaderText = "Status";
            CotTrangThai.MinimumWidth = 6;
            CotTrangThai.Name = "CotTrangThai";
            // 
            // dgvSuCo
            // 
            dgvSuCo.AllowUserToAddRows = false;
            dgvSuCo.BackgroundColor = Color.LightGray;
            dataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = SystemColors.Control;
            dataGridViewCellStyle6.Font = new Font("Century Gothic", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle6.ForeColor = Color.Black;
            dataGridViewCellStyle6.SelectionBackColor = Color.FromArgb(79, 0, 5);
            dataGridViewCellStyle6.SelectionForeColor = Color.White;
            dataGridViewCellStyle6.WrapMode = DataGridViewTriState.True;
            dgvSuCo.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle6;
            dgvSuCo.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSuCo.Columns.AddRange(new DataGridViewColumn[] { CotThoiGian, CotTenMaySuCo, CotChiTietSuCo });
            dgvSuCo.Dock = DockStyle.Fill;
            dgvSuCo.Location = new Point(0, 0);
            dgvSuCo.Name = "dgvSuCo";
            dgvSuCo.RowHeadersVisible = false;
            dgvSuCo.RowHeadersWidth = 51;
            dgvSuCo.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSuCo.ShowRowErrors = false;
            dgvSuCo.Size = new Size(399, 529);
            dgvSuCo.TabIndex = 2;
            // 
            // CotThoiGian
            // 
            CotThoiGian.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            CotThoiGian.HeaderText = "Time";
            CotThoiGian.MinimumWidth = 6;
            CotThoiGian.Name = "CotThoiGian";
            CotThoiGian.Width = 90;
            // 
            // CotTenMaySuCo
            // 
            CotTenMaySuCo.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            CotTenMaySuCo.HeaderText = "Computer";
            CotTenMaySuCo.MinimumWidth = 6;
            CotTenMaySuCo.Name = "CotTenMaySuCo";
            CotTenMaySuCo.Width = 105;
            // 
            // CotChiTietSuCo
            // 
            CotChiTietSuCo.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            CotChiTietSuCo.HeaderText = "Issue Details";
            CotChiTietSuCo.MinimumWidth = 6;
            CotChiTietSuCo.Name = "CotChiTietSuCo";
            // 
            // panel3
            // 
            panel3.Controls.Add(dgvSuCo);
            panel3.Dock = DockStyle.Right;
            panel3.Location = new Point(913, 100);
            panel3.Name = "panel3";
            panel3.Size = new Size(450, 529);
            panel3.TabIndex = 2;
            // 
            // LabGuard
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(1363, 703);
            Controls.Add(panel4);
            Controls.Add(panel3);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "LabGuard";
            Text = "LabGuard";
            Load += DashboardFromNew_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            panel4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvPhongMay).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvSuCo).EndInit();
            panel3.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Panel panel2;
        private Panel panel4;
        private Label lblConnectionStatus;
        private Label lblServerIP;
        private Button btDangNhap;
        private Label label1;
        private DataGridView dgvPhongMay;
        private PictureBox pictureBox1;
        private DataGridView dgvSuCo;
        private Panel panel3;
        private DataGridViewTextBoxColumn CotThoiGian;
        private DataGridViewTextBoxColumn CotTenMaySuCo;
        private DataGridViewTextBoxColumn CotChiTietSuCo;
        private Button button1;
        private Button button2;
        private DataGridViewTextBoxColumn CotTenMay;
        private DataGridViewTextBoxColumn CotCPU;
        private DataGridViewTextBoxColumn CotRAM;
        private DataGridViewTextBoxColumn CotTrangThai;
        private PictureBox pictureBox2;
        private Splitter splitter1;
    }
}
