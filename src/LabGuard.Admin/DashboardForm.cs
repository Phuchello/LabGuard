using System.Data;
using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;

namespace LabGuard.Admin;

public class DashboardForm : Form
{
    private readonly AdminClient _adminClient;
    private readonly DataTable _deviceTable;
    private readonly DataTable _incidentTable;
    private readonly DataGridView _grid;
    private readonly DataGridView _incidentGrid;
    private readonly Label _lblTotalDevices;
    private readonly Label _lblOnlineDevices;
    private readonly Label _lblOfflineDevices;
    private readonly Label _lblActiveIncidents;
    private readonly ToolStripStatusLabel _lblConnectionStatus;
    private readonly System.Windows.Forms.Timer _uiRefreshTimer;
    private Button _btnAck = null!;
    private Button _btnAssign = null!;
    private Button _btnStart = null!;
    private Button _btnResolve = null!;
    private Button _btnRefreshIncidents = null!;

    public DashboardForm()
    {
        // 1. Form properties
        Text = "LabGuard — Hệ Thống Giám Sát & Quản Lý Sự Cố Phòng Máy (NT106)";
        Size = new Size(1020, 740);
        MinimumSize = new Size(850, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Color.FromArgb(248, 250, 252); // Slate 50 (#F8FAFC)

        _adminClient = new AdminClient();

        // 2. DataTables
        _deviceTable = new DataTable();
        _deviceTable.Columns.Add("DeviceId", typeof(string));
        _deviceTable.Columns.Add("Status", typeof(string));
        _deviceTable.Columns.Add("CpuPercent", typeof(string));
        _deviceTable.Columns.Add("MemoryPercent", typeof(string));
        _deviceTable.Columns.Add("Uptime", typeof(string));
        _deviceTable.Columns.Add("LastSeen", typeof(string));

        _incidentTable = new DataTable();
        _incidentTable.Columns.Add("IncidentId", typeof(long));
        _incidentTable.Columns.Add("DeviceId", typeof(string));
        _incidentTable.Columns.Add("RuleName", typeof(string));
        _incidentTable.Columns.Add("ConditionState", typeof(string));
        _incidentTable.Columns.Add("WorkflowState", typeof(string));
        _incidentTable.Columns.Add("OccurrenceCount", typeof(int));
        _incidentTable.Columns.Add("OpenedAt", typeof(string));
        _incidentTable.Columns.Add("RowVersionHex", typeof(string));

        // 3. Header Panel
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(15, 23, 42), // Slate 900
            Padding = new Padding(20, 10, 20, 10)
        };

        var lblTitle = new Label
        {
            Text = "LABGUARD",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 12)
        };

        var lblSubtitle = new Label
        {
            Text = "Hệ thống giám sát & quản lý sự cố phòng máy • NT106 UIT",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(148, 163, 184), // Slate 400
            AutoSize = true,
            Location = new Point(22, 42)
        };

        var lblUserBadge = new Label
        {
            Text = "👤 Admin: Quản Trị Viên",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(56, 189, 248), // Sky 400
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(780, 25)
        };

        headerPanel.Controls.Add(lblTitle);
        headerPanel.Controls.Add(lblSubtitle);
        headerPanel.Controls.Add(lblUserBadge);

        // 4. KPI Summary Cards Panel (4 columns)
        var cardsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 100,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(20, 10, 20, 10),
            BackColor = Color.Transparent
        };
        cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        var cardTotal = CreateCard("TỔNG SỐ THIẾT BỊ", "0", Color.FromArgb(37, 99, 235), out _lblTotalDevices);
        var cardOnline = CreateCard("ĐANG KẾT NỐI (ONLINE)", "0", Color.FromArgb(22, 163, 74), out _lblOnlineDevices);
        var cardOffline = CreateCard("MẤT TÍN HIỆU (OFFLINE)", "0", Color.FromArgb(220, 38, 38), out _lblOfflineDevices);
        var cardIncidents = CreateCard("SỰ CỐ ACTIVE", "0", Color.FromArgb(234, 88, 12), out _lblActiveIncidents);

        cardsPanel.Controls.Add(cardTotal, 0, 0);
        cardsPanel.Controls.Add(cardOnline, 1, 0);
        cardsPanel.Controls.Add(cardOffline, 2, 0);
        cardsPanel.Controls.Add(cardIncidents, 3, 0);

        // 5. Main Split Container (Devices on top, Incidents on bottom)
        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 260,
            SplitterWidth = 8,
            BackColor = Color.FromArgb(241, 245, 249),
            Padding = new Padding(20, 0, 20, 0)
        };

        // 5.1 Device Grid (Top Panel)
        var devicePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var deviceHeaderLabel = new Label
        {
            Text = "Danh Sách Máy Trạm & Hiệu Năng Thời Gian Thực",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Dock = DockStyle.Top,
            Height = 28
        };

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = _deviceTable,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            RowTemplate = { Height = 36 },
            GridColor = Color.FromArgb(241, 245, 249),
            EnableHeadersVisualStyles = false
        };
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _grid.ColumnHeadersHeight = 34;

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DeviceId", HeaderText = "MÃ MÁY TRẠM", Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "TRẠNG THÁI", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CpuPercent", HeaderText = "CPU (%)", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MemoryPercent", HeaderText = "RAM (%)", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Uptime", HeaderText = "UPTIME", Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LastSeen", HeaderText = "LẦN CUỐI THẤY", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.CellFormatting += Grid_CellFormatting;

        devicePanel.Controls.Add(_grid);
        devicePanel.Controls.Add(deviceHeaderLabel);
        splitContainer.Panel1.Controls.Add(devicePanel);

        // 5.2 Incident Grid (Bottom Panel)
        var incidentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var incidentHeaderLabel = new Label
        {
            Text = "Danh Sách Sự Cố Phát Hiện (Incidents)",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Dock = DockStyle.Top,
            Height = 28
        };

        var actionToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 4),
            AutoSize = false
        };

        _btnAck = CreateActionButton("Tiếp nhận (ACK)", Color.FromArgb(37, 99, 235), async (s, e) => await HandleAckClickedAsync());
        _btnAssign = CreateActionButton("Phân công (Assign)", Color.FromArgb(79, 70, 229), async (s, e) => await HandleAssignClickedAsync());
        _btnStart = CreateActionButton("Bắt đầu xử lý (Start)", Color.FromArgb(217, 119, 6), async (s, e) => await HandleStartClickedAsync());
        _btnResolve = CreateActionButton("Đóng sự cố (Resolve)", Color.FromArgb(16, 185, 129), async (s, e) => await HandleResolveClickedAsync());
        _btnRefreshIncidents = CreateActionButton("Làm mới", Color.FromArgb(100, 116, 139), async (s, e) => await _adminClient.RequestIncidentListAsync());

        _btnAck.Enabled = false;
        _btnAssign.Enabled = false;
        _btnStart.Enabled = false;
        _btnResolve.Enabled = false;

        actionToolbar.Controls.Add(_btnAck);
        actionToolbar.Controls.Add(_btnAssign);
        actionToolbar.Controls.Add(_btnStart);
        actionToolbar.Controls.Add(_btnResolve);
        actionToolbar.Controls.Add(_btnRefreshIncidents);

        _incidentGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = _incidentTable,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            RowTemplate = { Height = 36 },
            GridColor = Color.FromArgb(241, 245, 249),
            EnableHeadersVisualStyles = false
        };
        _incidentGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        _incidentGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(71, 85, 105);
        _incidentGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _incidentGrid.ColumnHeadersHeight = 34;

        _incidentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DeviceId", HeaderText = "MÃ MÁY TRẠM", Width = 130 });
        _incidentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RuleName", HeaderText = "LOẠI SỰ CỐ", Width = 140 });
        _incidentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ConditionState", HeaderText = "ĐIỀU KIỆN", Width = 110 });
        _incidentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "WorkflowState", HeaderText = "TIẾN TRÌNH", Width = 130 });
        _incidentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OccurrenceCount", HeaderText = "SỐ LẦN LẶP", Width = 90 });
        _incidentGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OpenedAt", HeaderText = "THỜI ĐIỂM PHÁT HIỆN", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _incidentGrid.CellFormatting += IncidentGrid_CellFormatting;
        _incidentGrid.SelectionChanged += (s, e) => UpdateActionButtonStates();

        incidentPanel.Controls.Add(_incidentGrid);
        incidentPanel.Controls.Add(actionToolbar);
        incidentPanel.Controls.Add(incidentHeaderLabel);
        splitContainer.Panel2.Controls.Add(incidentPanel);

        // 6. Status Strip
        var statusStrip = new StatusStrip
        {
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9F)
        };
        _lblConnectionStatus = new ToolStripStatusLabel
        {
            Text = "Đang khởi tạo kết nối Central Server...",
            ForeColor = Color.FromArgb(100, 116, 139)
        };
        statusStrip.Items.Add(_lblConnectionStatus);

        // Add controls to Form
        Controls.Add(splitContainer);
        Controls.Add(cardsPanel);
        Controls.Add(headerPanel);
        Controls.Add(statusStrip);

        // 7. Wire AdminClient events
        _adminClient.OnConnectionStatusChanged += msg =>
        {
            if (IsHandleCreated)
            {
                BeginInvoke(() => _lblConnectionStatus.Text = msg);
            }
        };

        _adminClient.OnDevicePageReceived += devices =>
        {
            if (IsHandleCreated)
            {
                BeginInvoke(() => UpdateDeviceList(devices));
            }
        };

        _adminClient.OnDeviceUpdated += update =>
        {
            if (IsHandleCreated)
            {
                BeginInvoke(() => UpdateSingleDevice(update));
            }
        };

        _adminClient.OnIncidentPageReceived += incidents =>
        {
            if (IsHandleCreated)
            {
                BeginInvoke(() => UpdateIncidentList(incidents));
            }
        };

        _adminClient.OnIncidentOpened += open =>
        {
            if (IsHandleCreated)
            {
                BeginInvoke(() => AddOrUpdateSingleIncident(open.IncidentId, open.DeviceId, open.RuleName, open.ConditionState, open.WorkflowState, 1, open.OpenedAtUtc, open.RowVersionHex));
            }
        };

        _adminClient.OnIncidentUpdated += upd =>
        {
            if (IsHandleCreated)
            {
                BeginInvoke(() => UpdateIncidentCondition(upd.IncidentId, upd.ConditionState, upd.WorkflowState, upd.RowVersionHex));
            }
        };

        // Periodic background refresh timer
        _uiRefreshTimer = new System.Windows.Forms.Timer { Interval = 4000 };
        _uiRefreshTimer.Tick += async (s, e) =>
        {
            if (_adminClient.IsConnected && _adminClient.IsAuthenticated)
            {
                await _adminClient.RequestDeviceListAsync().ConfigureAwait(false);
                await _adminClient.RequestIncidentListAsync().ConfigureAwait(false);
            }
        };

        Load += async (s, e) =>
        {
            await ConnectToCentralAsync();
            _uiRefreshTimer.Start();
        };

        FormClosing += async (s, e) =>
        {
            _uiRefreshTimer.Stop();
            await _adminClient.DisposeAsync();
        };
    }

    private static Panel CreateCard(string title, string initialValue, Color accentColor, out Label valueLabel)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(15, 10, 15, 10),
            Margin = new Padding(5)
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Top,
            Height = 18
        };

        var lblVal = new Label
        {
            Text = initialValue,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = accentColor,
            Dock = DockStyle.Fill
        };

        valueLabel = lblVal;
        panel.Controls.Add(lblVal);
        panel.Controls.Add(lblTitle);
        return panel;
    }

    private async Task ConnectToCentralAsync()
    {
        string host = Environment.GetEnvironmentVariable("LABGUARD_SERVER_HOST") ?? "127.0.0.1";
        int port = 5000;
        if (int.TryParse(Environment.GetEnvironmentVariable("LABGUARD_SERVER_PORT"), out int p))
        {
            port = p;
        }

        await _adminClient.ConnectAndLoginAsync(host, port, "admin", "Admin@123");
    }

    private void UpdateDeviceList(List<DeviceSummaryDto> devices)
    {
        int onlineCount = 0;
        int offlineCount = 0;

        foreach (var dev in devices)
        {
            bool isOnline = string.Equals(dev.Status, "ONLINE", StringComparison.OrdinalIgnoreCase);
            if (isOnline) onlineCount++;
            else offlineCount++;

            DataRow? row = FindDeviceRow(dev.DeviceId);
            if (row == null)
            {
                row = _deviceTable.NewRow();
                row["DeviceId"] = dev.DeviceId;
                _deviceTable.Rows.Add(row);
            }

            row["Status"] = dev.Status;
            row["CpuPercent"] = dev.CpuUsage.HasValue ? $"{dev.CpuUsage.Value:F1}%" : "--";
            row["MemoryPercent"] = dev.RamUsage.HasValue ? $"{dev.RamUsage.Value:F1}%" : "--";
            row["Uptime"] = dev.UptimeSeconds.HasValue ? FormatUptime(dev.UptimeSeconds.Value) : "--";
            row["LastSeen"] = FormatLastSeen(dev.LastSeenUtc);
        }

        _lblTotalDevices.Text = devices.Count.ToString();
        _lblOnlineDevices.Text = onlineCount.ToString();
        _lblOfflineDevices.Text = offlineCount.ToString();
    }

    private void UpdateSingleDevice(DeviceUpdatePayload update)
    {
        DataRow? row = FindDeviceRow(update.DeviceId);
        if (row == null)
        {
            row = _deviceTable.NewRow();
            row["DeviceId"] = update.DeviceId;
            _deviceTable.Rows.Add(row);
        }

        row["Status"] = update.Status;
        if (update.CpuUsage.HasValue)
        {
            row["CpuPercent"] = $"{update.CpuUsage.Value:F1}%";
        }
        if (update.RamUsage.HasValue)
        {
            row["MemoryPercent"] = $"{update.RamUsage.Value:F1}%";
        }
        if (update.UptimeSeconds.HasValue)
        {
            row["Uptime"] = FormatUptime(update.UptimeSeconds.Value);
        }
        row["LastSeen"] = DateTime.Now.ToString("HH:mm:ss");

        RecalculateDeviceKpis();
    }

    private void UpdateIncidentList(List<IncidentSummaryDto> incidents)
    {
        int activeCount = 0;
        foreach (var inc in incidents)
        {
            if (string.Equals(inc.ConditionState, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                activeCount++;
            }

            AddOrUpdateSingleIncident(
                inc.IncidentId,
                inc.DeviceId,
                inc.RuleName,
                inc.ConditionState,
                inc.WorkflowState,
                inc.OccurrenceCount,
                inc.OpenedAtUtc,
                inc.RowVersionHex);
        }

        _lblActiveIncidents.Text = activeCount.ToString();
        UpdateActionButtonStates();
    }

    private void AddOrUpdateSingleIncident(
        long incidentId,
        string deviceId,
        string ruleName,
        string conditionState,
        string workflowState,
        int occurrenceCount,
        string openedAtUtc,
        string rowVersionHex)
    {
        DataRow? row = FindIncidentRow(incidentId);
        if (row == null)
        {
            row = _incidentTable.NewRow();
            row["IncidentId"] = incidentId;
            row["DeviceId"] = deviceId;
            row["RuleName"] = ruleName;
            _incidentTable.Rows.Add(row);
        }

        row["ConditionState"] = conditionState;
        row["WorkflowState"] = workflowState;
        row["OccurrenceCount"] = occurrenceCount;
        row["OpenedAt"] = FormatLastSeen(openedAtUtc);
        row["RowVersionHex"] = rowVersionHex;

        RecalculateIncidentKpis();
        UpdateActionButtonStates();
    }

    private void UpdateIncidentCondition(long incidentId, string conditionState, string? workflowState = null, string? rowVersionHex = null)
    {
        DataRow? row = FindIncidentRow(incidentId);
        if (row != null)
        {
            row["ConditionState"] = conditionState;
            if (!string.IsNullOrEmpty(workflowState))
            {
                row["WorkflowState"] = workflowState;
            }
            if (!string.IsNullOrEmpty(rowVersionHex))
            {
                row["RowVersionHex"] = rowVersionHex;
            }
        }

        RecalculateIncidentKpis();
        UpdateActionButtonStates();
    }

    private void RecalculateDeviceKpis()
    {
        int total = _deviceTable.Rows.Count;
        int online = 0;
        int offline = 0;

        foreach (DataRow row in _deviceTable.Rows)
        {
            string status = row["Status"]?.ToString() ?? "";
            if (string.Equals(status, "ONLINE", StringComparison.OrdinalIgnoreCase))
            {
                online++;
            }
            else
            {
                offline++;
            }
        }

        _lblTotalDevices.Text = total.ToString();
        _lblOnlineDevices.Text = online.ToString();
        _lblOfflineDevices.Text = offline.ToString();
    }

    private void RecalculateIncidentKpis()
    {
        int active = 0;
        foreach (DataRow row in _incidentTable.Rows)
        {
            string state = row["ConditionState"]?.ToString() ?? "";
            if (string.Equals(state, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                active++;
            }
        }
        _lblActiveIncidents.Text = active.ToString();
    }

    private DataRow? FindDeviceRow(string deviceId)
    {
        foreach (DataRow row in _deviceTable.Rows)
        {
            if (string.Equals(row["DeviceId"]?.ToString(), deviceId, StringComparison.OrdinalIgnoreCase))
            {
                return row;
            }
        }
        return null;
    }

    private DataRow? FindIncidentRow(long incidentId)
    {
        foreach (DataRow row in _incidentTable.Rows)
        {
            if (row["IncidentId"] is long id && id == incidentId)
            {
                return row;
            }
        }
        return null;
    }

    private static string FormatUptime(long seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    private static string FormatLastSeen(string? isoUtc)
    {
        if (string.IsNullOrEmpty(isoUtc)) return "--";
        if (DateTime.TryParse(isoUtc, out var dt))
        {
            return dt.ToLocalTime().ToString("HH:mm:ss dd/MM");
        }
        return isoUtc;
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;

        if (_grid.Columns[e.ColumnIndex].DataPropertyName == "Status" && e.Value != null)
        {
            string status = e.Value.ToString() ?? "";
            if (string.Equals(status, "ONLINE", StringComparison.OrdinalIgnoreCase))
            {
                e.CellStyle!.ForeColor = Color.FromArgb(22, 163, 74);
                e.CellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            }
            else
            {
                e.CellStyle!.ForeColor = Color.FromArgb(220, 38, 38);
                e.CellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            }
        }
    }

    private void IncidentGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _incidentGrid.Rows.Count) return;

        var row = _incidentGrid.Rows[e.RowIndex];
        string cond = string.Empty;
        var dataRowView = row.DataBoundItem as DataRowView;
        if (dataRowView != null && dataRowView.Row.Table.Columns.Contains("ConditionState"))
        {
            cond = dataRowView.Row["ConditionState"]?.ToString() ?? "";
        }

        if (string.Equals(cond, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            row.DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242); // Soft red (#FEF2F2)
        }
        else
        {
            row.DefaultCellStyle.BackColor = Color.White;
        }

        string propName = _incidentGrid.Columns[e.ColumnIndex].DataPropertyName;

        if (propName == "ConditionState" && e.Value != null)
        {
            if (string.Equals(cond, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                e.CellStyle!.ForeColor = Color.FromArgb(220, 38, 38);
                e.CellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            }
            else
            {
                e.CellStyle!.ForeColor = Color.FromArgb(22, 163, 74);
                e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            }
        }
        else if (propName == "WorkflowState" && e.Value != null)
        {
            string wf = e.Value.ToString()?.ToUpperInvariant() ?? "";
            e.CellStyle!.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            switch (wf)
            {
                case "OPEN":
                    e.CellStyle.ForeColor = Color.FromArgb(180, 83, 9); // Amber
                    break;
                case "ACKNOWLEDGED":
                    e.CellStyle.ForeColor = Color.FromArgb(3, 105, 161); // Blue
                    break;
                case "IN_PROGRESS":
                    e.CellStyle.ForeColor = Color.FromArgb(79, 70, 229); // Indigo
                    break;
                case "RECOVERED":
                    e.CellStyle.ForeColor = Color.FromArgb(22, 163, 74); // Green
                    break;
                case "RESOLVED":
                    e.CellStyle.ForeColor = Color.FromArgb(107, 114, 128); // Gray
                    break;
            }
        }
    }

    private void UpdateActionButtonStates()
    {
        if (_incidentGrid.SelectedRows.Count == 0)
        {
            _btnAck.Enabled = false;
            _btnAssign.Enabled = false;
            _btnStart.Enabled = false;
            _btnResolve.Enabled = false;
            return;
        }

        var selectedRow = _incidentGrid.SelectedRows[0];
        var dataRowView = selectedRow.DataBoundItem as DataRowView;
        if (dataRowView == null)
        {
            _btnAck.Enabled = false;
            _btnAssign.Enabled = false;
            _btnStart.Enabled = false;
            _btnResolve.Enabled = false;
            return;
        }

        string wf = dataRowView.Row["WorkflowState"]?.ToString()?.ToUpperInvariant() ?? "OPEN";
        string cond = dataRowView.Row["ConditionState"]?.ToString()?.ToUpperInvariant() ?? "ACTIVE";

        _btnAck.Enabled = (wf == "OPEN");
        _btnAssign.Enabled = (wf == "OPEN" || wf == "ACKNOWLEDGED" || wf == "IN_PROGRESS");
        _btnStart.Enabled = (wf == "OPEN" || wf == "ACKNOWLEDGED");
        _btnResolve.Enabled = (wf != "RESOLVED" && cond == "RECOVERED");
    }

    private async Task HandleAckClickedAsync()
    {
        await ExecuteSelectedIncidentCommandAsync(MessageTypes.IncidentAck, null);
    }

    private async Task HandleAssignClickedAsync()
    {
        string? input = PromptDialog.Show("Phân Công Xử Lý Sự Cố", "Nhập User ID kỹ thuật viên chịu trách nhiệm xử lý:", "1");
        if (string.IsNullOrWhiteSpace(input)) return;

        if (!int.TryParse(input.Trim(), out int userId) || userId <= 0)
        {
            MessageBox.Show(this, "User ID phải là số nguyên dương hợp lệ.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await ExecuteSelectedIncidentCommandAsync(MessageTypes.IncidentAssign, new Dictionary<string, object?>
        {
            ["assignedToUserId"] = userId
        });
    }

    private async Task HandleStartClickedAsync()
    {
        await ExecuteSelectedIncidentCommandAsync(MessageTypes.IncidentStart, null);
    }

    private async Task HandleResolveClickedAsync()
    {
        string? note = PromptDialog.Show("Đóng Sự Cố (Resolve)", "Nhập ghi chú kết quả xử lý (bắt buộc):", "Đã kiểm tra và hoàn tất khắc phục sự cố.");
        if (string.IsNullOrWhiteSpace(note)) return;

        await ExecuteSelectedIncidentCommandAsync(MessageTypes.IncidentResolve, new Dictionary<string, object?>
        {
            ["resolutionNote"] = note.Trim()
        });
    }

    private async Task ExecuteSelectedIncidentCommandAsync(string cmdType, Dictionary<string, object?>? parameters)
    {
        if (_incidentGrid.SelectedRows.Count == 0) return;
        var dataRowView = _incidentGrid.SelectedRows[0].DataBoundItem as DataRowView;
        if (dataRowView == null) return;

        long incidentId = Convert.ToInt64(dataRowView.Row["IncidentId"]);
        string expectedRv = dataRowView.Row["RowVersionHex"]?.ToString() ?? string.Empty;

        _btnAck.Enabled = false;
        _btnAssign.Enabled = false;
        _btnStart.Enabled = false;
        _btnResolve.Enabled = false;

        _lblConnectionStatus.Text = $"Đang gửi lệnh {cmdType} cho sự cố #{incidentId}...";

        var result = await _adminClient.SendIncidentCommandAsync(cmdType, incidentId, expectedRv, parameters);

        if (result.Success)
        {
            _lblConnectionStatus.Text = $"Thực hiện {cmdType} trên sự cố #{incidentId} thành công!";
            await _adminClient.RequestIncidentListAsync();
        }
        else if (result.StatusCode == 409)
        {
            MessageBox.Show(this,
                "Sự cố đã được thay đổi bởi quản trị viên khác hoặc dữ liệu chưa được cập nhật.\nHệ thống đang làm mới danh sách sự cố...",
                "Xung Đột Đồng Thời (OCC Conflict)",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            await _adminClient.RequestIncidentListAsync();
        }
        else
        {
            MessageBox.Show(this,
                $"Thao tác thất bại: {result.ErrorMessage ?? result.ErrorCode ?? "Lỗi không xác định"}",
                "Lỗi Thực Thi Lệnh",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            await _adminClient.RequestIncidentListAsync();
        }
    }

    private static Button CreateActionButton(string text, Color activeColor, EventHandler onClick)
    {
        var btn = new Button
        {
            Text = text,
            BackColor = activeColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Height = 32,
            AutoSize = true,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0)
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += onClick;
        btn.EnabledChanged += (s, e) =>
        {
            if (btn.Enabled)
            {
                btn.BackColor = activeColor;
                btn.ForeColor = Color.White;
                btn.Cursor = Cursors.Hand;
            }
            else
            {
                btn.BackColor = Color.FromArgb(226, 232, 240); // Slate 200
                btn.ForeColor = Color.FromArgb(148, 163, 184); // Slate 400
                btn.Cursor = Cursors.Default;
            }
        };
        return btn;
    }
}

public static class PromptDialog
{
    public static string? Show(string title, string promptText, string defaultValue = "")
    {
        using var form = new Form
        {
            Width = 460,
            Height = 180,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            Font = new Font("Segoe UI", 9.5F)
        };

        var lbl = new Label { Left = 20, Top = 16, Width = 400, Text = promptText, AutoSize = true };
        var txt = new TextBox { Left = 20, Top = 48, Width = 400, Text = defaultValue };
        var btnOk = new Button { Text = "Xác nhận", Left = 230, Width = 90, Top = 90, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Hủy", Left = 330, Width = 90, Top = 90, DialogResult = DialogResult.Cancel };

        form.Controls.Add(lbl);
        form.Controls.Add(txt);
        form.Controls.Add(btnOk);
        form.Controls.Add(btnCancel);
        form.AcceptButton = btnOk;
        form.CancelButton = btnCancel;

        return form.ShowDialog() == DialogResult.OK ? txt.Text : null;
    }
}
