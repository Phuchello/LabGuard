using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ScottPlot.WinForms;

namespace LabGuard.Admin
{
    public partial class DashboardFormNew : Form
    {
        private Button? currentButton;
        private System.Windows.Forms.Timer waveTimer;
        private float waveOffset = 0f;
        private FormsPlot? formsPlot;
        private Panel pnlDeviceDetails = new Panel();
        private readonly List<double> cpuData = new();
        private readonly List<double> ramData = new();

        private int statTotal = 26;
        private int statOnline = 22;
        private int statOffline = 4;
        private int statAlerts = 3;
        private int statCritical = 2;
        private int statWarning = 1;

        public DashboardFormNew()
        {
            InitializeComponent();
            InitSidebarLogic();
            InitHeaderLogic();
            InitDataGridViewLogic();
            InitDashboardCards();
            InitChartLogic();
            InitActionButtons();
            InitDeviceDetails();
        }

        private void EnableDoubleBuffering(Control control)
        {
            if (control == null) return;
            typeof(Control).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, control, new object[] { true });
        }

        private void InitSidebarLogic()
        {
            Button?[] menuButtons = { btnOverview, btnDevices, btnIncidents, btnServices, btnLogs, btnCommands };
            foreach (var btn in menuButtons)
            {
                if (btn != null) btn.Click += MenuButton_Click;
            }

            if (btnOverview != null) ActivateButton(btnOverview);
            CreateFutureBadge();
        }

        private void MenuButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button clickedBtn) ActivateButton(clickedBtn);
        }

        private void ActivateButton(Button btnSender)
        {
            if (currentButton != null)
            {
                currentButton.BackColor = Color.Transparent;
                currentButton.ForeColor = Color.FromArgb(200, 200, 200);
            }

            currentButton = btnSender;
            currentButton.BackColor = Color.FromArgb(35, 40, 50);
            currentButton.ForeColor = Color.White;

            if (pnlNavIndicator != null)
            {
                pnlNavIndicator.Top = currentButton.Top;
                pnlNavIndicator.BringToFront();
            }
        }

        private void CreateFutureBadge()
        {
            if (btnCommands == null) return;

            Label lblFuture = new Label
            {
                Text = "FUTURE",
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(100, 70, 200),
                AutoSize = false,
                Size = new Size(50, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };

            using (GraphicsPath path = new GraphicsPath())
            {
                int radius = 8;
                path.AddArc(0, 0, radius, radius, 180, 90);
                path.AddArc(lblFuture.Width - radius, 0, radius, radius, 270, 90);
                path.AddArc(lblFuture.Width - radius, lblFuture.Height - radius, radius, radius, 0, 90);
                path.AddArc(0, lblFuture.Height - radius, radius, radius, 90, 90);
                lblFuture.Region = new Region(path);
            }

            lblFuture.Location = new Point(btnCommands.Width - lblFuture.Width - 10, (btnCommands.Height - lblFuture.Height) / 2);
            lblFuture.Click += (s, e) => btnCommands.PerformClick();
            btnCommands.Controls.Add(lblFuture);
        }

        private void InitHeaderLogic()
        {
            if (lblTabOverview != null) lblTabOverview.Click += Tab_Click;
            if (lblTabDashboard != null) lblTabDashboard.Click += Tab_Click;

            if (lblLogoTitle != null)
            {
                lblLogoTitle.ForeColor = Color.Transparent;
                waveTimer = new System.Windows.Forms.Timer { Interval = 50 };
                waveTimer.Tick += (s, e) =>
                {
                    waveOffset = (waveOffset + 0.05f) % 1f;
                    lblLogoTitle.Invalidate();
                };
                lblLogoTitle.Paint += LblLogoTitle_Paint;
                waveTimer.Start();
            }
        }

        private void Tab_Click(object? sender, EventArgs e)
        {
            if (sender is Label clickedTab)
            {
                if (lblTabOverview != null) lblTabOverview.ForeColor = Color.FromArgb(150, 150, 150);
                if (lblTabDashboard != null) lblTabDashboard.ForeColor = Color.FromArgb(150, 150, 150);

                clickedTab.ForeColor = Color.FromArgb(64, 120, 255);

                if (pnlTabIndicator != null)
                {
                    pnlTabIndicator.Width = clickedTab.Width;
                    pnlTabIndicator.Left = clickedTab.Left;
                }
            }
        }

        private void LblLogoTitle_Paint(object? sender, PaintEventArgs e)
        {
            if (lblLogoTitle == null || lblLogoTitle.ClientRectangle.Width == 0) return;

            e.Graphics.Clear(Color.FromArgb(25, 30, 40));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            Rectangle rect = lblLogoTitle.ClientRectangle;

            using (LinearGradientBrush brush = new LinearGradientBrush(new Point(0, 0), new Point(rect.Width, 0), Color.White, Color.White))
            {
                ColorBlend blend = new ColorBlend
                {
                    Positions = new float[] { 0f, 0.5f, 1f },
                    Colors = new Color[] { Color.FromArgb(10, 40, 100), Color.FromArgb(64, 120, 255), Color.FromArgb(10, 40, 100) }
                };
                brush.InterpolationColors = blend;
                brush.TranslateTransform(waveOffset * rect.Width, 0);
                brush.WrapMode = WrapMode.Tile;
                e.Graphics.DrawString(lblLogoTitle.Text, lblLogoTitle.Font, brush, 0, 0);
            }
        }

        private void InitDataGridViewLogic()
        {
            if (dgvDevices != null)
            {
                EnableDoubleBuffering(dgvDevices);
                dgvDevices.CellPainting += DgvDevices_CellPainting;
            }
        }

        private void DgvDevices_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dgvDevices == null) return;
            string colName = dgvDevices.Columns[e.ColumnIndex].Name;

            bool isSelected = (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;

            if (colName == "colStatus")
            {
                e.PaintBackground(e.CellBounds, isSelected);
                string status = e.Value?.ToString()?.ToLower() ?? "";
                Color dotCol = status.Contains("online") ? Color.LimeGreen : status.Contains("offline") ? Color.OrangeRed : Color.Gray;

                if (e.CellStyle.Font != null)
                {
                    TextRenderer.DrawText(e.Graphics!, status, e.CellStyle.Font,
                        new Rectangle(e.CellBounds.X + 20, e.CellBounds.Y, e.CellBounds.Width - 20, e.CellBounds.Height),
                        e.CellStyle.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                }

                e.Graphics!.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush dotBrush = new SolidBrush(dotCol))
                {
                    e.Graphics.FillEllipse(dotBrush, e.CellBounds.X + 5, e.CellBounds.Y + (e.CellBounds.Height / 2) - 4, 8, 8);
                }
                e.Handled = true;
            }
            else if (colName == "colCPU" || colName == "colRAM")
            {
                e.PaintBackground(e.CellBounds, isSelected);
                if (e.Value != null && double.TryParse(e.Value.ToString(), out double pct))
                {
                    pct = Math.Clamp(pct, 0, 100);
                    Rectangle bgBar = new Rectangle(e.CellBounds.X + 5, e.CellBounds.Y + 12, e.CellBounds.Width - 15, e.CellBounds.Height - 24);

                    using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(40, 45, 55)))
                    {
                        e.Graphics!.FillRectangle(bgBrush, bgBar);
                    }

                    if (pct > 0)
                    {
                        Rectangle fillBar = new Rectangle(bgBar.X, bgBar.Y, (int)((pct / 100.0) * bgBar.Width), bgBar.Height);
                        Color fillCol = (colName == "colCPU") ? Color.FromArgb(64, 120, 255) : Color.LimeGreen;

                        using (GraphicsPath p = new GraphicsPath())
                        using (SolidBrush fillBrush = new SolidBrush(fillCol))
                        {
                            int r = 4;
                            p.AddArc(fillBar.X, fillBar.Y, r, r, 180, 90);
                            p.AddArc(fillBar.X + fillBar.Width - r, fillBar.Y, r, r, 270, 90);
                            p.AddArc(fillBar.X + fillBar.Width - r, fillBar.Bottom - r, r, r, 0, 90);
                            p.AddArc(fillBar.X, fillBar.Bottom - r, r, r, 90, 90);

                            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            e.Graphics.FillPath(fillBrush, p);
                        }
                    }
                }
                e.Handled = true;
            }
        }

        private void InitDashboardCards()
        {
            Panel[] panels = { pnlTotal, pnlOnline, pnlAlerts, pnlOffline };
            foreach (var pnl in panels)
            {
                if (pnl != null) EnableDoubleBuffering(pnl);
            }

            ArrangeCards();
            this.Resize += (s, e) => ArrangeCards();

            if (pnlTotal != null) pnlTotal.Paint += (s, e) => DrawCard(e.Graphics, pnlTotal.ClientRectangle, "TỔNG MÁY", statTotal.ToString(), "Tất cả thiết bị trong phòng máy", Color.FromArgb(64, 120, 255), -1, "💻");
            if (pnlOnline != null) pnlOnline.Paint += (s, e) => DrawCard(e.Graphics, pnlOnline.ClientRectangle, "ĐANG ONLINE", statOnline.ToString(), $"{(statTotal > 0 ? (statOnline * 100.0 / statTotal).ToString("F1") : "0")}% tổng số máy", Color.LimeGreen, statTotal > 0 ? (float)statOnline / statTotal * 100 : 0, "✔");
            if (pnlAlerts != null) pnlAlerts.Paint += (s, e) => DrawCard(e.Graphics, pnlAlerts.ClientRectangle, "CẢNH BÁO / SỰ CỐ MỞ", statAlerts.ToString(), $"{statCritical} nghiêm trọng, {statWarning} cảnh báo", Color.FromArgb(235, 60, 60), 30f, "⚠");
            if (pnlOffline != null) pnlOffline.Paint += (s, e) => DrawCard(e.Graphics, pnlOffline.ClientRectangle, "MẤT KẾT NỐI", statOffline.ToString(), $"{(statTotal > 0 ? (statOffline * 100.0 / statTotal).ToString("F1") : "0")}% tổng số máy", Color.FromArgb(150, 160, 180), statTotal > 0 ? (float)statOffline / statTotal * 100 : 0, "🔗");
        }

        private void ArrangeCards()
        {
            if (pnlTotal == null || pnlOnline == null || pnlAlerts == null || pnlOffline == null) return;
            Control parent = pnlTotal.Parent;
            if (parent == null) return;

            if (pnlChartContainer != null) pnlChartContainer.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            if (dgvDevices != null) dgvDevices.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            if (pnlDeviceDetails != null) pnlDeviceDetails.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            if (rtbLogs != null) rtbLogs.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            if (pnlAction != null) pnlAction.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            int spacing = 15;
            int startX = 20;
            int startY = 20;
            int cardHeight = 110;

            int availableWidth = Math.Max(100, parent.Width - (startX * 2));
            int cardWidth = (availableWidth - (spacing * 3)) / 4;

            pnlTotal.Bounds = new Rectangle(startX, startY, cardWidth, cardHeight);
            pnlOnline.Bounds = new Rectangle(startX + cardWidth + spacing, startY, cardWidth, cardHeight);
            pnlAlerts.Bounds = new Rectangle(startX + (cardWidth + spacing) * 2, startY, cardWidth, cardHeight);
            pnlOffline.Bounds = new Rectangle(startX + (cardWidth + spacing) * 3, startY, cardWidth, cardHeight);

            int contentTop = pnlTotal.Bottom + 15;
            int blackPanelHeight = (this.WindowState == FormWindowState.Maximized) ? 140 : 70;
            int bottomMargin = blackPanelHeight + 35;

            if (pnlChartContainer != null && dgvDevices != null)
            {
                if (this.WindowState == FormWindowState.Maximized)
                {
                    pnlDeviceDetails.Visible = true;

                    int rightColWidth = 350;
                    int rightColLeft = parent.Width - startX - rightColWidth;

                    pnlChartContainer.Bounds = new Rectangle(rightColLeft, contentTop, rightColWidth, rightColWidth);

                    int detailsTop = pnlChartContainer.Bottom + 15;
                    int detailsHeight = parent.Height - detailsTop - bottomMargin;
                    pnlDeviceDetails.Bounds = new Rectangle(rightColLeft, detailsTop, rightColWidth, detailsHeight);

                    dgvDevices.Bounds = new Rectangle(startX, contentTop, rightColLeft - startX - 15, parent.Height - contentTop - bottomMargin);
                }
                else
                {
                    pnlDeviceDetails.Visible = false;
                    int chartHeight = parent.Height - contentTop - bottomMargin;
                    int rightColLeft = parent.Width - startX - chartHeight;
                    pnlChartContainer.Bounds = new Rectangle(rightColLeft, contentTop, chartHeight, chartHeight);
                    dgvDevices.Bounds = new Rectangle(startX, contentTop, rightColLeft - startX - 15, chartHeight);
                }
            }

            int blackPanelTop = parent.Height - blackPanelHeight - 20;

            if (rtbLogs != null && dgvDevices != null)
            {
                rtbLogs.Bounds = new Rectangle(startX, blackPanelTop, dgvDevices.Width, blackPanelHeight);
            }

            if (pnlAction != null && pnlChartContainer != null)
            {
                pnlAction.Bounds = new Rectangle(pnlChartContainer.Left, blackPanelTop, pnlChartContainer.Width, blackPanelHeight);

                int gap = 0;
                int leftColWidth = (pnlAction.Width * 55) / 100;
                int rightColWidth = pnlAction.Width - leftColWidth - gap;
                int halfHeight = (pnlAction.Height - gap) / 2;

                float btnFontSize = (this.WindowState == FormWindowState.Maximized) ? 12f : 10.5f;

                foreach (Control ctrl in pnlAction.Controls)
                {
                    ctrl.Font = new Font("Segoe UI", btnFontSize, FontStyle.Bold);
                    string txt = ctrl.Text.ToLower();

                    if (txt.Contains("restart"))
                    {
                        ctrl.Bounds = new Rectangle(leftColWidth + gap, 0, rightColWidth, pnlAction.Height);
                    }
                    else if (txt.Contains("intake") || txt.Contains("incident"))
                    {
                        ctrl.Bounds = new Rectangle(0, 0, leftColWidth, halfHeight);
                    }
                    else if (txt.Contains("resolved"))
                    {
                        ctrl.Bounds = new Rectangle(0, halfHeight + gap, leftColWidth, pnlAction.Height - halfHeight - gap);
                    }
                }
            }

            pnlTotal.Invalidate();
            pnlOnline.Invalidate();
            pnlAlerts.Invalidate();
            pnlOffline.Invalidate();
            pnlDeviceDetails.Invalidate();
        }

        private void DrawCard(Graphics g, Rectangle rect, string title, string value, string subText, Color themeColor, float progressPct, string iconSymbol)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (GraphicsPath path = new GraphicsPath())
            {
                int r = 10;
                int x = 2;
                int y = 2;
                int w = rect.Width - 5;
                int h = rect.Height - 5;

                path.AddArc(x, y, r, r, 180, 90);
                path.AddArc(x + w - r, y, r, r, 270, 90);
                path.AddArc(x + w - r, y + h - r, r, r, 0, 90);
                path.AddArc(x, y + h - r, r, r, 90, 90);
                path.CloseFigure();

                using (Pen borderPen = new Pen(Color.FromArgb(80, themeColor), 1.5f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            int iconSize = 42;
            Rectangle iconRect = new Rectangle(rect.X + 12, rect.Y + (rect.Height - iconSize) / 2 - 5, iconSize, iconSize);

            using (SolidBrush iconBg = new SolidBrush(Color.FromArgb(30, themeColor)))
            {
                g.FillEllipse(iconBg, iconRect);
            }

            using (Font iconFont = new Font("Segoe UI Emoji", 14, FontStyle.Regular))
            using (SolidBrush iconBrush = new SolidBrush(themeColor))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(iconSymbol, iconFont, iconBrush, iconRect, sf);
            }

            int textX = iconRect.Right + 10;
            using (Font titleFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (Font valueFont = new Font("Segoe UI", 22, FontStyle.Bold))
            using (Font subFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (SolidBrush subBrush = subText.Contains("%") ? new SolidBrush(themeColor) : new SolidBrush(Color.FromArgb(170, 170, 170)))
            {
                g.DrawString(title, titleFont, Brushes.LightGray, textX, rect.Y + 12);
                g.DrawString(value, valueFont, Brushes.White, textX - 2, rect.Y + 26);
                g.DrawString(subText, subFont, subBrush, textX, rect.Y + 68);
            }

            if (progressPct >= 0)
            {
                int barWidth = rect.Width - textX - 14;
                int barHeight = 4;
                int barY = rect.Bottom - 16;

                using (SolidBrush bgBar = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                {
                    g.FillRectangle(bgBar, textX, barY, barWidth, barHeight);
                }

                int fillWidth = (int)(barWidth * Math.Min(progressPct, 100) / 100f);
                if (fillWidth > 0)
                {
                    using (SolidBrush fillBar = new SolidBrush(themeColor))
                    {
                        g.FillRectangle(fillBar, textX, barY, fillWidth, barHeight);
                    }
                }
            }
        }

        private void InitChartLogic()
        {
            if (pnlChartContainer == null) return;

            formsPlot = new FormsPlot { Dock = DockStyle.Fill };
            pnlChartContainer.Controls.Add(formsPlot);

            cpuData.AddRange(new double[] { 42, 50, 48, 65, 80, 75, 60, 55, 68, 50, 45, 48, 52, 60, 55 });
            ramData.AddRange(new double[] { 35, 36, 38, 40, 45, 50, 48, 42, 40, 45, 47, 45, 42, 40, 38 });
            RenderScottPlot();
        }

        private void RenderScottPlot()
        {
            if (formsPlot == null || cpuData.Count == 0) return;

            formsPlot.Plot.Clear();
            double[] xs = new double[cpuData.Count];
            for (int i = 0; i < xs.Length; i++) xs[i] = i;

            var cpuLine = formsPlot.Plot.Add.Scatter(xs, cpuData.ToArray());
            cpuLine.LegendText = "CPU Usage";
            cpuLine.Color = ScottPlot.Color.FromColor(System.Drawing.Color.FromArgb(64, 120, 255));
            cpuLine.LineWidth = 2.5f;

            var ramLine = formsPlot.Plot.Add.Scatter(xs, ramData.ToArray());
            ramLine.LegendText = "RAM Usage";
            ramLine.Color = ScottPlot.Color.FromColor(System.Drawing.Color.LimeGreen);
            ramLine.LineWidth = 2.5f;

            formsPlot.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#151820");
            formsPlot.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#151820");
            formsPlot.Plot.Axes.Color(ScottPlot.Color.FromHex("#808080"));
            formsPlot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#252a35");

            formsPlot.Plot.Axes.SetLimitsY(0, 100);
            formsPlot.Plot.ShowLegend();
            formsPlot.Refresh();
        }

        private void InitActionButtons()
        {
            if (btnIntake != null) btnIntake.Click += BtnIntake_Click;
            if (btnResolved != null) btnResolved.Click += BtnResolved_Click;
            if (btnRestart != null) btnRestart.Click += BtnRestart_Click;
        }

        private void BtnIntake_Click(object? sender, EventArgs e) { }
        private void BtnResolved_Click(object? sender, EventArgs e) { }
        private void BtnRestart_Click(object? sender, EventArgs e) { }

        public void UpdateDeviceSummary(int onlineCount, int offlineCount)
        {
            statOnline = onlineCount;
            statOffline = offlineCount;
            statTotal = onlineCount + offlineCount;

            if (pnlTotal != null) pnlTotal.Invalidate();
            if (pnlOnline != null) pnlOnline.Invalidate();
            if (pnlOffline != null) pnlOffline.Invalidate();
        }

        public void UpdateAlertSummary(int criticalCount, int warningCount)
        {
            statCritical = criticalCount;
            statWarning = warningCount;
            statAlerts = criticalCount + warningCount;

            if (pnlAlerts != null) pnlAlerts.Invalidate();
        }

        public void AddChartData(double newCpu, double newRam)
        {
            cpuData.Add(newCpu);
            ramData.Add(newRam);

            if (cpuData.Count > 15)
            {
                cpuData.RemoveAt(0);
                ramData.RemoveAt(0);
            }
            RenderScottPlot();
        }

        private void InitDeviceDetails()
        {
            pnlDeviceDetails.Visible = false;
            EnableDoubleBuffering(pnlDeviceDetails);
            pnlDeviceDetails.Paint += PnlDeviceDetails_Paint;

            if (pnlChartContainer != null && pnlChartContainer.Parent != null)
            {
                pnlChartContainer.Parent.Controls.Add(pnlDeviceDetails);
            }
            else
            {
                this.Controls.Add(pnlDeviceDetails);
            }

            pnlDeviceDetails.BringToFront();
        }

        private void PnlDeviceDetails_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = pnlDeviceDetails.ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0) return;

            using (GraphicsPath path = new GraphicsPath())
            {
                int r = 10;
                path.AddArc(1, 1, r, r, 180, 90);
                path.AddArc(rect.Width - r - 3, 1, r, r, 270, 90);
                path.AddArc(rect.Width - r - 3, rect.Height - r - 3, r, r, 0, 90);
                path.AddArc(1, rect.Height - r - 3, r, r, 90, 90);
                path.CloseFigure();
                using (Pen borderPen = new Pen(Color.FromArgb(50, 255, 255, 255), 1.5f))
                    g.DrawPath(borderPen, path);
            }

            using (Font titleFont = new Font("Segoe UI", 9, FontStyle.Bold))
            using (Font machineFont = new Font("Segoe UI", 16, FontStyle.Bold))
            using (Font infoFont = new Font("Segoe UI", 8, FontStyle.Regular))
            {
                g.DrawString("💻 Chi tiết thiết bị", titleFont, Brushes.LightSkyBlue, 15, 10);

                string statusText = "Đang online";
                SizeF statusSize = g.MeasureString(statusText, infoFont);
                float statusX = rect.Width - statusSize.Width - 15;
                g.DrawString(statusText, infoFont, Brushes.LimeGreen, statusX, 12);

                using (SolidBrush dotBrush = new SolidBrush(Color.LimeGreen))
                {
                    g.FillEllipse(dotBrush, statusX - 12, 16, 8, 8);
                }

                g.DrawString("MAY-03", machineFont, Brushes.White, 15, 32);
                g.DrawString("IP: 192.168.1.103   |   Phòng: Lab A   |   Win 11 Pro", infoFont, Brushes.LightGray, 15, 62);
            }

            int padding = 15;
            int cardW = (rect.Width - (padding * 3)) / 2;
            int cardH = 65;
            int startY = 90;

            DrawMiniCard(g, new Rectangle(padding, startY, cardW, cardH), "CPU", "92.4%", Color.FromArgb(235, 60, 60), 92.4f, "⚙");
            DrawMiniCard(g, new Rectangle(padding * 2 + cardW, startY, cardW, cardH), "RAM", "58.5%", Color.FromArgb(245, 160, 40), 58.5f, "💾");
            DrawMiniCard(g, new Rectangle(padding, startY + cardH + 10, cardW, cardH), "Disk (C:)", "71%", Color.FromArgb(245, 160, 40), 71f, "💽");
            DrawMiniCard(g, new Rectangle(padding * 2 + cardW, startY + cardH + 10, cardW, cardH), "Uptime", "3 ngày 14 giờ", Color.LightGray, -1, "🕒", "Từ 10/05 00:14");
        }

        private void DrawMiniCard(Graphics g, Rectangle rect, string title, string value, Color color, float pct, string icon, string sub = "")
        {
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(25, 30, 40)))
                g.FillRectangle(bg, rect);

            using (Pen p = new Pen(Color.FromArgb(45, 50, 60)))
                g.DrawRectangle(p, rect);

            using (Font fIcon = new Font("Segoe UI Emoji", 12))
            using (Font fTitle = new Font("Segoe UI", 7f))
            using (Font fVal = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (Font fSub = new Font("Segoe UI", 6.5f))
            using (SolidBrush valBrush = new SolidBrush(color))
            {
                int textX = rect.X + 38;

                g.DrawString(icon, fIcon, Brushes.LightGray, rect.X + 8, rect.Y + 14);
                g.DrawString(title, fTitle, Brushes.LightGray, textX, rect.Y + 8);
                g.DrawString(value, fVal, valBrush, textX - 2, rect.Y + 22);

                if (pct >= 0)
                {
                    int barW = rect.Width - textX - 8;
                    using (SolidBrush barBg = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                    {
                        g.FillRectangle(barBg, textX, rect.Bottom - 12, barW, 4);
                    }

                    using (SolidBrush barFill = new SolidBrush(color))
                    {
                        g.FillRectangle(barFill, textX, rect.Bottom - 12, barW * (pct / 100f), 4);
                    }
                }
                else if (!string.IsNullOrEmpty(sub))
                {
                    g.DrawString(sub, fSub, Brushes.Gray, textX, rect.Bottom - 18);
                }
            }
        }
    }
}