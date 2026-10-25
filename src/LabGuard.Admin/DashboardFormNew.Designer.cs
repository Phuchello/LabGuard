namespace LabGuard.Admin
{
    partial class DashboardFormNew
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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            pnlSidebar = new Panel();
            label1 = new Label();
            btnCommands = new Button();
            pnlNavIndicator = new Panel();
            btnLogs = new Button();
            btnServices = new Button();
            btnIncidents = new Button();
            btnDevices = new Button();
            lblLogoTitle = new Label();
            pictureBox1 = new PictureBox();
            btnOverview = new Button();
            pnlMain = new Panel();
            pnlOffline = new Panel();
            pnlAlerts = new Panel();
            panel4 = new Panel();
            pnlOnline = new Panel();
            pnlTotal = new Panel();
            pnlAction = new Panel();
            btnRestart = new Button();
            btnResolved = new Button();
            btnIntake = new Button();
            pnlChartContainer = new Panel();
            rtbLogs = new RichTextBox();
            dgvDevices = new DataGridView();
            colDeviceName = new DataGridViewTextBoxColumn();
            colIPAddress = new DataGridViewTextBoxColumn();
            colStatus = new DataGridViewTextBoxColumn();
            colCPU = new DataGridViewTextBoxColumn();
            colRAM = new DataGridViewTextBoxColumn();
            colLastSeen = new DataGridViewTextBoxColumn();
            pnlTabIndicator = new Panel();
            pnlHeader = new Panel();
            picProfile = new PictureBox();
            lblTabDashboard = new Label();
            pnlCentralStatus = new Panel();
            picStatusDot = new PictureBox();
            lblStatusBottom = new Label();
            lblStatusTop = new Label();
            lblTabOverview = new Label();
            lblHeaderTitle = new Label();
            pnlSidebar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            pnlMain.SuspendLayout();
            pnlAlerts.SuspendLayout();
            pnlAction.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDevices).BeginInit();
            pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picProfile).BeginInit();
            pnlCentralStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picStatusDot).BeginInit();
            SuspendLayout();
            // 
            // pnlSidebar
            // 
            pnlSidebar.Controls.Add(label1);
            pnlSidebar.Controls.Add(btnCommands);
            pnlSidebar.Controls.Add(pnlNavIndicator);
            pnlSidebar.Controls.Add(btnLogs);
            pnlSidebar.Controls.Add(btnServices);
            pnlSidebar.Controls.Add(btnIncidents);
            pnlSidebar.Controls.Add(btnDevices);
            pnlSidebar.Controls.Add(lblLogoTitle);
            pnlSidebar.Controls.Add(pictureBox1);
            pnlSidebar.Controls.Add(btnOverview);
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Location = new Point(0, 0);
            pnlSidebar.Name = "pnlSidebar";
            pnlSidebar.Size = new Size(250, 644);
            pnlSidebar.TabIndex = 0;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.ForeColor = SystemColors.ScrollBar;
            label1.Location = new Point(27, 601);
            label1.Name = "label1";
            label1.Size = new Size(103, 20);
            label1.TabIndex = 8;
            label1.Text = "LabGuard v0.2";
            // 
            // btnCommands
            // 
            btnCommands.BackColor = Color.Transparent;
            btnCommands.FlatAppearance.BorderSize = 0;
            btnCommands.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 39, 47);
            btnCommands.FlatStyle = FlatStyle.Flat;
            btnCommands.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnCommands.ForeColor = SystemColors.ScrollBar;
            btnCommands.Image = Properties.Resources.code;
            btnCommands.ImageAlign = ContentAlignment.MiddleLeft;
            btnCommands.Location = new Point(12, 380);
            btnCommands.Name = "btnCommands";
            btnCommands.Padding = new Padding(20, 0, 0, 0);
            btnCommands.Size = new Size(232, 45);
            btnCommands.TabIndex = 7;
            btnCommands.Text = "        Commands";
            btnCommands.TextAlign = ContentAlignment.MiddleLeft;
            btnCommands.UseVisualStyleBackColor = false;
            // 
            // pnlNavIndicator
            // 
            pnlNavIndicator.BackColor = Color.FromArgb(64, 120, 255);
            pnlNavIndicator.Location = new Point(27, 380);
            pnlNavIndicator.Name = "pnlNavIndicator";
            pnlNavIndicator.Size = new Size(4, 45);
            pnlNavIndicator.TabIndex = 0;
            // 
            // btnLogs
            // 
            btnLogs.BackColor = Color.Transparent;
            btnLogs.FlatAppearance.BorderSize = 0;
            btnLogs.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 39, 47);
            btnLogs.FlatStyle = FlatStyle.Flat;
            btnLogs.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnLogs.ForeColor = SystemColors.ScrollBar;
            btnLogs.Image = Properties.Resources.document;
            btnLogs.ImageAlign = ContentAlignment.MiddleLeft;
            btnLogs.Location = new Point(12, 329);
            btnLogs.Name = "btnLogs";
            btnLogs.Padding = new Padding(20, 0, 0, 0);
            btnLogs.Size = new Size(232, 45);
            btnLogs.TabIndex = 6;
            btnLogs.Text = "        Logs/Recent events";
            btnLogs.TextAlign = ContentAlignment.MiddleLeft;
            btnLogs.UseVisualStyleBackColor = false;
            // 
            // btnServices
            // 
            btnServices.BackColor = Color.Transparent;
            btnServices.FlatAppearance.BorderSize = 0;
            btnServices.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 39, 47);
            btnServices.FlatStyle = FlatStyle.Flat;
            btnServices.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnServices.ForeColor = SystemColors.ScrollBar;
            btnServices.Image = Properties.Resources.settings;
            btnServices.ImageAlign = ContentAlignment.MiddleLeft;
            btnServices.Location = new Point(12, 278);
            btnServices.Name = "btnServices";
            btnServices.Padding = new Padding(20, 0, 0, 0);
            btnServices.Size = new Size(232, 45);
            btnServices.TabIndex = 5;
            btnServices.Text = "        Services";
            btnServices.TextAlign = ContentAlignment.MiddleLeft;
            btnServices.UseVisualStyleBackColor = false;
            // 
            // btnIncidents
            // 
            btnIncidents.BackColor = Color.Transparent;
            btnIncidents.FlatAppearance.BorderSize = 0;
            btnIncidents.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 39, 47);
            btnIncidents.FlatStyle = FlatStyle.Flat;
            btnIncidents.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnIncidents.ForeColor = SystemColors.ScrollBar;
            btnIncidents.Image = Properties.Resources.error_message;
            btnIncidents.ImageAlign = ContentAlignment.MiddleLeft;
            btnIncidents.Location = new Point(12, 227);
            btnIncidents.Name = "btnIncidents";
            btnIncidents.Padding = new Padding(20, 0, 0, 0);
            btnIncidents.Size = new Size(232, 45);
            btnIncidents.TabIndex = 4;
            btnIncidents.Text = "        Incidents/Alerts";
            btnIncidents.TextAlign = ContentAlignment.MiddleLeft;
            btnIncidents.UseVisualStyleBackColor = false;
            // 
            // btnDevices
            // 
            btnDevices.BackColor = Color.Transparent;
            btnDevices.FlatAppearance.BorderSize = 0;
            btnDevices.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 39, 47);
            btnDevices.FlatStyle = FlatStyle.Flat;
            btnDevices.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnDevices.ForeColor = SystemColors.ScrollBar;
            btnDevices.Image = Properties.Resources.responsive;
            btnDevices.ImageAlign = ContentAlignment.MiddleLeft;
            btnDevices.Location = new Point(12, 176);
            btnDevices.Name = "btnDevices";
            btnDevices.Padding = new Padding(20, 0, 0, 0);
            btnDevices.Size = new Size(232, 45);
            btnDevices.TabIndex = 3;
            btnDevices.Text = "        Devices";
            btnDevices.TextAlign = ContentAlignment.MiddleLeft;
            btnDevices.UseVisualStyleBackColor = false;
            // 
            // lblLogoTitle
            // 
            lblLogoTitle.AutoSize = true;
            lblLogoTitle.BackColor = Color.Transparent;
            lblLogoTitle.Font = new Font("Segoe UI", 16.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblLogoTitle.ForeColor = Color.Transparent;
            lblLogoTitle.ImageAlign = ContentAlignment.MiddleLeft;
            lblLogoTitle.Location = new Point(101, 30);
            lblLogoTitle.Name = "lblLogoTitle";
            lblLogoTitle.Size = new Size(143, 38);
            lblLogoTitle.TabIndex = 2;
            lblLogoTitle.Text = "LabGuard";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.security1;
            pictureBox1.Location = new Point(12, 12);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(83, 74);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // btnOverview
            // 
            btnOverview.BackColor = Color.Transparent;
            btnOverview.FlatAppearance.BorderSize = 0;
            btnOverview.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 39, 47);
            btnOverview.FlatStyle = FlatStyle.Flat;
            btnOverview.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnOverview.ForeColor = SystemColors.ScrollBar;
            btnOverview.Image = Properties.Resources.home1;
            btnOverview.ImageAlign = ContentAlignment.MiddleLeft;
            btnOverview.Location = new Point(12, 125);
            btnOverview.Name = "btnOverview";
            btnOverview.Padding = new Padding(20, 0, 0, 0);
            btnOverview.RightToLeft = RightToLeft.No;
            btnOverview.Size = new Size(232, 45);
            btnOverview.TabIndex = 0;
            btnOverview.Text = "        Overview";
            btnOverview.TextAlign = ContentAlignment.MiddleLeft;
            btnOverview.UseVisualStyleBackColor = false;
            // 
            // pnlMain
            // 
            pnlMain.Controls.Add(pnlOffline);
            pnlMain.Controls.Add(pnlAlerts);
            pnlMain.Controls.Add(pnlOnline);
            pnlMain.Controls.Add(pnlTotal);
            pnlMain.Controls.Add(pnlAction);
            pnlMain.Controls.Add(pnlChartContainer);
            pnlMain.Controls.Add(rtbLogs);
            pnlMain.Controls.Add(dgvDevices);
            pnlMain.Controls.Add(pnlTabIndicator);
            pnlMain.Dock = DockStyle.Fill;
            pnlMain.Location = new Point(250, 93);
            pnlMain.Name = "pnlMain";
            pnlMain.Size = new Size(1121, 551);
            pnlMain.TabIndex = 0;
            // 
            // pnlOffline
            // 
            pnlOffline.Location = new Point(856, 16);
            pnlOffline.Name = "pnlOffline";
            pnlOffline.Size = new Size(250, 125);
            pnlOffline.TabIndex = 3;
            // 
            // pnlAlerts
            // 
            pnlAlerts.Controls.Add(panel4);
            pnlAlerts.Location = new Point(581, 16);
            pnlAlerts.Name = "pnlAlerts";
            pnlAlerts.Size = new Size(250, 125);
            pnlAlerts.TabIndex = 2;
            // 
            // panel4
            // 
            panel4.Location = new Point(249, 3);
            panel4.Name = "panel4";
            panel4.Size = new Size(250, 125);
            panel4.TabIndex = 2;
            // 
            // pnlOnline
            // 
            pnlOnline.Location = new Point(301, 16);
            pnlOnline.Name = "pnlOnline";
            pnlOnline.Size = new Size(250, 125);
            pnlOnline.TabIndex = 2;
            // 
            // pnlTotal
            // 
            pnlTotal.Location = new Point(25, 16);
            pnlTotal.Name = "pnlTotal";
            pnlTotal.Size = new Size(250, 125);
            pnlTotal.TabIndex = 1;
            // 
            // pnlAction
            // 
            pnlAction.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            pnlAction.Controls.Add(btnRestart);
            pnlAction.Controls.Add(btnResolved);
            pnlAction.Controls.Add(btnIntake);
            pnlAction.Location = new Point(758, 458);
            pnlAction.Name = "pnlAction";
            pnlAction.Size = new Size(351, 81);
            pnlAction.TabIndex = 7;
            // 
            // btnRestart
            // 
            btnRestart.BackColor = Color.DarkGreen;
            btnRestart.FlatAppearance.BorderSize = 0;
            btnRestart.FlatStyle = FlatStyle.Flat;
            btnRestart.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnRestart.ForeColor = Color.White;
            btnRestart.Location = new Point(178, 3);
            btnRestart.Name = "btnRestart";
            btnRestart.Size = new Size(170, 74);
            btnRestart.TabIndex = 8;
            btnRestart.Text = "Restart";
            btnRestart.UseVisualStyleBackColor = false;
            // 
            // btnResolved
            // 
            btnResolved.BackColor = Color.Navy;
            btnResolved.FlatAppearance.BorderSize = 0;
            btnResolved.FlatStyle = FlatStyle.Flat;
            btnResolved.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnResolved.ForeColor = Color.White;
            btnResolved.Location = new Point(8, 42);
            btnResolved.Name = "btnResolved";
            btnResolved.Size = new Size(164, 35);
            btnResolved.TabIndex = 4;
            btnResolved.Text = "Resolved";
            btnResolved.UseVisualStyleBackColor = false;
            // 
            // btnIntake
            // 
            btnIntake.BackColor = Color.Maroon;
            btnIntake.FlatAppearance.BorderSize = 0;
            btnIntake.FlatStyle = FlatStyle.Flat;
            btnIntake.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnIntake.ForeColor = Color.White;
            btnIntake.Location = new Point(8, 3);
            btnIntake.Name = "btnIntake";
            btnIntake.Size = new Size(164, 35);
            btnIntake.TabIndex = 3;
            btnIntake.Text = "Incident intake";
            btnIntake.UseVisualStyleBackColor = false;
            // 
            // pnlChartContainer
            // 
            pnlChartContainer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pnlChartContainer.Location = new Point(758, 174);
            pnlChartContainer.Name = "pnlChartContainer";
            pnlChartContainer.Size = new Size(351, 278);
            pnlChartContainer.TabIndex = 6;
            // 
            // rtbLogs
            // 
            rtbLogs.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rtbLogs.BackColor = Color.FromArgb(10, 10, 15);
            rtbLogs.BorderStyle = BorderStyle.None;
            rtbLogs.ForeColor = Color.Gray;
            rtbLogs.Location = new Point(25, 461);
            rtbLogs.Name = "rtbLogs";
            rtbLogs.ReadOnly = true;
            rtbLogs.Size = new Size(727, 81);
            rtbLogs.TabIndex = 5;
            rtbLogs.Text = "";
            // 
            // dgvDevices
            // 
            dgvDevices.AllowUserToAddRows = false;
            dgvDevices.AllowUserToDeleteRows = false;
            dgvDevices.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvDevices.BackgroundColor = Color.FromArgb(23, 26, 33);
            dgvDevices.BorderStyle = BorderStyle.None;
            dgvDevices.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(35, 40, 50);
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = Color.White;
            dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(35, 40, 50);
            dataGridViewCellStyle1.SelectionForeColor = Color.Honeydew;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dgvDevices.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvDevices.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDevices.Columns.AddRange(new DataGridViewColumn[] { colDeviceName, colIPAddress, colStatus, colCPU, colRAM, colLastSeen });
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = Color.FromArgb(23, 26, 33);
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = SystemColors.ScrollBar;
            dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(50, 55, 65);
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            dgvDevices.DefaultCellStyle = dataGridViewCellStyle2;
            dgvDevices.EnableHeadersVisualStyles = false;
            dgvDevices.Location = new Point(25, 174);
            dgvDevices.Name = "dgvDevices";
            dgvDevices.ReadOnly = true;
            dgvDevices.RowHeadersVisible = false;
            dgvDevices.RowHeadersWidth = 51;
            dgvDevices.Size = new Size(727, 278);
            dgvDevices.TabIndex = 4;
            // 
            // colDeviceName
            // 
            colDeviceName.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colDeviceName.HeaderText = "Device Name";
            colDeviceName.MinimumWidth = 6;
            colDeviceName.Name = "colDeviceName";
            colDeviceName.ReadOnly = true;
            colDeviceName.Width = 120;
            // 
            // colIPAddress
            // 
            colIPAddress.HeaderText = "IP Address";
            colIPAddress.MinimumWidth = 6;
            colIPAddress.Name = "colIPAddress";
            colIPAddress.ReadOnly = true;
            colIPAddress.Width = 150;
            // 
            // colStatus
            // 
            colStatus.HeaderText = "Status";
            colStatus.MinimumWidth = 6;
            colStatus.Name = "colStatus";
            colStatus.ReadOnly = true;
            colStatus.Width = 125;
            // 
            // colCPU
            // 
            colCPU.HeaderText = "CPU";
            colCPU.MinimumWidth = 6;
            colCPU.Name = "colCPU";
            colCPU.ReadOnly = true;
            colCPU.Width = 120;
            // 
            // colRAM
            // 
            colRAM.HeaderText = "RAM";
            colRAM.MinimumWidth = 6;
            colRAM.Name = "colRAM";
            colRAM.ReadOnly = true;
            colRAM.Width = 120;
            // 
            // colLastSeen
            // 
            colLastSeen.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colLastSeen.HeaderText = "Last Seen";
            colLastSeen.MinimumWidth = 6;
            colLastSeen.Name = "colLastSeen";
            colLastSeen.ReadOnly = true;
            // 
            // pnlTabIndicator
            // 
            pnlTabIndicator.ForeColor = Color.FromArgb(64, 120, 255);
            pnlTabIndicator.Location = new Point(20, 0);
            pnlTabIndicator.Name = "pnlTabIndicator";
            pnlTabIndicator.Size = new Size(79, 2);
            pnlTabIndicator.TabIndex = 0;
            // 
            // pnlHeader
            // 
            pnlHeader.Controls.Add(picProfile);
            pnlHeader.Controls.Add(lblTabDashboard);
            pnlHeader.Controls.Add(pnlCentralStatus);
            pnlHeader.Controls.Add(lblTabOverview);
            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(250, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(1121, 93);
            pnlHeader.TabIndex = 0;
            // 
            // picProfile
            // 
            picProfile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            picProfile.Image = Properties.Resources.profile_picture1;
            picProfile.Location = new Point(1035, 12);
            picProfile.Name = "picProfile";
            picProfile.Size = new Size(61, 56);
            picProfile.SizeMode = PictureBoxSizeMode.Zoom;
            picProfile.TabIndex = 1;
            picProfile.TabStop = false;
            // 
            // lblTabDashboard
            // 
            lblTabDashboard.AutoSize = true;
            lblTabDashboard.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTabDashboard.ForeColor = Color.FromArgb(150, 150, 150);
            lblTabDashboard.Location = new Point(110, 53);
            lblTabDashboard.Name = "lblTabDashboard";
            lblTabDashboard.Size = new Size(93, 23);
            lblTabDashboard.TabIndex = 1;
            lblTabDashboard.Text = "Dashboard";
            // 
            // pnlCentralStatus
            // 
            pnlCentralStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pnlCentralStatus.BackColor = Color.FromArgb(35, 40, 50);
            pnlCentralStatus.BorderStyle = BorderStyle.FixedSingle;
            pnlCentralStatus.Controls.Add(picStatusDot);
            pnlCentralStatus.Controls.Add(lblStatusBottom);
            pnlCentralStatus.Controls.Add(lblStatusTop);
            pnlCentralStatus.Location = new Point(762, 12);
            pnlCentralStatus.Name = "pnlCentralStatus";
            pnlCentralStatus.Size = new Size(258, 56);
            pnlCentralStatus.TabIndex = 1;
            // 
            // picStatusDot
            // 
            picStatusDot.Location = new Point(15, 17);
            picStatusDot.Name = "picStatusDot";
            picStatusDot.Size = new Size(15, 15);
            picStatusDot.TabIndex = 1;
            picStatusDot.TabStop = false;
            // 
            // lblStatusBottom
            // 
            lblStatusBottom.AutoSize = true;
            lblStatusBottom.Font = new Font("Segoe UI", 7.20000029F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblStatusBottom.ForeColor = Color.Lime;
            lblStatusBottom.Location = new Point(45, 25);
            lblStatusBottom.Name = "lblStatusBottom";
            lblStatusBottom.Size = new Size(201, 17);
            lblStatusBottom.TabIndex = 1;
            lblStatusBottom.Text = "CENTRAL SERVER: CONNECTED";
            // 
            // lblStatusTop
            // 
            lblStatusTop.AutoSize = true;
            lblStatusTop.Font = new Font("Segoe UI", 7.20000029F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblStatusTop.ForeColor = Color.Transparent;
            lblStatusTop.Location = new Point(45, 8);
            lblStatusTop.Name = "lblStatusTop";
            lblStatusTop.Size = new Size(88, 17);
            lblStatusTop.TabIndex = 0;
            lblStatusTop.Text = "Central Status";
            // 
            // lblTabOverview
            // 
            lblTabOverview.AutoSize = true;
            lblTabOverview.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTabOverview.ForeColor = Color.FromArgb(64, 120, 255);
            lblTabOverview.Location = new Point(25, 54);
            lblTabOverview.Name = "lblTabOverview";
            lblTabOverview.Size = new Size(79, 23);
            lblTabOverview.TabIndex = 0;
            lblTabOverview.Text = "Overview";
            // 
            // lblHeaderTitle
            // 
            lblHeaderTitle.AutoSize = true;
            lblHeaderTitle.Font = new Font("Segoe UI", 16.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblHeaderTitle.ForeColor = Color.White;
            lblHeaderTitle.Location = new Point(20, 15);
            lblHeaderTitle.Name = "lblHeaderTitle";
            lblHeaderTitle.Size = new Size(293, 38);
            lblHeaderTitle.TabIndex = 0;
            lblHeaderTitle.Text = "LabGuard Dashboard";
            // 
            // DashboardFormNew
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(20, 23, 31);
            ClientSize = new Size(1371, 644);
            Controls.Add(pnlMain);
            Controls.Add(pnlHeader);
            Controls.Add(pnlSidebar);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "DashboardFormNew";
            Text = "LabGuard";
            pnlSidebar.ResumeLayout(false);
            pnlSidebar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            pnlMain.ResumeLayout(false);
            pnlAlerts.ResumeLayout(false);
            pnlAction.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvDevices).EndInit();
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picProfile).EndInit();
            pnlCentralStatus.ResumeLayout(false);
            pnlCentralStatus.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picStatusDot).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlSidebar;
        private Button btnOverview;
        private Panel pnlMain;
        private Panel pnlHeader;
        private PictureBox pictureBox1;
        private Button btnLogs;
        private Button btnServices;
        private Button btnIncidents;
        private Button btnDevices;
        private Panel pnlNavIndicator;
        private Button btnCommands;
        private Label lblHeaderTitle;
        private Label lblTabOverview;
        private Label lblTabDashboard;
        private Panel pnlTabIndicator;
        private Panel pnlCentralStatus;
        private Label lblStatusTop;
        private Label lblStatusBottom;
        private PictureBox picStatusDot;
        private PictureBox picProfile;
        private DataGridView dgvDevices;
        private RichTextBox rtbLogs;
        private DataGridViewTextBoxColumn colDeviceName;
        private DataGridViewTextBoxColumn colIPAddress;
        private DataGridViewTextBoxColumn colStatus;
        private DataGridViewTextBoxColumn colCPU;
        private DataGridViewTextBoxColumn colRAM;
        private DataGridViewTextBoxColumn colLastSeen;
        private Panel pnlChartContainer;
        private Panel pnlAction;
        private Button btnIntake;
        private Button btnRestart;
        private Button btnResolved;
        private Panel pnlOffline;
        private Panel pnlAlerts;
        private Panel panel4;
        private Panel pnlOnline;
        private Panel pnlTotal;
        private Label label1;
        private Label lblLogoTitle;
    }
}