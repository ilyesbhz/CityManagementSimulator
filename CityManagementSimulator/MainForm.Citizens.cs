using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using CityManagementSimulator.Models; // ADDED

namespace CityManagementSimulator
{
    public partial class MainForm
    {
        private void BtnAddCitizen_Click(object sender, EventArgs e)
        {
            var dlg = new AddPersonDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var p = new Person { FullName = dlg.PersonName, Age = dlg.PersonAge };
                _personRepo.Add(p);
                RefreshDashboard();
                RenderAllBuildings();
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
                RefreshDashboard();
                RenderAllBuildings();
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
                RenderAllBuildings();
            }
        }
    }
}
