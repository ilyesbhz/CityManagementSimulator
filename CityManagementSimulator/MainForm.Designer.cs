using CityManagementSimulator.Models;
using CityManagementSimulator.Repositories;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace CityManagementSimulator
{
    public partial class MainForm : Form
    {
        // Repos
        private readonly BuildingRepository _buildingRepo = new BuildingRepository();
        private readonly PersonRepository _personRepo = new PersonRepository();
        private readonly CityRepository _cityRepo = new CityRepository();

        // UI
        private Panel leftPanel;
        private FlowLayoutPanel toolbox;
        private Panel topPanel;
        private Panel mapContainer;    // scrollable container
        private Panel mapPanel;        // drawing surface (large)
        private TableLayoutPanel rightPanel;
        private Panel infoPanel;      // top right info (we will show building info here)
        private Chart chart;
        private Label lblBudget, lblEnergy, lblWater, lblPopulation, lblPollution, lblHappiness, lblDay;
        private Button btnNextDay, btnAddCitizen, btnAssignCitizen, btnAssignAll;
        private ComboBox comboTileSize;

        // map & visuals
        private readonly int tileSizeDefault = 80;
        private int tileSize;
        private int gridWidth = 500;  // large grid => "infinite-like"
        private int gridHeight = 500;
        private Dictionary<int, Control> buildingControls = new Dictionary<int, Control>(); // buildingId -> control

        // toolbox definitions
        private Dictionary<string, Building> definitions;

        // state
        private string selectedTool = null;

        // Dragging support
        private bool isDragging = false;
        private Control draggingControl = null;
        private Point dragStartMouse; // absolute mouse position at drag start
        private Point dragStartControlLocation; // control.Location at drag start

        // Right panel selected building
        private int? selectedBuildingIdOnRight = null;

        public MainForm()
        {
            Text = "City Management Simulator";
            Width = 1280;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            tileSize = tileSizeDefault;
            InitDefinitions();
            InitializeUI();

            LoadState();
            RenderAllBuildings();
            RefreshDashboard();
            RefreshChart();
        }

        private void InitDefinitions()
        {
            definitions = new Dictionary<string, Building>();
            definitions["House"] = new Building { Name = "House", Type = "House", CapacityPopulation = 5, Cost = 200, Income = 0, EnergyConsumption = 2, WaterConsumption = 3, Pollution = 0.5 };
            definitions["Factory"] = new Building { Name = "Factory", Type = "Factory", CapacityPopulation = 0, Cost = 800, Income = 50, EnergyConsumption = 20, WaterConsumption = 10, Pollution = 15 };
            definitions["Park"] = new Building { Name = "Park", Type = "Park", CapacityPopulation = 0, Cost = 300, Income = 0, EnergyConsumption = 1, WaterConsumption = 1, Pollution = -5 };
            definitions["Commerce"] = new Building { Name = "Commerce", Type = "Commerce", CapacityPopulation = 3, Cost = 400, Income = 10, EnergyConsumption = 5, WaterConsumption = 3, Pollution = 3 };
        }

        private void InitializeUI()
        {
            // Left toolbox
            leftPanel = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = Color.FromArgb(240, 248, 255) };
            Controls.Add(leftPanel);

            var title = new Label
            {
                Text = "Toolbox - Buildings",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            leftPanel.Controls.Add(title);

            // Make toolbox occupy fixed area so it doesn't overlay other controls
            toolbox = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 300, AutoScroll = true, Padding = new Padding(8) };
            leftPanel.Controls.Add(toolbox);

            foreach (var d in definitions)
            {
                var btn = new Button
                {
                    Text = d.Key,
                    Tag = d.Key,
                    Width = 180,
                    Height = 44,
                    BackColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9)
                };
                btn.FlatAppearance.BorderColor = Color.LightGray;
                btn.Click += (s, e) =>
                {
                    selectedTool = (string)((Button)s).Tag;
                    HighlightTool((Button)s);
                };
                toolbox.Controls.Add(btn);
            }

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 140, Padding = new Padding(8) };
            leftPanel.Controls.Add(btnPanel);

            btnAddCitizen = new Button { Text = "Add Citizen", Width = 180, Height = 36 };
            btnAddCitizen.Click += BtnAddCitizen_Click;
            btnPanel.Controls.Add(btnAddCitizen);

            btnAssignCitizen = new Button { Text = "Assign Citizen", Width = 180, Height = 36 };
            btnAssignCitizen.Click += BtnAssignCitizen_Click;
            btnPanel.Controls.Add(btnAssignCitizen);

            btnAssignAll = new Button { Text = "Assign All", Width = 180, Height = 36 };
            btnAssignAll.Click += BtnAssignAll_Click;
            btnPanel.Controls.Add(btnAssignAll);

            // center top: controls
            topPanel = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.WhiteSmoke };
            Controls.Add(topPanel);

            btnNextDay = new Button { Text = "Next Day", Left = 12, Width = 100, Height = 30, Top = 8 };
            btnNextDay.Click += BtnNextDay_Click;
            topPanel.Controls.Add(btnNextDay);

            comboTileSize = new ComboBox { Left = 130, Top = 10, Width = 80 };
            comboTileSize.Items.AddRange(new object[] { "40", "60", "80", "100" });
            comboTileSize.SelectedItem = tileSize.ToString();
            comboTileSize.SelectedIndexChanged += (s, e) =>
            {
                if (int.TryParse(comboTileSize.SelectedItem.ToString(), out int ts)) { tileSize = ts; ResizeMapSurface(); RenderAllBuildings(); }
            };
            topPanel.Controls.Add(comboTileSize);

            // map container (scrollable)
            mapContainer = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
            Controls.Add(mapContainer);

            mapPanel = new Panel { BackColor = Color.White };
            ResizeMapSurface();
            mapPanel.Paint += MapPanel_Paint;
            mapPanel.MouseClick += MapPanel_MouseClick;
            mapContainer.Controls.Add(mapPanel);

            // Right panel: dashboard + chart (we'll add building info box here)
            rightPanel = new TableLayoutPanel { Dock = DockStyle.Right, Width = 360, ColumnCount = 1, RowCount = 3, BackColor = Color.WhiteSmoke };
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 180)); // info area (will show building info or general)
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 160)); // general stats
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // chart
            Controls.Add(rightPanel);

            // Top-right: dynamic building info panel (initially shows summary header)
            infoPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            rightPanel.Controls.Add(infoPanel, 0, 0);

            var header = new Label { Text = "Selected Building", Dock = DockStyle.Top, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            infoPanel.Controls.Add(header);

            // Middle-right: general stats
            var statsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            rightPanel.Controls.Add(statsPanel, 0, 1);

            lblDay = new Label { Text = "Day: 0", Dock = DockStyle.Top, Font = new Font("Segoe UI", 9) };
            statsPanel.Controls.Add(lblDay);

            lblBudget = new Label { Text = "Budget: 0", Dock = DockStyle.Top, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            statsPanel.Controls.Add(lblBudget);

            lblPopulation = new Label { Text = "Population: 0", Dock = DockStyle.Top };
            statsPanel.Controls.Add(lblPopulation);

            lblPollution = new Label { Text = "Pollution: 0", Dock = DockStyle.Top };
            statsPanel.Controls.Add(lblPollution);

            lblEnergy = new Label { Text = "Energy pool: 0", Dock = DockStyle.Top };
            statsPanel.Controls.Add(lblEnergy);

            lblWater = new Label { Text = "Water pool: 0", Dock = DockStyle.Top };
            statsPanel.Controls.Add(lblWater);

            lblHappiness = new Label { Text = "Happiness: 0", Dock = DockStyle.Top };
            statsPanel.Controls.Add(lblHappiness);

            // Chart
            chart = new Chart { Dock = DockStyle.Fill, BackColor = Color.White };
            var area = new ChartArea("area") { BackColor = Color.WhiteSmoke };
            chart.ChartAreas.Add(area);
            chart.Legends.Add(new Legend());
            chart.Series.Add(new Series("Population") { ChartType = SeriesChartType.Line, BorderWidth = 3 });
            chart.Series.Add(new Series("Budget") { ChartType = SeriesChartType.Line, YAxisType = AxisType.Secondary, BorderWidth = 2 });
            rightPanel.Controls.Add(chart, 0, 2);
        }

        private void HighlightTool(Button btn)
        {
            foreach (Control c in toolbox.Controls) if (c is Button b) b.BackColor = Color.White;
            btn.BackColor = Color.LightBlue;
        }

        private void ResizeMapSurface()
        {
            int w = gridWidth * tileSize;
            int h = gridHeight * tileSize;
            mapPanel.Size = new Size(w, h);
        }

        // Draw visible grid lines and building controls (building controls are separate)
        private void MapPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var clip = e.ClipRectangle;
            var offset = mapContainer.AutoScrollPosition;
            int startCol = Math.Max(0, (clip.Left - offset.X) / tileSize);
            int endCol = Math.Min(gridWidth - 1, (clip.Right - offset.X) / tileSize + 1);
            int startRow = Math.Max(0, (clip.Top - offset.Y) / tileSize);
            int endRow = Math.Min(gridHeight - 1, (clip.Bottom - offset.Y) / tileSize + 1);

            using (var pen = new Pen(Color.LightGray))
            {
                for (int c = startCol; c <= endCol; c++)
                {
                    int x = c * tileSize;
                    g.DrawLine(pen, x, startRow * tileSize, x, (endRow + 1) * tileSize);
                }
                for (int r = startRow; r <= endRow; r++)
                {
                    int y = r * tileSize;
                    g.DrawLine(pen, startCol * tileSize, y, (endCol + 1) * tileSize, y);
                }
            }

            using (var f = new Font("Segoe UI", 7))
            using (var br = new SolidBrush(Color.Gray))
            {
                for (int c = startCol; c <= endCol; c++)
                    for (int r = startRow; r <= endRow; r++)
                    {
                        int x = c * tileSize + 2;
                        int y = r * tileSize + 2;
                        g.DrawString($"({c},{r})", f, br, x, y);
                    }
            }
        }

        private void MapPanel_MouseClick(object sender, MouseEventArgs e)
        {
            int cellX = e.X / tileSize;
            int cellY = e.Y / tileSize;

            // Check if a building already exists on this tile
            var buildingOnTile = _buildingRepo.GetAll()
                .FirstOrDefault(building => building.CellX == cellX && building.CellY == cellY);

            // === CLICK ON EXISTING BUILDING ===
            if (buildingOnTile != null)
            {
                ShowBuildingOnRight(buildingOnTile.Id);
                return;
            }

            // === LEFT CLICK on empty tile → create building based on selected TOOL ===
            if (e.Button == MouseButtons.Left)
            {
                if (string.IsNullOrEmpty(selectedTool) || !definitions.ContainsKey(selectedTool))
                {
                    MessageBox.Show("Select House, Factory, Park or Commerce first.");
                    return;
                }

                // Extract building definition
                var def = definitions[selectedTool];

                var newBuilding = new Building
                {
                    Name = def.Name,
                    Type = def.Type,
                    CapacityPopulation = def.CapacityPopulation,
                    Cost = def.Cost,
                    Income = def.Income,
                    EnergyConsumption = def.EnergyConsumption,
                    WaterConsumption = def.WaterConsumption,
                    Pollution = def.Pollution,
                    CellX = cellX,
                    CellY = cellY
                };

                int id = _buildingRepo.Add(newBuilding);
                newBuilding.Id = id;

                RenderAllBuildings();
                ShowBuildingOnRight(id);
                return;
            }

            // === RIGHT CLICK → context menu for empty tile ===
            if (e.Button == MouseButtons.Right)
            {
                var cm = new ContextMenuStrip();

                // add building types to menu
                foreach (var kv in definitions)
                {
                    var item = cm.Items.Add("Add " + kv.Key);
                    item.Name = kv.Key;
                }

                cm.ItemClicked += (ss, ee2) =>
                {
                    string type = ee2.ClickedItem.Name;

                    if (!definitions.ContainsKey(type)) return;

                    var def = definitions[type];

                    var newBuilding = new Building
                    {
                        Name = def.Name,
                        Type = def.Type,
                        CapacityPopulation = def.CapacityPopulation,
                        Cost = def.Cost,
                        Income = def.Income,
                        EnergyConsumption = def.EnergyConsumption,
                        WaterConsumption = def.WaterConsumption,
                        Pollution = def.Pollution,
                        CellX = cellX,
                        CellY = cellY
                    };

                    int id = _buildingRepo.Add(newBuilding);
                    newBuilding.Id = id;

                    RenderAllBuildings();
                    ShowBuildingOnRight(id);
                };

                cm.Show(System.Windows.Forms.Cursor.Position);

            }
        }




        private void RenderAllBuildings()
        {
            foreach (var kv in buildingControls) { var c = kv.Value; if (mapPanel.Controls.Contains(c)) mapPanel.Controls.Remove(c); }
            buildingControls.Clear();

            var buildings = _buildingRepo.GetAll();
            foreach (var b in buildings)
            {
                if (b.CellX.HasValue && b.CellY.HasValue)
                    RenderBuilding(b, b.Id);
            }
            mapPanel.Invalidate();
        }

        private void RenderBuilding(Building b, int id)
        {
            if (!b.CellX.HasValue || !b.CellY.HasValue) return;

            // remove existing control if exists
            if (buildingControls.ContainsKey(id))
            {
                var old = buildingControls[id];
                if (mapPanel.Controls.Contains(old)) mapPanel.Controls.Remove(old);
                buildingControls.Remove(id);
            }

            var ctrl = new Panel
            {
                Size = new Size(tileSize - 4, tileSize - 4),
                Location = new Point(b.CellX.Value * tileSize + 2, b.CellY.Value * tileSize + 2),
                BackColor = GetColorByType(b.Type),
                Tag = id,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lbl = new Label { Text = $"{b.Type}\n{b.Name}", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8) };
            ctrl.Controls.Add(lbl);

            // occupant badge (top-right)
            var badge = new Label
            {
                Text = GetOccupantCountForBadge(id).ToString(),
                AutoSize = false,
                Size = new Size(28, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(255, 255, 200),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            badge.Location = new Point(ctrl.Width - badge.Width - 2, 2);
            badge.Name = "badge";
            ctrl.Controls.Add(badge);
            badge.BringToFront();

            // tooltips
            var tip = new ToolTip();
            tip.SetToolTip(ctrl, $"{b.Name} ({b.Type})");

            // mouse events for context menu, left click (show in right panel), and drag
            ctrl.MouseDown += BuildingControl_MouseDown;
            ctrl.MouseMove += BuildingControl_MouseMove;
            ctrl.MouseUp += BuildingControl_MouseUp;

            // also attach label events (so clicking label acts like clicking panel)
            lbl.MouseDown += (s, e) => BuildingControl_MouseDown(ctrl, e);
            lbl.MouseMove += (s, e) => BuildingControl_MouseMove(ctrl, e);
            lbl.MouseUp += (s, e) => BuildingControl_MouseUp(ctrl, e);
            badge.MouseDown += (s, e) => BuildingControl_MouseDown(ctrl, e);
            badge.MouseMove += (s, e) => BuildingControl_MouseMove(ctrl, e);
            badge.MouseUp += (s, e) => BuildingControl_MouseUp(ctrl, e);

            mapPanel.Controls.Add(ctrl);
            buildingControls[id] = ctrl;
        }

        private int GetOccupantCountForBadge(int buildingId)
        {
            return _personRepo.GetAll().Count(p => p.BuildingId == buildingId);
        }

        private Color GetColorByType(string type)
        {
            switch (type)
            {
                case "House": return Color.FromArgb(173, 216, 230);
                case "Factory": return Color.FromArgb(240, 128, 128);
                case "Park": return Color.FromArgb(144, 238, 144);
                case "Commerce": return Color.FromArgb(255, 228, 181);
                default: return Color.LightGray;
            }
        }

        private void BuildingControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (!(sender is Control ctrl)) return;
            if (!(ctrl.Tag is int bid)) return;

            var building = _buildingRepo.GetAll().FirstOrDefault(x => x.Id == bid);
            if (building == null) return;

            if (e.Button == MouseButtons.Right)
            {
                var cm = new ContextMenuStrip();
                cm.Items.Add("Edit").Name = "edit";
                cm.Items.Add("Delete").Name = "delete";
                cm.Items.Add("Show occupants").Name = "occupants";
                cm.ItemClicked += (ss, ee) =>
                {
                    if (ee.ClickedItem.Name == "edit") EditBuilding(bid);
                    else if (ee.ClickedItem.Name == "delete") DeleteBuilding(bid);
                    else if (ee.ClickedItem.Name == "occupants") ShowOccupants(bid);
                };
                cm.Show(System.Windows.Forms.Cursor.Position);
                return;
            }

            // left button => select or start drag
            if (e.Button == MouseButtons.Left)
            {
                // record drag start
                isDragging = true;
                draggingControl = ctrl;
                dragStartMouse = System.Windows.Forms.Cursor.Position; // <-- fully qualified
                dragStartControlLocation = ctrl.Location;
            }
        }



        private void BuildingControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || draggingControl == null) return;

            // compute delta in screen coords then in mapPanel coordinates
            var curMouse = System.Windows.Forms.Cursor.Position; // <-- fully qualified
            var delta = new Point(curMouse.X - dragStartMouse.X, curMouse.Y - dragStartMouse.Y);

            // Move control visually with the mouse (limited by map size)
            var newLoc = new Point(dragStartControlLocation.X + delta.X, dragStartControlLocation.Y + delta.Y);

            newLoc.X = Math.Max(2, Math.Min(mapPanel.Width - draggingControl.Width - 2, newLoc.X));
            newLoc.Y = Math.Max(2, Math.Min(mapPanel.Height - draggingControl.Height - 2, newLoc.Y));

            draggingControl.Location = newLoc;
            draggingControl.BringToFront();
        }

        private void BuildingControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (!(sender is Control ctrl)) return;
            if (!(ctrl.Tag is int bid)) return;

            if (isDragging && draggingControl == ctrl)
            {
                // Determine end cell coordinates based on control.Location
                int newCellX = Math.Max(0, (draggingControl.Location.X - 2 + tileSize / 2) / tileSize);
                int newCellY = Math.Max(0, (draggingControl.Location.Y - 2 + tileSize / 2) / tileSize);

                // Check occupancy at target (if another building occupies the same tile)
                var other = _buildingRepo.GetAll().FirstOrDefault(b => b.Id != bid && b.CellX == newCellX && b.CellY == newCellY);
                if (other != null)
                {
                    // revert to old location and show message
                    var oldBuilding = _buildingRepo.GetAll().FirstOrDefault(b => b.Id == bid);
                    if (oldBuilding != null && oldBuilding.CellX.HasValue && oldBuilding.CellY.HasValue)
                    {
                        draggingControl.Location = new Point(oldBuilding.CellX.Value * tileSize + 2, oldBuilding.CellY.Value * tileSize + 2);
                    }
                    MessageBox.Show("Cannot move: target tile is occupied.", "Move failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    // update building position in repo
                    var building = _buildingRepo.GetAll().FirstOrDefault(x => x.Id == bid);
                    if (building != null)
                    {
                        building.CellX = newCellX;
                        building.CellY = newCellY;
                        _buildingRepo.Update(building);
                        // snap control to tile exactly
                        draggingControl.Location = new Point(newCellX * tileSize + 2, newCellY * tileSize + 2);
                    }
                }

                // end drag
                isDragging = false;
                draggingControl = null;
            }
            else
            {
                // This was a click (no drag) - show building info in right panel
                ShowBuildingOnRight(bid);
            }
        }


        private void ShowBuildingOnRight(int id)
        {
            var b = _buildingRepo.GetAll().FirstOrDefault(x => x.Id == id);
            if (b == null) return;
            selectedBuildingIdOnRight = id;

            infoPanel.Controls.Clear();

            var header = new Label { Text = $"{b.Name} ({b.Type})", Dock = DockStyle.Top, Font = new Font("Segoe UI", 10, FontStyle.Bold), Height = 28 };
            infoPanel.Controls.Add(header);

            var lblCap = new Label { Text = $"Capacity: {b.CapacityPopulation}", Dock = DockStyle.Top };
            infoPanel.Controls.Add(lblCap);

            var occupants = _personRepo.GetAll().Where(p => p.BuildingId == b.Id).ToList();
            var lblOcc = new Label { Text = $"Occupants: {occupants.Count}", Dock = DockStyle.Top };
            infoPanel.Controls.Add(lblOcc);

            var list = new ListBox { Dock = DockStyle.Fill };
            foreach (var p in occupants) list.Items.Add($"{p.FullName} (Age {p.Age})");
            infoPanel.Controls.Add(list);

            var pnlButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.RightToLeft };
            var btnDelete = new Button { Text = "Delete", BackColor = Color.IndianRed, ForeColor = Color.White, Width = 90 };
            btnDelete.Click += (s, e) => { DeleteBuilding(id); ClearRightInfo(); };
            var btnEdit = new Button { Text = "Edit", Width = 90 };
            btnEdit.Click += (s, e) => { EditBuilding(id); ShowBuildingOnRight(id); };
            var btnAddCitizenToThis = new Button { Text = "Add Citizens", Width = 100 };
            btnAddCitizenToThis.Click += (s, e) => { AddCitizenToBuildingDialog(id); ShowBuildingOnRight(id); };

            pnlButtons.Controls.Add(btnDelete);
            pnlButtons.Controls.Add(btnEdit);
            pnlButtons.Controls.Add(btnAddCitizenToThis);
            infoPanel.Controls.Add(pnlButtons);
        }

        private void ClearRightInfo()
        {
            selectedBuildingIdOnRight = null;
            infoPanel.Controls.Clear();
            var header = new Label { Text = "Selected Building", Dock = DockStyle.Top, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            infoPanel.Controls.Add(header);
        }

        private void AddCitizenToBuildingDialog(int buildingId)
        {
            var b = _buildingRepo.GetAll().FirstOrDefault(x => x.Id == buildingId);
            if (b == null) return;

            var unassigned = _personRepo.GetAll().Where(p => p.BuildingId == null).ToList();
            if (!unassigned.Any()) { MessageBox.Show("No unassigned citizens available."); return; }

            int current = _personRepo.GetAll().Count(p => p.BuildingId == buildingId);
            int capacityLeft = Math.Max(0, b.CapacityPopulation - current);
            if (capacityLeft <= 0) { MessageBox.Show("No capacity left in this building."); return; }

            // Ask how many to add (up to capacityLeft and unassigned.Count)
            var prompt = new Form { Width = 300, Height = 160, StartPosition = FormStartPosition.CenterParent, Text = "Add Citizens" };
            var lbl = new Label { Text = $"Add how many? (max {Math.Min(capacityLeft, unassigned.Count)})", Top = 10, Left = 10, Width = 260 };
            var num = new NumericUpDown { Top = 40, Left = 10, Width = 120, Minimum = 1, Maximum = Math.Min(capacityLeft, unassigned.Count), Value = 1 };
            var ok = new Button { Text = "OK", Top = 80, Left = 10, Width = 80 };
            var cancel = new Button { Text = "Cancel", Top = 80, Left = 100, Width = 80 };
            ok.Click += (s, e) => { prompt.DialogResult = DialogResult.OK; prompt.Close(); };
            cancel.Click += (s, e) => { prompt.DialogResult = DialogResult.Cancel; prompt.Close(); };
            prompt.Controls.AddRange(new Control[] { lbl, num, ok, cancel });
            if (prompt.ShowDialog() == DialogResult.OK)
            {
                int toAdd = (int)num.Value;
                var toAssign = unassigned.Take(toAdd).ToList();
                foreach (var p in toAssign)
                {
                    p.BuildingId = b.Id;
                    _personRepo.Update(p);
                }
                RefreshDashboard();
                RefreshChart();
                RenderAllBuildings(); // update badges
            }
        }

        private void DeleteBuilding(int id)
        {
            var confirm = MessageBox.Show("Delete this building? Occupants will be unassigned.", "Confirm", MessageBoxButtons.YesNo);
            if (confirm != DialogResult.Yes) return;
            var persons = _personRepo.GetAll().Where(p => p.BuildingId == id).ToList();
            foreach (var p in persons) { p.BuildingId = null; _personRepo.Update(p); }
            _buildingRepo.Delete(id);
            if (buildingControls.ContainsKey(id)) { mapPanel.Controls.Remove(buildingControls[id]); buildingControls.Remove(id); }
            RefreshDashboard(); RefreshChart();
            ClearRightInfo();
        }

        private void EditBuilding(int id)
        {
            var b = _buildingRepo.GetAll().FirstOrDefault(x => x.Id == id);
            if (b == null) return;
            var dlg = new EditBuildingDialog(b);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                b.Name = dlg.BuildingName; b.CapacityPopulation = dlg.Capacity;
                b.Cost = dlg.Cost; b.EnergyConsumption = dlg.Energy; b.WaterConsumption = dlg.Water;
                _buildingRepo.Update(b);
                RenderAllBuildings(); RefreshDashboard(); RefreshChart();
            }
        }

        private void ShowOccupants(int id)
        {
            var persons = _personRepo.GetAll().Where(p => p.BuildingId == id).ToList();
            string msg = persons.Count == 0 ? "No occupants." : string.Join("\n", persons.Select(p => p.FullName));
            MessageBox.Show(msg, "Occupants");
        }

        private void BtnAddCitizen_Click(object sender, EventArgs e)
        {
            var dlg = new AddPersonDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var p = new Person { FullName = dlg.PersonName, Age = dlg.PersonAge };
                _personRepo.Add(p);
                RefreshDashboard(); RefreshChart();
                RenderAllBuildings(); // update badges
            }
        }

        private void BtnAssignCitizen_Click(object sender, EventArgs e)
        {
            var persons = _personRepo.GetAll();
            if (persons.Count == 0) { MessageBox.Show("No citizens. Add first."); return; }
            var buildings = _buildingRepo.GetAll();
            if (buildings.Count == 0) { MessageBox.Show("No buildings. Place a building first."); return; }

            var choose = new AssignDialog(persons, buildings);
            if (choose.ShowDialog() == DialogResult.OK)
            {
                var selectedPerson = choose.SelectedPerson;
                var selectedBuilding = choose.SelectedBuilding;

                int currentOcc = _personRepo.GetAll().Count(p => p.BuildingId == selectedBuilding.Id);
                if (selectedBuilding.CapacityPopulation > 0 && currentOcc >= selectedBuilding.CapacityPopulation)
                {
                    MessageBox.Show("Building is full.", "Assign failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                selectedPerson.BuildingId = selectedBuilding.Id;
                _personRepo.Update(selectedPerson);
                RefreshDashboard(); RefreshChart();
                RenderAllBuildings(); // update badge
            }
        }

        private void BtnAssignAll_Click(object sender, EventArgs e)
        {
            var buildings = _buildingRepo.GetAll();
            if (!buildings.Any()) { MessageBox.Show("No buildings available."); return; }

            var dlg = new SelectBuildingDialog(buildings);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var b = dlg.SelectedBuilding;
                var free = _personRepo.GetAll().Where(p => p.BuildingId == null).ToList();

                if (free.Count == 0) { MessageBox.Show("No unassigned citizens to assign."); return; }

                int currentOcc = _personRepo.GetAll().Count(p => p.BuildingId == b.Id);
                int capacityLeft = b.CapacityPopulation > 0 ? Math.Max(0, b.CapacityPopulation - currentOcc) : int.MaxValue;

                if (capacityLeft == 0)
                {
                    MessageBox.Show("Selected building has no capacity left.", "Assign all", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int assignCount = Math.Min(capacityLeft == int.MaxValue ? free.Count : capacityLeft, free.Count);
                var toAssign = free.Take(assignCount).ToList();
                foreach (var p in toAssign)
                {
                    p.BuildingId = b.Id;
                    _personRepo.Update(p);
                }

                MessageBox.Show($"{toAssign.Count} citizens assigned to {b.Name}");
                RefreshDashboard();
                RefreshChart();
                RenderAllBuildings();
            }
        }

        private void BtnNextDay_Click(object sender, EventArgs e)
        {
            var buildings = _buildingRepo.GetAll();
            int population = _personRepo.CountAll();
            var state = _cityRepo.GetState();

            double income = buildings.Sum(b => b.Income);
            double totalEnergyDemand = buildings.Sum(b => b.EnergyConsumption) + population * 0.2;
            double totalWaterDemand = buildings.Sum(b => b.WaterConsumption) + population * 0.3;
            double pollution = buildings.Sum(b => b.Pollution) + population * 0.1;
            double expenses = totalEnergyDemand * 0.5 + totalWaterDemand * 0.2 + population * 0.1;

            double energyConsumed = Math.Min(state.EnergyPool, totalEnergyDemand);
            double waterConsumed = Math.Min(state.WaterPool, totalWaterDemand);
            state.EnergyPool -= energyConsumed;
            state.WaterPool -= waterConsumed;

            double newBudget = state.Budget + income - expenses;
            state.Budget = newBudget;

            state.DayCounter += 1;
            _cityRepo.UpdateState(state);

            double parks = buildings.Count(b => b.Type == "Park");
            double happiness = 70 - pollution * 0.4 + parks * 5 + (state.Budget / 10000.0) * 5;
            happiness = Math.Max(0, Math.Min(100, happiness));

            _cityRepo.LogDay(state.DayCounter, population, income, expenses, state.Budget, pollution, energyConsumed, waterConsumed, happiness);

            RefreshDashboard();
            RefreshChart();
            MessageBox.Show("Day simulated.");
        }

        private void RefreshDashboard()
        {
            var state = _cityRepo.GetState();
            lblDay.Text = $"Day: {state.DayCounter}";
            lblBudget.Text = $"Budget: {Math.Round(state.Budget, 2)}";
            lblEnergy.Text = $"Energy pool: {Math.Round(state.EnergyPool, 2)}";
            lblWater.Text = $"Water pool: {Math.Round(state.WaterPool, 2)}";
            lblPopulation.Text = $"Population: {_personRepo.CountAll()}";
            var buildings = _buildingRepo.GetAll();
            double pollution = buildings.Sum(b => b.Pollution) + _personRepo.CountAll() * 0.1;
            lblPollution.Text = $"Pollution: {Math.Round(pollution, 2)}";
            double parks = buildings.Count(b => b.Type == "Park");
            double happiness = 70 - pollution * 0.4 + parks * 5 + (state.Budget / 10000.0) * 5;
            lblHappiness.Text = $"Happiness: {Math.Round(happiness, 2)}";

            // Update badges if visible
            foreach (var kv in buildingControls)
            {
                var ctrl = kv.Value;
                var badge = ctrl.Controls.OfType<Control>().FirstOrDefault(c => c.Name == "badge");
                if (badge != null)
                {
                    badge.Text = GetOccupantCountForBadge(kv.Key).ToString();
                }
            }
        }

        private void RefreshChart()
        {
            var dt = _cityRepo.GetEconomyLog();
            chart.Series["Population"].Points.Clear();
            chart.Series["Budget"].Points.Clear();
            foreach (System.Data.DataRow r in dt.Rows)
            {
                int day = Convert.ToInt32(r["DayNumber"]);
                int pop = Convert.ToInt32(r["TotalPopulation"]);
                double bal = Convert.ToDouble(r["Balance"]);
                chart.Series["Population"].Points.AddXY(day, pop);
                chart.Series["Budget"].Points.AddXY(day, bal);
            }
        }

        private void LoadState()
        {
            // placeholder: repositories read DB on demand
        }

        // ===== Dialog classes with safe property names =====

        private class PlaceBuildingWindow : Form
        {
            public string BuildingName { get; private set; }
            public int Capacity { get; private set; }
            public double Cost { get; private set; }
            public double Energy { get; private set; }
            public double Water { get; private set; }
            public double Income { get; private set; }
            public double Pollution { get; private set; }

            public PlaceBuildingWindow(Building def)
            {
                Text = "Place Building";
                Width = 380; Height = 320; StartPosition = FormStartPosition.CenterParent;
                var lblName = new Label { Text = "Name", Left = 10, Top = 10 };
                var txtName = new TextBox { Left = 120, Top = 10, Width = 220, Text = def.Name };
                var lblCap = new Label { Text = "Capacity", Left = 10, Top = 50 };
                var numCap = new NumericUpDown { Left = 120, Top = 50, Width = 100, Value = def.CapacityPopulation, Maximum = 1000 };
                var lblCost = new Label { Text = "Cost", Left = 10, Top = 90 };
                var numCost = new NumericUpDown { Left = 120, Top = 90, Width = 120, DecimalPlaces = 2, Maximum = 100000, Value = (decimal)def.Cost };
                var lblEnergy = new Label { Text = "Energy", Left = 10, Top = 130 };
                var numEnergy = new NumericUpDown { Left = 120, Top = 130, Width = 100, DecimalPlaces = 2, Maximum = 10000, Value = (decimal)def.EnergyConsumption };
                var lblWater = new Label { Text = "Water", Left = 10, Top = 170 };
                var numWater = new NumericUpDown { Left = 120, Top = 170, Width = 100, DecimalPlaces = 2, Maximum = 10000, Value = (decimal)def.WaterConsumption };
                var lblIncome = new Label { Text = "Income/day", Left = 10, Top = 210 };
                var numIncome = new NumericUpDown { Left = 120, Top = 210, Width = 120, DecimalPlaces = 2, Maximum = 100000, Value = (decimal)def.Income };
                var btnOk = new Button { Text = "OK", Left = 120, Top = 250, Width = 80 };
                var btnCancel = new Button { Text = "Cancel", Left = 210, Top = 250, Width = 80 };
                btnOk.Click += (s, e) =>
                {
                    BuildingName = txtName.Text;
                    Capacity = (int)numCap.Value;
                    Cost = (double)numCost.Value;
                    Energy = (double)numEnergy.Value;
                    Water = (double)numWater.Value;
                    Income = (double)numIncome.Value;
                    Pollution = def.Pollution;
                    DialogResult = DialogResult.OK; Close();
                };
                btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
                Controls.AddRange(new Control[] { lblName, txtName, lblCap, numCap, lblCost, numCost, lblEnergy, numEnergy, lblWater, numWater, lblIncome, numIncome, btnOk, btnCancel });
            }
        }

        private class EditBuildingDialog : PlaceBuildingWindow
        {
            public EditBuildingDialog(Building b) : base(b) { }
        }

        private class AddPersonDialog : Form
        {
            public string PersonName { get; private set; }
            public int PersonAge { get; private set; }
            public AddPersonDialog()
            {
                Text = "Add Citizen";
                Width = 340; Height = 180; StartPosition = FormStartPosition.CenterParent;
                var lbl = new Label { Text = "Full name", Left = 10, Top = 12 };
                var txt = new TextBox { Left = 100, Top = 12, Width = 200 };
                var lbl2 = new Label { Text = "Age", Left = 10, Top = 52 };
                var num = new NumericUpDown { Left = 100, Top = 52, Width = 80, Maximum = 120, Minimum = 0 };
                var ok = new Button { Text = "Add", Left = 100, Top = 90, Width = 80 };
                var cancel = new Button { Text = "Cancel", Left = 190, Top = 90, Width = 80 };
                ok.Click += (s, e) => { PersonName = txt.Text; PersonAge = (int)num.Value; DialogResult = DialogResult.OK; Close(); };
                cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
                Controls.AddRange(new Control[] { lbl, txt, lbl2, num, ok, cancel });
            }
        }

        private class AssignDialog : Form
        {
            public Person SelectedPerson { get; private set; }
            public Building SelectedBuilding { get; private set; }

            public AssignDialog(List<Person> persons, List<Building> buildings)
            {
                Text = "Assign Citizen";
                Width = 420; Height = 220; StartPosition = FormStartPosition.CenterParent;
                var lblp = new Label { Text = "Person", Left = 10, Top = 10 };
                var cbp = new ComboBox { Left = 100, Top = 10, Width = 280 };
                cbp.DisplayMember = "FullName"; cbp.DataSource = persons;
                var lblb = new Label { Text = "Building", Left = 10, Top = 50 };
                var cbb = new ComboBox { Left = 100, Top = 50, Width = 280 };
                cbb.DisplayMember = "Name"; cbb.DataSource = buildings;
                var ok = new Button { Text = "OK", Left = 100, Top = 100, Width = 80 };
                ok.Click += (s, e) => { SelectedPerson = (Person)cbp.SelectedItem; SelectedBuilding = (Building)cbb.SelectedItem; DialogResult = DialogResult.OK; Close(); };
                var cancel = new Button { Text = "Cancel", Left = 200, Top = 100, Width = 80 };
                cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
                Controls.AddRange(new Control[] { lblp, cbp, lblb, cbb, ok, cancel });
            }
        }

        private class SelectBuildingDialog : Form
        {
            public Building SelectedBuilding { get; private set; }

            public SelectBuildingDialog(List<Building> buildings)
            {
                Text = "Select Building";
                Width = 300;
                Height = 150;
                StartPosition = FormStartPosition.CenterParent;

                var cb = new ComboBox { Dock = DockStyle.Top };
                cb.DataSource = buildings;
                cb.DisplayMember = "Name";
                Controls.Add(cb);

                var ok = new Button { Text = "OK", Dock = DockStyle.Bottom };
                ok.Click += (s, e) => { SelectedBuilding = (Building)cb.SelectedItem; DialogResult = DialogResult.OK; Close(); };
                Controls.Add(ok);
            }
        }
    }
   

}
