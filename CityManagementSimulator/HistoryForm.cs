using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CityManagementSimulator.Repositories;

namespace CityManagementSimulator
{
    public class HistoryForm : Form
    {
        private readonly CityRepository _cityRepo = new CityRepository();
        private DataGridView grid;
        private Button btnClose;
        private Button btnRefresh;

        public HistoryForm()
        {
            Text = "Economy History";
            Width = 900;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;
            InitializeUI();
        }

        private void InitializeUI()
        {
            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(8),
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.WhiteSmoke
            };

            btnRefresh = new Button { Text = "Refresh", Width = 90, Height = 28 };
            btnRefresh.Click += (s, e) => LoadData();
            btnClose = new Button { Text = "Close", Width = 90, Height = 28 };
            btnClose.Click += (s, e) => Close();

            top.Controls.Add(btnRefresh);
            top.Controls.Add(btnClose);

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            Controls.Add(grid);
            Controls.Add(top);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadData();
        }

        private void LoadData()
        {
            DataTable dt = _cityRepo.GetEconomyLog();
            grid.DataSource = dt;
        }
    }
}
