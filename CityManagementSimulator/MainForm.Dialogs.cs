using System;
using System.Windows.Forms;
using CityManagementSimulator.Models; // ADDED

namespace CityManagementSimulator
{
    public partial class MainForm
    {
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

            public AssignDialog(System.Collections.Generic.List<Person> persons, System.Collections.Generic.List<Building> buildings)
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

            public SelectBuildingDialog(System.Collections.Generic.List<Building> buildings)
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
