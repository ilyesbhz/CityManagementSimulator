using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using CityManagementSimulator.Models; // ADDED

namespace CityManagementSimulator
{
    public partial class MainForm
    {
        private void ShowBuildingOnRight(int id)
        {
            var b = _buildingRepo.GetAll().FirstOrDefault(x => x.Id == id);
            if (b == null) return;
            selectedBuildingIdOnRight = id;

            infoPanel.Controls.Clear();

            var header = new Label { Text = $"{b.Name} ({b.Type})", Dock = DockStyle.Top, Font = new Font("Segoe UI", 10, FontStyle.Bold), Height = 28 };
            infoPanel.Controls.Add(header);

            infoPanel.Controls.Add(new Label { Text = $"Capacity: {b.CapacityPopulation}", Dock = DockStyle.Top });

            var occupants = _personRepo.GetAll().Where(p => p.BuildingId == b.Id).ToList();
            infoPanel.Controls.Add(new Label { Text = $"Occupants: {occupants.Count}", Dock = DockStyle.Top });

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
            infoPanel.Controls.Add(new Label { Text = "Selected Building", Dock = DockStyle.Top, Font = new Font("Segoe UI", 10, FontStyle.Bold) });
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
                RenderAllBuildings();
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
            RefreshDashboard();
            ClearRightInfo();
        }

        private void EditBuilding(int id)
        {
            var b = _buildingRepo.GetAll().FirstOrDefault(x => x.Id == id);
            if (b == null) return;
            var dlg = new EditBuildingDialog(b);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                b.Name = dlg.BuildingName;
                b.CapacityPopulation = dlg.Capacity;
                b.Cost = dlg.Cost;
                b.EnergyConsumption = dlg.Energy;
                b.WaterConsumption = dlg.Water;
                _buildingRepo.Update(b);
                RenderAllBuildings();
                RefreshDashboard();
            }
        }

        private void ShowOccupants(int id)
        {
            var persons = _personRepo.GetAll().Where(p => p.BuildingId == id).ToList();
            string msg = persons.Count == 0 ? "No occupants." : string.Join("\n", persons.Select(p => p.FullName));
            MessageBox.Show(msg, "Occupants");
        }

        private string GenerateName(string type)
        {
            if (!typeCounters.ContainsKey(type)) typeCounters[type] = 0;
            int next = ++typeCounters[type];
            return $"{type} #{next}";
        }
    }
}
