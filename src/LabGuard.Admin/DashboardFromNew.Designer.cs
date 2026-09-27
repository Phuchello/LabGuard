namespace LabGuard.Admin
{
    partial class DashboardFromNew
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
            panel1 = new Panel();
            panel2 = new Panel();
            panel4 = new Panel();
            ConnectionStatus = new Label();
            ServerIP = new Label();
            DangNhap = new Button();
            label1 = new Label();
            panel3 = new Panel();
            dataGridView1 = new DataGridView();
            CotTenMay = new DataGridViewTextBoxColumn();
            CotCPU = new DataGridViewTextBoxColumn();
            CotRAM = new DataGridViewTextBoxColumn();
            CotTrangThai = new DataGridViewTextBoxColumn();
            panel1.SuspendLayout();
            panel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = Color.SteelBlue;
            panel1.Controls.Add(label1);
            panel1.Controls.Add(DangNhap);
            panel1.Controls.Add(ServerIP);
            panel1.Controls.Add(ConnectionStatus);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(1377, 100);
            panel1.TabIndex = 0;
            panel1.Paint += panel1_Paint;
            // 
            // panel2
            // 
            panel2.Dock = DockStyle.Left;
            panel2.Location = new Point(0, 100);
            panel2.Name = "panel2";
            panel2.Size = new Size(177, 603);
            panel2.TabIndex = 1;
            // 
            // panel4
            // 
            panel4.Controls.Add(dataGridView1);
            panel4.Dock = DockStyle.Fill;
            panel4.Location = new Point(177, 100);
            panel4.Name = "panel4";
            panel4.Size = new Size(769, 603);
            panel4.TabIndex = 3;
            // 
            // ConnectionStatus
            // 
            ConnectionStatus.AutoSize = true;
            ConnectionStatus.Location = new Point(48, 23);
            ConnectionStatus.Name = "ConnectionStatus";
            ConnectionStatus.Size = new Size(167, 20);
            ConnectionStatus.TabIndex = 0;
            ConnectionStatus.Text = "Trạng thái: Đang kết nối";
            ConnectionStatus.Click += label1_Click;
            // 
            // ServerIP
            // 
            ServerIP.AutoSize = true;
            ServerIP.Location = new Point(48, 55);
            ServerIP.Name = "ServerIP";
            ServerIP.Size = new Size(118, 20);
            ServerIP.TabIndex = 1;
            ServerIP.Text = "Máy chủ: IP:5000";
            // 
            // DangNhap
            // 
            DangNhap.Location = new Point(1161, 51);
            DangNhap.Name = "DangNhap";
            DangNhap.Size = new Size(171, 29);
            DangNhap.TabIndex = 0;
            DangNhap.Text = "Đăng Nhập";
            DangNhap.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.Font = new Font("Century Gothic", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.ForeColor = SystemColors.ControlLightLight;
            label1.ImageAlign = ContentAlignment.MiddleLeft;
            label1.Location = new Point(1140, 9);
            label1.Name = "label1";
            label1.Size = new Size(210, 34);
            label1.TabIndex = 0;
            label1.Text = "Team 5 - LabGuard";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            label1.Click += label1_Click_2;
            // 
            // panel3
            // 
            panel3.Dock = DockStyle.Right;
            panel3.Location = new Point(946, 100);
            panel3.Name = "panel3";
            panel3.Size = new Size(431, 603);
            panel3.TabIndex = 2;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { CotTenMay, CotCPU, CotRAM, CotTrangThai });
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Location = new Point(0, 0);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.Size = new Size(769, 603);
            dataGridView1.TabIndex = 0;
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
            // 
            // CotTenMay
            // 
            CotTenMay.HeaderText = "Tên máy tính";
            CotTenMay.MinimumWidth = 6;
            CotTenMay.Name = "CotTenMay";
            CotTenMay.Width = 125;
            // 
            // CotCPU
            // 
            CotCPU.HeaderText = "Mức CPU (%)";
            CotCPU.MinimumWidth = 6;
            CotCPU.Name = "CotCPU";
            CotCPU.Width = 125;
            // 
            // CotRAM
            // 
            CotRAM.HeaderText = "Mức RAM (GB)";
            CotRAM.MinimumWidth = 6;
            CotRAM.Name = "CotRAM";
            CotRAM.Width = 125;
            // 
            // CotTrangThai
            // 
            CotTrangThai.HeaderText = "Trạng thái sự cố";
            CotTrangThai.MinimumWidth = 6;
            CotTrangThai.Name = "CotTrangThai";
            CotTrangThai.Width = 125;
            // 
            // DashboardFromNew
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1377, 703);
            Controls.Add(panel4);
            Controls.Add(panel3);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "DashboardFromNew";
            Text = "DashboardFromNew";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Panel panel2;
        private Panel panel4;
        private Label ConnectionStatus;
        private Label ServerIP;
        private Button DangNhap;
        private Label label1;
        private DataGridView dataGridView1;
        private Panel panel3;
        private DataGridViewTextBoxColumn CotTenMay;
        private DataGridViewTextBoxColumn CotCPU;
        private DataGridViewTextBoxColumn CotRAM;
        private DataGridViewTextBoxColumn CotTrangThai;
    }
}