using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CityManagementSimulator.Repositories;
using CityManagementSimulator.Models;

namespace CityManagementSimulator
{
    public class PopulationForm : Form
    {
        private readonly PersonRepository _personRepo = new PersonRepository();
        private readonly BuildingRepository _buildingRepo = new BuildingRepository();
        private ListView listView;
        private Button btnAdd;
        private Button btnRemove;
        private Button btnPlace;
        private Button btnClose;

        public PopulationForm()
        {
            Text = "Population";
            Width = 520;
            Height = 420;
            StartPosition = FormStartPosition.CenterParent;
            InitializeUI();
        }

        private void InitializeUI()
        {
            listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            listView.Columns.Add("Name", 240);
            listView.Columns.Add("Age", 80, HorizontalAlignment.Right);
            listView.Columns.Add("Assigned Building", 160);

            var panelTop = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.WhiteSmoke };
            btnAdd = new Button { Text = "Add Person", Width = 110, Height = 28, Left = 8, Top = 8 };
            btnAdd.Click += (s, e) =>
            {
                var dlg = new AddPersonDialogLocal();
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var p = new Person { FullName = dlg.PersonName, Age = dlg.PersonAge };
                    _personRepo.Add(p);
                    LoadData();
                }
            };
            btnRemove = new Button { Text = "Remove Person", Width = 120, Height = 28, Left = btnAdd.Right + 8, Top = 8 };
            btnRemove.Click += (s, e) =>
            {
                if (listView.SelectedItems.Count == 0)
                {
                    MessageBox.Show(this, "Select a person to remove.", "Population", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var item = listView.SelectedItems[0];
                var name = item.SubItems[0].Text;
                var person = _personRepo.GetAll().FirstOrDefault(p => p.FullName == name);
                if (person == null) return;
                var confirm = MessageBox.Show(this, $"Remove {person.FullName}?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm == DialogResult.Yes)
                {
                    _personRepo.Delete(person.Id);
                    LoadData();
                }
            };

            btnPlace = new Button { Text = "Place Person", Width = 110, Height = 28, Left = btnRemove.Right + 8, Top = 8 };
            btnPlace.Click += (s, e) =>
            {
                if (listView.SelectedItems.Count == 0)
                {
                    MessageBox.Show(this, "Select a person to place.", "Population", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var item = listView.SelectedItems[0];
                var name = item.SubItems[0].Text;
                var person = _personRepo.GetAll().FirstOrDefault(p => p.FullName == name);
                if (person == null) return;

                var buildings = _buildingRepo.GetAll();
                if (buildings.Count == 0)
                {
                    MessageBox.Show(this, "No buildings available.", "Place Person", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var dlg = new SelectBuildingLocalDialog(buildings);
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedBuilding != null)
                {
                    var b = dlg.SelectedBuilding;
                    int currentOcc = _personRepo.GetAll().Count(p => p.BuildingId == b.Id);
                    int capacityLeft = b.CapacityPopulation > 0 ? Math.Max(0, b.CapacityPopulation - currentOcc) : int.MaxValue;
                    if (capacityLeft == 0)
                    {
                        MessageBox.Show(this, "Selected building has no capacity left.", "Place Person", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    person.BuildingId = b.Id;
                    _personRepo.Update(person);
                    LoadData();
                }
            };

            btnClose = new Button { Text = "Close", Width = 90, Height = 28, Left = btnPlace.Right + 8, Top = 8 };
            btnClose.Click += (s, e) => Close();
            panelTop.Controls.AddRange(new Control[] { btnAdd, btnRemove, btnPlace, btnClose });

            Controls.Add(listView);
            Controls.Add(panelTop);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadData();
        }

        private void LoadData()
        {
            listView.Items.Clear();
            var people = _personRepo.GetAll();
            foreach (var p in people)
            {
                string building = p.BuildingId.HasValue ? p.BuildingId.Value.ToString() : "Unassigned";
                var item = new ListViewItem(new[] { p.FullName, p.Age.ToString(), building });
                listView.Items.Add(item);
            }
        }

        private class AddPersonDialogLocal : Form
        {
            public string PersonName { get; private set; }
            public int PersonAge { get; private set; }
            public AddPersonDialogLocal()
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

        private class SelectBuildingLocalDialog : Form
        {
            public Building SelectedBuilding { get; private set; }
            public SelectBuildingLocalDialog(System.Collections.Generic.List<Building> buildings)
            {
                Text = "Select Building";
                Width = 320; Height = 160; StartPosition = FormStartPosition.CenterParent;
                var cb = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
                cb.DataSource = buildings;
                cb.DisplayMember = "Name";
                var ok = new Button { Text = "OK", Dock = DockStyle.Bottom };
                ok.Click += (s, e) => { SelectedBuilding = (Building)cb.SelectedItem; DialogResult = DialogResult.OK; Close(); };
                Controls.Add(cb);
                Controls.Add(ok);
            }
        }
    }
}
