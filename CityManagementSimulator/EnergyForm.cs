using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CityManagementSimulator.Repositories;

namespace CityManagementSimulator
{
    public class EnergyForm : Form
    {
        private readonly BuildingRepository _buildingRepo = new BuildingRepository();
        private readonly CityRepository _cityRepo = new CityRepository();

        private Label lblProduction;
        private Label lblConsumption;
        private Label lblNet;
        private ListView listByType;

        private Button btnRefresh;
        private Button btnClose;

        public EnergyForm()
        {
            Text = "Energy";
            Width = 560;
            Height = 420;
            StartPosition = FormStartPosition.CenterParent;
            InitializeUI();
        }

        private void InitializeUI()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            var grpOverview = new GroupBox { Text = "Overview", Dock = DockStyle.Fill, Padding = new Padding(10) };
            var overview = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            overview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            overview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

            lblProduction = new Label { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            lblConsumption = new Label { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            lblNet = new Label { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            overview.Controls.Add(new Label { Text = "Energy production (pool):", AutoSize = true }, 0, 0);
            overview.Controls.Add(lblProduction, 1, 0);
            overview.Controls.Add(new Label { Text = "Energy consumption (buildings):", AutoSize = true }, 0, 1);
            overview.Controls.Add(lblConsumption, 1, 1);
            overview.Controls.Add(new Label { Text = "Net (production - consumption):", AutoSize = true }, 0, 2);
            overview.Controls.Add(lblNet, 1, 2);
            grpOverview.Controls.Add(overview);

            var grpBreakdown = new GroupBox { Text = "Consumption by building type", Dock = DockStyle.Fill, Padding = new Padding(10) };
            listByType = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            listByType.Columns.Add("Type", 160);
            listByType.Columns.Add("Count", 80, HorizontalAlignment.Right);
            listByType.Columns.Add("Total consumption", 140, HorizontalAlignment.Right);
            grpBreakdown.Controls.Add(listByType);

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            btnClose = new Button { Text = "Close", Width = 90, Height = 28 };
            btnClose.Click += (s, e) => Close();
            btnRefresh = new Button { Text = "Refresh", Width = 90, Height = 28 };
            btnRefresh.Click += (s, e) => LoadData();
            bottom.Controls.Add(btnClose);
            bottom.Controls.Add(btnRefresh);

            root.Controls.Add(grpOverview, 0, 0);
            root.Controls.Add(grpBreakdown, 0, 1);
            root.Controls.Add(bottom, 0, 2);

            Controls.Add(root);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadData();
        }

        private void LoadData()
        {
            var state = _cityRepo.GetState();
            double production = state != null ? state.EnergyPool : 0.0;

            var buildings = _buildingRepo.GetAll();
            double consumption = buildings.Sum(b => b.EnergyConsumption);
            double net = production - consumption;

            lblProduction.Text = production.ToString("0.00");
            lblConsumption.Text = consumption.ToString("0.00");
            lblNet.Text = net.ToString("0.00");

            listByType.BeginUpdate();
            listByType.Items.Clear();

            var groups = buildings
                .GroupBy(b => b.Type)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(x => x.EnergyConsumption)
                });

            foreach (var g in groups)
            {
                var item = new ListViewItem(new[]
                {
                    g.Type,
                    g.Count.ToString(),
                    g.Total.ToString("0.00")
                });
                listByType.Items.Add(item);
            }

            listByType.EndUpdate();
        }
    }
}
