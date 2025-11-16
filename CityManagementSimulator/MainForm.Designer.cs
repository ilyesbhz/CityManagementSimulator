using CityManagementSimulator.Models;
using CityManagementSimulator.Repositories;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CityManagementSimulator
{
    public partial class MainForm : Form
    {
        // Repos
        private readonly BuildingRepository _buildingRepo = new BuildingRepository();
        private readonly PersonRepository _personRepo = new PersonRepository();
        private readonly CityRepository _cityRepo = new CityRepository();
        private readonly RoadRepository _roadRepo = new RoadRepository();

        // UI
        private Panel leftPanel;
        private FlowLayoutPanel toolbox;
        private Panel topPanel;
        private Panel mapContainer;
        private Panel mapPanel;
        private TableLayoutPanel rightPanel;
        private Panel infoPanel;
        private Button btnNextDay, btnAddCitizen, btnAssignCitizen, btnAssignAll;
        private ComboBox comboTileSize;
        private Button btnShowChart;
        private Button btnMove;

        // Dashboard labels
        private Label lblDay;
        private Label lblBudget;
        private Label lblEnergy;
        private Label lblWater;
        private Label lblPopulation;
        private Label lblPollution;
        private Label lblHappiness;

        // Map & visuals
        private readonly int tileSizeDefault = 80;
        private int tileSize;
        private int gridWidth = 500;
        private int gridHeight = 500;
        private Dictionary<int, Control> buildingControls = new Dictionary<int, Control>();

        // Toolbox definitions
        private Dictionary<string, Building> definitions;

        // State
        private string selectedTool = null;

        // Dragging
        private bool isDragging = false;
        private bool didDrag = false;
        private Control draggingControl = null;
        private Point dragStartMouse;
        private Point dragStartClient;
        private Point dragStartControlLocation;

        // Panning (Move tool)
        private bool isPanning = false;
        private Point panStartClient;
        private Point panStartScroll;

        // Right panel selection
        private int? selectedBuildingIdOnRight = null;

        private readonly Dictionary<string, Image> buildingImages = new Dictionary<string, Image>();
        private Image roadImage;

        private readonly Dictionary<string, int> typeCounters = new Dictionary<string, int>
        {
            { "House", 0 }, { "Factory", 0 }, { "Park", 0 }, { "Commerce", 0 }, { "Road", 0 }
        };

        // Zoom
        private int minTileSize = 20;
        private int maxTileSize = 160;
        private int zoomStep = 10;

        public MainForm()
        {
            Text = "City Management Simulator";
            Width = 1280;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            tileSize = tileSizeDefault;
            InitDefinitions();
            LoadBuildingImages();
            LoadRoadImage();
            InitializeUI();

            LoadState();
            RenderAllBuildings();
            RefreshDashboard();
        }

        private void InitDefinitions()
        {
            definitions = new Dictionary<string, Building>();
            definitions["House"] = new Building { Name = "House", Type = "House", CapacityPopulation = 5, Cost = 200, Income = 0, EnergyConsumption = 2, WaterConsumption = 3, Pollution = 0.5 };
            definitions["Factory"] = new Building { Name = "Factory", Type = "Factory", CapacityPopulation = 0, Cost = 800, Income = 50, EnergyConsumption = 20, WaterConsumption = 10, Pollution = 15 };
            definitions["Park"] = new Building { Name = "Park", Type = "Park", CapacityPopulation = 0, Cost = 300, Income = 0, EnergyConsumption = 1, WaterConsumption = 1, Pollution = -5 };
            definitions["Commerce"] = new Building { Name = "Commerce", Type = "Commerce", CapacityPopulation = 3, Cost = 400, Income = 10, EnergyConsumption = 5, WaterConsumption = 3, Pollution = 3 };
        }

        private void LoadBuildingImages()
        {
            buildingImages.Clear();
            TryAddResource("House", () => CityManagementSimulator.Properties.Resources.house);
            TryAddResource("Factory", () => CityManagementSimulator.Properties.Resources.factory);
            TryAddResource("Park", () => CityManagementSimulator.Properties.Resources.park);
            TryAddResource("Commerce", () => CityManagementSimulator.Properties.Resources.commerce);

            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string assetsPath = System.IO.Path.Combine(basePath, "Assets");
            if (System.IO.Directory.Exists(assetsPath))
            {
                TryAddFile("House", System.IO.Path.Combine(assetsPath, "house.png"));
                TryAddFile("Factory", System.IO.Path.Combine(assetsPath, "factory.png"));
                TryAddFile("Commerce", System.IO.Path.Combine(assetsPath, "commerce.png"));
                TryAddFile("Park", System.IO.Path.Combine(assetsPath, "park.png"));
            }

            void TryAddResource(string key, Func<Image> getter)
            {
                try
                {
                    var img = getter();
                    if (img != null && !buildingImages.ContainsKey(key))
                        buildingImages[key] = img;
                }
                catch { }
            }

            void TryAddFile(string key, string path)
            {
                if (!buildingImages.ContainsKey(key) && System.IO.File.Exists(path))
                    buildingImages[key] = Image.FromFile(path);
            }
        }

        private void InitializeUI()
        {
            leftPanel = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = Color.FromArgb(240, 248, 255) };
            Controls.Add(leftPanel);

            var title = new Label
            {
                Text = "Toolbox - Buildings",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Margin = Padding.Empty
            };

            toolbox = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 300,
                AutoScroll = true,
                Padding = new Padding(8),
                Margin = Padding.Empty
            };

            // IMPORTANT: add toolbox first, then title so title docks above the buttons.
            leftPanel.SuspendLayout();
            leftPanel.Controls.Add(toolbox); // added first => goes below
            leftPanel.Controls.Add(title);   // added last  => stays at the very top
            leftPanel.ResumeLayout();

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
                    Font = new Font("Segoe UI", 9),
                    Margin = new Padding(4, 4, 4, 4)
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
                if (int.TryParse(comboTileSize.SelectedItem.ToString(), out int ts))
                {
                    if (ts == tileSize) return;
                    int old = tileSize;
                    tileSize = ts;
                    ApplyTileSizeChange(old); // preserve view correctly on UI zoom change
                }
            };
            topPanel.Controls.Add(comboTileSize);

            btnShowChart = new Button { Text = "Show Charts", Left = 220, Width = 110, Height = 30, Top = 8 };
            btnShowChart.Click += BtnShowChart_Click;
            topPanel.Controls.Add(btnShowChart);

            btnMove = new Button { Text = "Move", Width = 90, Height = 30, Top = 8 };
            topPanel.Controls.Add(btnMove);
            btnMove.Left = btnShowChart.Right + 8;
            btnMove.Click += (s, e) =>
            {
                if (selectedTool == "Move")
                {
                    selectedTool = null;
                    btnMove.BackColor = Color.White;
                    mapPanel.Cursor = Cursors.Default;
                    foreach (Control c in toolbox.Controls)
                        if (c is Button b) b.BackColor = Color.White;
                }
                else
                {
                    selectedTool = "Move";
                    HighlightTool(btnMove);
                }
            };

            mapContainer = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = Padding.Empty };
            Controls.Add(mapContainer);

            mapPanel = new Panel { BackColor = Color.White, Margin = Padding.Empty, Location = Point.Empty };
            typeof(Panel).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, mapPanel, new object[] { true });

            // initial size + scroll area
            ResizeMapSurface(false, null);
            mapPanel.Paint += MapPanel_Paint;
            mapPanel.MouseClick += MapPanel_MouseClick;

            mapPanel.MouseDown += MapPanel_MouseDown;
            mapPanel.MouseMove += MapPanel_MouseMove;
            mapPanel.MouseUp += MapPanel_MouseUp;
            mapPanel.MouseLeave += (s, e) =>
            {
                if (isPanning)
                {
                    isPanning = false;
                    mapPanel.Cursor = selectedTool == "Move" ? Cursors.Hand : Cursors.Default;
                }
            };
            mapPanel.MouseWheel += MapPanel_MouseWheel;

            mapContainer.Controls.Add(mapPanel);

            rightPanel = new TableLayoutPanel { Dock = DockStyle.Right, Width = 360, ColumnCount = 1, RowCount = 3, BackColor = Color.White };
            rightPanel.Padding = new Padding(6);
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(rightPanel);

            infoPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            rightPanel.Controls.Add(infoPanel, 0, 0);
            var header = new Label { Text = "Selected Building", Dock = DockStyle.Top, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            infoPanel.Controls.Add(header);

            var statsGroup = new GroupBox { Text = "City Overview", Dock = DockStyle.Fill, Padding = new Padding(8) };
            rightPanel.Controls.Add(statsGroup, 0, 1);

            var statsTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 0,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 4, 0, 0)
            };
            statsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            statsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            statsGroup.Controls.Add(statsTable);

            lblDay = new Label { Text = "0", AutoSize = true, Anchor = AnchorStyles.Right, TextAlign = ContentAlignment.MiddleRight };
            lblBudget = new Label { Text = "0", AutoSize = true, Anchor = AnchorStyles.Right, Font = new Font("Segoe UI", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };
            lblPopulation = new Label { Text = "0", AutoSize = true, Anchor = AnchorStyles.Right, TextAlign = ContentAlignment.MiddleRight };
            lblPollution = new Label { Text = "0", AutoSize = true, Anchor = AnchorStyles.Right, TextAlign = ContentAlignment.MiddleRight };
            lblEnergy = new Label { Text = "0", AutoSize = true, Anchor = AnchorStyles.Right, TextAlign = ContentAlignment.MiddleRight };
            lblWater = new Label { Text = "0", AutoSize = true, Anchor = AnchorStyles.Right, TextAlign = ContentAlignment.MiddleRight };
            lblHappiness = new Label { Text = "0", AutoSize = true, Anchor = AnchorStyles.Right, TextAlign = ContentAlignment.MiddleRight };

            void AddRow(string caption, Label value)
            {
                int r = statsTable.RowCount;
                statsTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                var nameLbl = new Label { Text = caption, AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft };
                nameLbl.Margin = new Padding(0, 3, 0, 3);
                value.Margin = new Padding(0, 3, 0, 3);
                statsTable.Controls.Add(nameLbl, 0, r);
                statsTable.Controls.Add(value, 1, r);
                statsTable.RowCount++;
            }

            AddRow("Day", lblDay);
            AddRow("Budget", lblBudget);
            AddRow("Population", lblPopulation);
            AddRow("Pollution", lblPollution);
            AddRow("Energy pool", lblEnergy);
            AddRow("Water pool", lblWater);
            AddRow("Happiness", lblHappiness);

            var chartsPlaceholder = new Panel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke };
            var lblOpen = new Label { Text = "Use 'Show Charts' button to view economy graphs.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            chartsPlaceholder.Controls.Add(lblOpen);
            rightPanel.Controls.Add(chartsPlaceholder, 0, 2);

            var btnRoad = new Button
            {
                Text = "Road",
                Tag = "Road",
                Width = 180,
                Height = 44,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            btnRoad.FlatAppearance.BorderColor = Color.LightGray;
            btnRoad.Click += (s, e) =>
            {
                selectedTool = "Road";
                HighlightTool((Button)s);
            };
            toolbox.Controls.Add(btnRoad);

            mapPanel.TabStop = true;
            mapPanel.MouseEnter += (s, e) => mapPanel.Focus();
        }

        private void HighlightTool(Button btn)
        {
            foreach (Control c in toolbox.Controls) if (c is Button b) b.BackColor = Color.White;
            btn.BackColor = Color.LightBlue;
            mapPanel.Cursor = (selectedTool == "Move") ? Cursors.Hand : Cursors.Default;
        }

        private void BtnShowChart_Click(object sender, EventArgs e)
        {
            using (var cf = new ChartForm())
            {
                cf.ShowDialog(this);
            }
        }

        private void LoadRoadImage()
        {
            roadImage = null;
            try { roadImage = CityManagementSimulator.Properties.Resources.road; } catch { }
            if (roadImage == null)
            {
                var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "road.png");
                if (System.IO.File.Exists(path))
                    roadImage = Image.FromFile(path);
            }
        }

        private void InitializeComponent()
        {
            InitializeUI();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                roadImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
