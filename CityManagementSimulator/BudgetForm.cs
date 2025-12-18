using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CityManagementSimulator.Models;
using CityManagementSimulator.Repositories;

namespace CityManagementSimulator
{
    public class BudgetForm : Form
    {
        private readonly CityRepository _cityRepo = new CityRepository();

        private Label lblBudget;
        private Label lblLastRevenue;
        private Label lblLastExpenses;
        private Label lblLastBalance;

        private NumericUpDown numEnergyAllocation;
        private NumericUpDown numWaterAllocation;
        private Button btnApply;
        private Button btnRefresh;
        private Button btnClose;

        public BudgetForm()
        {
            Text = "Budget";
            Width = 520;
            Height = 360;
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
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            var grpOverview = new GroupBox { Text = "Overview", Dock = DockStyle.Fill, Padding = new Padding(10) };
            var overview = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            overview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            overview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            lblBudget = new Label { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            lblLastRevenue = new Label { AutoSize = true };
            lblLastExpenses = new Label { AutoSize = true };
            lblLastBalance = new Label { AutoSize = true };

            overview.Controls.Add(new Label { Text = "Current budget:", AutoSize = true }, 0, 0);
            overview.Controls.Add(lblBudget, 1, 0);
            overview.Controls.Add(new Label { Text = "Last day revenue:", AutoSize = true }, 0, 1);
            overview.Controls.Add(lblLastRevenue, 1, 1);
            overview.Controls.Add(new Label { Text = "Last day expenses:", AutoSize = true }, 0, 2);
            overview.Controls.Add(lblLastExpenses, 1, 2);
            overview.Controls.Add(new Label { Text = "Last day balance:", AutoSize = true }, 0, 3);
            overview.Controls.Add(lblLastBalance, 1, 3);

            grpOverview.Controls.Add(overview);

            var grpAlloc = new GroupBox { Text = "Budget allocation", Dock = DockStyle.Fill, Padding = new Padding(10) };
            var alloc = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true };
            alloc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            alloc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

            numEnergyAllocation = new NumericUpDown
            {
                Maximum = 1000000,
                DecimalPlaces = 2,
                ThousandsSeparator = true,
                Dock = DockStyle.Fill
            };
            numWaterAllocation = new NumericUpDown
            {
                Maximum = 1000000,
                DecimalPlaces = 2,
                ThousandsSeparator = true,
                Dock = DockStyle.Fill
            };

            alloc.Controls.Add(new Label { Text = "Allocate to energy pool:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            alloc.Controls.Add(numEnergyAllocation, 1, 0);
            alloc.Controls.Add(new Label { Text = "Allocate to water pool:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            alloc.Controls.Add(numWaterAllocation, 1, 1);

            var allocHint = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 0),
                Text = "This will subtract the amount from Budget and add it to the selected pool(s)."
            };

            btnApply = new Button { Text = "Apply", Width = 90, Height = 28 };
            btnApply.Click += (s, e) => ApplyAllocation();

            var allocButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 36, FlowDirection = FlowDirection.RightToLeft };
            allocButtons.Controls.Add(btnApply);

            grpAlloc.Controls.Add(allocButtons);
            grpAlloc.Controls.Add(allocHint);
            grpAlloc.Controls.Add(alloc);

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            btnClose = new Button { Text = "Close", Width = 90, Height = 28 };
            btnClose.Click += (s, e) => Close();
            btnRefresh = new Button { Text = "Refresh", Width = 90, Height = 28 };
            btnRefresh.Click += (s, e) => LoadData();
            bottom.Controls.Add(btnClose);
            bottom.Controls.Add(btnRefresh);

            root.Controls.Add(grpOverview, 0, 0);
            root.Controls.Add(grpAlloc, 0, 1);
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
            if (state == null)
            {
                lblBudget.Text = "0";
                lblLastRevenue.Text = "0";
                lblLastExpenses.Text = "0";
                lblLastBalance.Text = "0";
                return;
            }

            lblBudget.Text = state.Budget.ToString("0.00");

            var log = _cityRepo.GetEconomyLog();
            if (log != null && log.Rows.Count > 0)
            {
                DataRow last = log.Rows[log.Rows.Count - 1];
                lblLastRevenue.Text = Convert.ToDouble(last["TotalIncome"]).ToString("0.00");
                lblLastExpenses.Text = Convert.ToDouble(last["TotalExpenses"]).ToString("0.00");
                lblLastBalance.Text = Convert.ToDouble(last["Balance"]).ToString("0.00");
            }
            else
            {
                lblLastRevenue.Text = "0";
                lblLastExpenses.Text = "0";
                lblLastBalance.Text = "0";
            }

            // Default allocations to 0 each time; user chooses amounts for this operation.
            numEnergyAllocation.Value = 0;
            numWaterAllocation.Value = 0;
        }

        private void ApplyAllocation()
        {
            var state = _cityRepo.GetState();
            if (state == null) return;

            double energy = (double)numEnergyAllocation.Value;
            double water = (double)numWaterAllocation.Value;
            double total = energy + water;

            if (total <= 0)
            {
                MessageBox.Show(this, "Enter an amount to allocate.", "Budget allocation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (total > state.Budget)
            {
                MessageBox.Show(this, "Not enough budget for this allocation.", "Budget allocation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            state.Budget -= total;
            state.EnergyPool += energy;
            state.WaterPool += water;
            _cityRepo.UpdateState(state);

            LoadData();
        }
    }
}
